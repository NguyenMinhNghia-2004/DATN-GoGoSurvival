using System.Collections.Generic;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Projectile that reflects direction on camera edges (and optionally on enemy hits).
    /// Drives motion via <see cref="MoveBouncingBehavior"/> instead of the straight-line
    /// MoveProjectileBehavior. Per-entity damage cooldown prevents bouncing/piercing
    /// projectiles from spamming damage on the same enemy every frame.
    /// </summary>
    public class BouncingProjectile : ProjectileEntity, INormalProjectile
    {
        private readonly BouncingProjectileConfig _bouncingConfig;
        private MoveBouncingBehavior _move;
        private readonly Dictionary<IEntity, float> _lastHit = new Dictionary<IEntity, float>();

        public BouncingProjectile(BouncingProjectileConfig cfg, IEntity owner) : base(cfg, owner)
        {
            _bouncingConfig = cfg;
        }

        public override void Initialize()
        {
            base.Initialize();
            _collisionHandlerBehavior = new ProjectileCollisionHandlerBehavior(this, _owner);
            AddBehavior(_collisionHandlerBehavior);
            _move = new MoveBouncingBehavior(this, _bouncingConfig);
            AddBehavior(_move);
        }

        void INormalProjectile.SetDirectAndSpeedProjectile(double speed, Vector3 dir)
        {
            _move.Speed = speed;
            _move.Direction = dir;
        }

        public override void OnCollision(IEntity hitEntity)
        {
            // Per-entity damage cooldown — required for pierce/bounce so we don't
            // tick damage every frame while overlapping the same target.
            if (_lastHit.TryGetValue(hitEntity, out float t)
                && Time.time - t < _bouncingConfig.DamageCooldown)
            {
                return;
            }
            _lastHit[hitEntity] = Time.time;
            TakeDamage(hitEntity);

            // Variant flags:
            //  - Drill (pierce=true): pass through enemy, no reflection
            //  - Durian/Soccer (pierce=false): reflect direction back from enemy
            if (!_bouncingConfig.PierceEnemies)
            {
                _move.ReflectFromEnemy(hitEntity);
            }
            // Knockback (Durian)
            if (_bouncingConfig.KnockbackOnHit && hitEntity is CharacterBase ch)
            {
                Vector3 push = (ch.Transform.Position.Value - Transform.Position.Value);
                push = ((Vector2)push).normalized * _bouncingConfig.KnockbackForce;
                ch.Transform.SetPosition(ch.Transform.Position.Value + (Vector2)push);
            }
            // Note: NO OnDeath() here — projectile continues bouncing.
        }
    }
}
