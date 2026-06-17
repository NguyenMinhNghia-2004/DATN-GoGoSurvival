using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Luzart
{
    public class BombProjectile : ProjectileEntity, INormalProjectile
    {
        private BombProjectileConfig _bombProjectileConfig;
        private MoveRigidbodyBehavior _moveRigidbodyBehavior;
        private TargetProvider _targetProvider;
        public BombProjectile(BombProjectileConfig bombProjectileConfig, IEntity owner) : base(bombProjectileConfig, owner)
        {
            this._bombProjectileConfig = bombProjectileConfig;
        }
        public override void Initialize()
        {
            base.Initialize();
            _targetProvider = MyDomain.Get<TargetProvider>();
            _moveRigidbodyBehavior = new MoveRigidbodyBehavior(this);
            AddBehavior(_moveRigidbodyBehavior);
            _collisionHandlerBehavior = new ProjectileCollisionHandlerBehavior(this, _owner);
            AddBehavior(_collisionHandlerBehavior);
        }
        public void SetDirectAndSpeedProjectile(double speed, Vector3 dir)
        {
            _moveRigidbodyBehavior.SetDirectAndSpeedProjectile(speed, dir, _bombProjectileConfig.Gravity);
            _moveRigidbodyBehavior.SetMaxDistance(_bombProjectileConfig.MaxDistance);
            _moveRigidbodyBehavior.OnHitGround = OnHitGround;

            if (_bombProjectileConfig.RotateTowardsDirection)
            {
                // MoveRigidbodyBehavior now moves in +dir
                Vector3 actualDir = dir;
                float angle = Mathf.Atan2(actualDir.y, actualDir.x) * Mathf.Rad2Deg;
                Transform.SetRotation(Quaternion.Euler(0f, 0f, angle - 90f));
            }
        }
        public void SetParabolaProjectile(Vector2 startPos, Vector2 targetPos, float time)
        {
            float T = time;
            float g = _bombProjectileConfig.Gravity;
            float vx = (targetPos.x - startPos.x) / T;
            float vy = (targetPos.y - startPos.y) / T + 0.5f * g * T;
            _moveRigidbodyBehavior.SetParabola(startPos, targetPos, vx, vy, time, g);
            _moveRigidbodyBehavior.OnHitGround = OnHitGround;
        }
        public void OnHitGround()
        {
            float delayExplosion = (float)_statBehavior.Get(StatType.TimeDelayExplosion).Value;
            UniTask.WaitForSeconds(delayExplosion, cancellationToken: _cancellationTokenSource.Token).ContinueWith(() =>
            {
                Explode();
            });
        }
        public override void OnCollision(IEntity hitEntity)
        {
            // RPG-style: impact explosion (config flag). Otherwise classic bomb that
            // only damages the direct hit and lets ground-landing trigger the AoE.
            if (_bombProjectileConfig.ExplodeOnImpact)
            {
                TakeDamage(hitEntity);
                Explode();
                return;
            }
            TakeDamage(hitEntity);
        }
        public void Explode()
        {
            // Create explosion effect
            if (_bombProjectileConfig.ExplosionEffect != null)
            {
                _bombProjectileConfig.ExplosionEffect.CreateRender(_owner.MyDomain, Transform.Position.Value);
            }
            using var poolEntity = new ListPoolHelper<IEntity>(null);
            var results = poolEntity.List;
            SpatialLayer spatial = SpatialLayer.Enemies;
            if (_owner is PlayerCharacter)
            {
                spatial = SpatialLayer.Projectiles_Player;
            }
            float explosionRadius = (float)_statBehavior.Get(StatType.RadiusExplosion).Value;
            _targetProvider.GetTargetsInRange(Transform.Position.Value, explosionRadius, spatial, results);
            foreach (var entity in results)
            {
                if (entity is CharacterBase character && !character.IsDead)
                {
                    float distance = Vector3.Distance(Transform.Position.Value, character.Transform.Position.Value);
                    float damageMultiplier = Mathf.Clamp01(1f - (distance / explosionRadius));
                    double finalDamage = _statBehavior.Get(StatType.ATK).Value * damageMultiplier;
                    character.TakeDamage(finalDamage);
                }
            }
            OnDeath();
        }
    }
}
