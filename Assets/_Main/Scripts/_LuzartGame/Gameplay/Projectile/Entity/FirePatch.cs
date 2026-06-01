using System.Collections.Generic;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// "Ground patch" damage-over-time effect (Molotov). Spawned by ZSkillBehavior_GroundPatch
    /// when the bomb lands. Ticks damage on all enemies within radius every tickInterval,
    /// then self-destroys after duration.
    ///
    /// <para>Not a Luzart entity — pure MonoBehaviour. Damage routes through
    /// LuzartEnemyEntityRoot.TakeDamage (the same path projectiles + auras use), so the
    /// legacy visual HP system stays in sync.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FirePatch : MonoBehaviour
    {
        private float _radius;
        private double _damage;
        private float _duration;
        private float _tickInterval;
        private bool _ownerIsPlayer;

        private float _elapsed;
        private float _tickTimer;

        private static readonly Collider2D[] _hits = new Collider2D[16];

        public void Configure(float radius, double damage, float duration, float tickInterval, bool ownerIsPlayer)
        {
            _radius = radius;
            _damage = damage;
            _duration = duration;
            _tickInterval = Mathf.Max(0.05f, tickInterval);
            _ownerIsPlayer = ownerIsPlayer;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _elapsed += dt;
            if (_elapsed >= _duration) { Destroy(gameObject); return; }

            _tickTimer += dt;
            if (_tickTimer < _tickInterval) return;
            _tickTimer = 0f;

            ApplyTick();
        }

        private void ApplyTick()
        {
            if (_radius <= 0 || _damage <= 0) return;
            int count = Physics2D.OverlapCircleNonAlloc(transform.position, _radius, _hits);
            for (int i = 0; i < count; i++)
            {
                var col = _hits[i];
                if (col == null) continue;
                var er = col.GetComponent<EntityRef>();
                if (er == null || er.Entity == null) continue;
                bool isEnemy = er.Entity is EnemyCharacter;
                bool isPlayer = er.Entity is PlayerCharacter;
                if (_ownerIsPlayer && !isEnemy) continue;
                if (!_ownerIsPlayer && !isPlayer) continue;
                // Route through legacy HP system + framework character damage.
                if (er.Entity is CharacterBase ch && !ch.IsDead) ch.TakeDamage(_damage);
                if (_ownerIsPlayer)
                {
                    var root = col.GetComponentInParent<LuzartEnemyEntityRoot>();
                    if (root != null) root.TakeDamage((float)_damage);
                }
            }
        }
    }
}
