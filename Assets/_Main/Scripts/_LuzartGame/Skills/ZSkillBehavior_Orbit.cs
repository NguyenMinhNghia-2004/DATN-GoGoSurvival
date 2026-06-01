using System.Collections.Generic;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Orbit pattern (Guardian). Spawns N projectile-visual GameObjects that rotate around
    /// the owner. Each orbit has its own EntityRef → small DamageOrbiter component that
    /// damages enemies on touch (cooldown-gated to avoid frame-spam).
    /// </summary>
    public class ZSkillBehavior_Orbit : ZSkillBehavior<ZSkillBehaviorConfig_Orbit>
    {
        private readonly List<OrbitInstance> _orbits = new List<OrbitInstance>();
        private float _baseAngle;

        protected override void DoBind() { /* orbits spawn in RefreshNumbers via DoStart trigger */ }

        protected override void DoStart()
        {
            base.DoStart();
            RebuildOrbits();
        }

        protected override void RefreshNumbers()
        {
            base.RefreshNumbers();
            // On level-up, AmountProjectile + FireSpeed may change → rebuild.
            if (_bound) RebuildOrbits();
        }

        protected override void DoUpdate(float dt)
        {
            // Orbits don't use the cooldown→Attack loop. Skip base DoUpdate; just tick angles.
            if (_zSkillUpgradeConfig == null || _owner?.Transform == null) return;
            float angularSpeed = _behaviorConfig.BaseAngularSpeed
                                 * (float)_zSkillUpgradeConfig.GetStat(StatType.FireSpeed).Value;
            // FireSpeed stat baseline is around 10 in our upgrade configs → /10 to normalize.
            angularSpeed /= 10f;
            _baseAngle += angularSpeed * dt;

            Vector2 origin = _owner.Transform.Position.Value;
            float radius = _behaviorConfig.OrbitRadius;
            int n = _orbits.Count;
            for (int i = 0; i < n; i++)
            {
                if (_orbits[i].go == null) continue;
                float a = (_baseAngle + i * (360f / n)) * Mathf.Deg2Rad;
                Vector2 pos = origin + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                _orbits[i].go.transform.position = new Vector3(pos.x, pos.y, _orbits[i].go.transform.position.z);
            }
        }

        protected override void DoOnDestroy()
        {
            base.DoOnDestroy();
            DespawnAll();
        }

        private void RebuildOrbits()
        {
            DespawnAll();
            int n = (int)_zSkillUpgradeConfig.GetStat(StatType.AmountProjectile).Value;
            if (n <= 0) return;
            var prefab = _behaviorConfig.ProjectileConfig != null
                ? _behaviorConfig.ProjectileConfig.VisualPrefab : null;
            double damage = _zSkillUpgradeConfig.GetStat(StatType.ATK).Value;
            for (int i = 0; i < n; i++)
            {
                GameObject go;
                if (prefab != null)
                {
                    go = Object.Instantiate(prefab);
                    go.name = $"Orbit_{_skill.Config.name}_{i}";
                }
                else
                {
                    go = new GameObject($"Orbit_{_skill.Config.name}_{i}");
                }
                // Strip ProjectileVisualBinder if the prefab was authored for projectile sync —
                // orbit drives transform directly.
                var binder = go.GetComponent<ProjectileVisualBinder>();
                if (binder != null) Object.Destroy(binder);

                // Damage component on the orbit GO (per-orbit per-enemy cooldown so a slow
                // orbit doesn't tick damage every frame on a stuck enemy).
                var dmg = go.GetComponent<OrbitDamager>();
                if (dmg == null) dmg = go.AddComponent<OrbitDamager>();
                dmg.Configure(damage, _owner is PlayerCharacter);

                _orbits.Add(new OrbitInstance { go = go });
            }
        }

        private void DespawnAll()
        {
            for (int i = 0; i < _orbits.Count; i++)
            {
                if (_orbits[i].go != null) Object.Destroy(_orbits[i].go);
            }
            _orbits.Clear();
        }

        private struct OrbitInstance { public GameObject go; }
    }

    /// <summary>
    /// Small damage applier on an orbit GameObject. Triggers on overlap with enemies and
    /// applies damage with a per-entity cooldown. Also destroys enemy projectiles on
    /// contact (the "block bullets" half of Guardian's GDD description).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OrbitDamager : MonoBehaviour
    {
        private double _damage;
        private bool _ownerIsPlayer;
        private readonly Dictionary<int, float> _lastHit = new Dictionary<int, float>();
        private const float HIT_COOLDOWN = 0.5f;

        public void Configure(double damage, bool ownerIsPlayer)
        {
            _damage = damage;
            _ownerIsPlayer = ownerIsPlayer;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other == null) return;
            int id = other.GetInstanceID();
            if (_lastHit.TryGetValue(id, out float t) && Time.time - t < HIT_COOLDOWN) return;

            var er = other.GetComponent<EntityRef>();
            // Damage enemies (player-owned orbit) or player (enemy-owned orbit).
            if (er != null && er.Entity != null)
            {
                bool isEnemy = er.Entity is EnemyCharacter;
                bool isPlayer = er.Entity is PlayerCharacter;
                if (_ownerIsPlayer && isEnemy)
                {
                    _lastHit[id] = Time.time;
                    if (er.Entity is CharacterBase ch && !ch.IsDead) ch.TakeDamage(_damage);
                    var root = other.GetComponentInParent<LuzartEnemyEntityRoot>();
                    if (root != null) root.TakeDamage((float)_damage);
                    return;
                }
                if (!_ownerIsPlayer && isPlayer)
                {
                    _lastHit[id] = Time.time;
                    if (er.Entity is CharacterBase ch && !ch.IsDead) ch.TakeDamage(_damage);
                    return;
                }
            }

            // Bullet block — destroy enemy projectiles (no EntityRef but tagged "Bolt"
            // by legacy convention). Player orbit blocks enemy bullets only.
            if (_ownerIsPlayer && other.CompareTag("Bolt"))
            {
                // Skip our own player-side projectiles: they live on a child of Player.
                if (other.GetComponentInParent<LuzartPlayerEntityRoot>() == null)
                {
                    _lastHit[id] = Time.time;
                    Destroy(other.gameObject);
                }
            }
        }
    }
}
