using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Sustained AoE around the owner. Overrides DoUpdate to tick a damage timer instead
    /// of using the cooldown→Attack loop (since aura is continuous, not pulsed).
    ///
    /// Queries Unity Physics2D directly (same path as TargetProvider after the refactor)
    /// so the EntityRef bridge gives us the enemy GameObject + we route damage through
    /// LuzartEnemyEntityRoot.TakeDamage (the path that owns visual HP + death).
    /// </summary>
    public class ZSkillBehavior_Aura : ZSkillBehavior<ZSkillBehaviorConfig_Aura>
    {
        private float _tickTimer;
        private GameObject _visual;       // Vòng aura visual follow player
        private Transform _visualTr;
        private SpriteRenderer _visualSr; // Có thể null nếu prefab không có

        // Static buffer — multiple aura skills (if ever) share but each ticks
        // serially; safe because we don't hold the reference past one ApplyTick call.
        private static readonly Collider2D[] _hits = new Collider2D[32];

        protected override void DoStart()
        {
            base.DoStart();
            SpawnVisual();
        }

        protected override void DoOnDestroy()
        {
            base.DoOnDestroy();
            if (_visual != null) Object.Destroy(_visual);
            _visual = null; _visualTr = null; _visualSr = null;
        }

        private void SpawnVisual()
        {
            var prefab = _behaviorConfig != null ? _behaviorConfig.VisualPrefab : null;
            if (prefab == null) return;
            _visual = Object.Instantiate(prefab);
            _visual.name = $"AuraVisual_{_skill?.Config?.name}";
            // Bỏ collider trên visual — aura damage logic dùng Physics2D.OverlapCircle riêng,
            // không cần trigger trên visual (tránh false-positive cho ProjectileVisualBinder/EntityRef).
            var col = _visual.GetComponent<Collider2D>();
            if (col != null) Object.Destroy(col);
            var binder = _visual.GetComponent<ProjectileVisualBinder>();
            if (binder != null) Object.Destroy(binder);
            _visualTr = _visual.transform;
            _visualSr = _visual.GetComponent<SpriteRenderer>();
        }

        protected override void DoUpdate(float dt)
        {
            if (_zSkillUpgradeConfig == null || _owner?.Transform == null) return;

            // Sync visual mỗi frame: follow player position + scale theo RangeFind.
            UpdateVisual();

            _tickTimer += dt;
            if (_tickTimer < _behaviorConfig.TickInterval) return;
            _tickTimer = 0f;
            ApplyTick();
        }

        private void UpdateVisual()
        {
            if (_visualTr == null) return;
            Vector2 origin = _owner.Transform.Position.Value;
            _visualTr.position = new Vector3(origin.x, origin.y, _visualTr.position.z);
            // Scale sao cho sprite có đường kính = 2 * RangeFind. Sprite world-size mặc định
            // (Config.VisualUnitSize, mặc định 2.56 với PPU 256) → scale = diameter / unitSize.
            float range = (float)_zSkillUpgradeConfig.GetStat(StatType.RangeFind).Value;
            float unitSize = Mathf.Max(0.01f, _behaviorConfig.VisualUnitSize);
            float scale = (range * 2f) / unitSize;
            _visualTr.localScale = new Vector3(scale, scale, 1f);
        }

        private void ApplyTick()
        {
            float range = (float)_zSkillUpgradeConfig.GetStat(StatType.RangeFind).Value;
            double damage = _zSkillUpgradeConfig.GetStat(StatType.ATK).Value;
            if (range <= 0 || damage <= 0) return;

            Vector2 origin = _owner.Transform.Position.Value;
            int count = Physics2D.OverlapCircleNonAlloc(origin, range, _hits);
            bool ownerIsPlayer = _owner is PlayerCharacter;
            for (int i = 0; i < count; i++)
            {
                var col = _hits[i];
                if (col == null) continue;
                var er = col.GetComponent<EntityRef>();
                if (er == null || er.Entity == null) continue;
                // Faction filter — player aura hits enemies and vice-versa.
                bool isEnemy = er.Entity is EnemyCharacter;
                bool isPlayer = er.Entity is PlayerCharacter;
                if (ownerIsPlayer && !isEnemy) continue;
                if (!ownerIsPlayer && !isPlayer) continue;

                // Belt-and-suspenders damage (same shape as ProjectileVisualBinder):
                //  - Luzart character (no-op if StatsBehavior was skipped at Init)
                //  - Legacy LuzartEnemyEntityRoot (owns visual HP for now)
                if (er.Entity is CharacterBase ch && !ch.IsDead) ch.TakeDamage(damage);
                if (ownerIsPlayer)
                {
                    var root = col.GetComponentInParent<LuzartEnemyEntityRoot>();
                    if (root != null) root.TakeDamage((float)damage);
                }
            }
        }
    }
}
