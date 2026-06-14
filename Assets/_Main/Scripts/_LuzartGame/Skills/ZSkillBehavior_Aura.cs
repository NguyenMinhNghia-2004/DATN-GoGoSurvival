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
        private float _visualTimer;
        private const int VISUAL_COUNT = 6;
        private GameObject[] _visuals = new GameObject[VISUAL_COUNT];
        private Transform[] _visualTrs = new Transform[VISUAL_COUNT];
        private SpriteRenderer[] _visualSrs = new SpriteRenderer[VISUAL_COUNT];

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
            if (_visuals != null)
            {
                for (int i = 0; i < _visuals.Length; i++)
                {
                    if (_visuals[i] != null) Object.Destroy(_visuals[i]);
                }
            }
            _visuals = null; _visualTrs = null; _visualSrs = null;
        }

        private void SpawnVisual()
        {
            var prefab = _behaviorConfig != null ? _behaviorConfig.VisualPrefab : null;
            if (prefab == null) return;
            for (int i = 0; i < VISUAL_COUNT; i++)
            {
                _visuals[i] = Object.Instantiate(prefab);
                _visuals[i].name = $"AuraVisual_{_skill?.Config?.name}_{i}";
                // Bỏ collider trên visual — aura damage logic dùng Physics2D.OverlapCircle riêng,
                // không cần trigger trên visual (tránh false-positive cho ProjectileVisualBinder/EntityRef).
                var col = _visuals[i].GetComponent<Collider2D>();
                if (col != null) Object.Destroy(col);
                var binder = _visuals[i].GetComponent<ProjectileVisualBinder>();
                if (binder != null) Object.Destroy(binder);
                _visualTrs[i] = _visuals[i].transform;
                _visualSrs[i] = _visuals[i].GetComponent<SpriteRenderer>();
            }
        }

        protected override void DoUpdate(float dt)
        {
            if (_zSkillUpgradeConfig == null || _owner?.Transform == null) return;

            _visualTimer += dt;
            _tickTimer += dt;
            if (_tickTimer < _behaviorConfig.TickInterval) return;
            _tickTimer = 0f;
            ApplyTick();
        }

        private void LateUpdate()
        {
            if (!_bound || _zSkillUpgradeConfig == null || _owner?.Transform == null) return;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (_visualTrs == null || _visualTrs.Length == 0) return;
            Vector2 origin = _owner.Transform.Position.Value;
            
            // Tính toán kích thước tối đa dựa trên RangeFind
            float range = (float)_zSkillUpgradeConfig.GetStat(StatType.RangeFind).Value;
            float unitSize = Mathf.Max(0.01f, _behaviorConfig.VisualUnitSize);
            float maxScale = (range * 2f) / unitSize;

            // Dùng _visualTimer để mượt mà hơn. Đặt chu kỳ hiển thị là 2.5 giây để hiệu ứng chậm lại,
            // không bị phụ thuộc vào TickInterval (vốn gây sát thương rất nhanh).
            float visualDuration = 4f; 
            float baseT = (_visualTimer / visualDuration) % 1f;
            
            for (int i = 0; i < _visualTrs.Length; i++)
            {
                if (_visualTrs[i] == null) continue;
                _visualTrs[i].position = new Vector3(origin.x, origin.y, _visualTrs[i].position.z);

                // Offset từng vòng để nó mọc ra lần lượt
                float phaseOffset = i * (1f / VISUAL_COUNT);
                float t = (baseT + phaseOffset) % 1f;

                // Dùng hàm easing out (nhanh dần rồi chậm lại) để hiệu ứng nhìn tự nhiên hơn
                float easeOutQuart = 1f - Mathf.Pow(1f - t, 4f);
                float currentScale = Mathf.Lerp(0f, maxScale, easeOutQuart);
                
                _visualTrs[i].localScale = new Vector3(currentScale, currentScale, 1f);

                // Hiệu ứng mờ dần (fade out) khi vòng năng lượng tan biến
                if (_visualSrs[i] != null)
                {
                    Color c = _visualSrs[i].color;
                    c.a = Mathf.Lerp(1f, 0f, easeOutQuart);
                    _visualSrs[i].color = c;
                }
            }
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
