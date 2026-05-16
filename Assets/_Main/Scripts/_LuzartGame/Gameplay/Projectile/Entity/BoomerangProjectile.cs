using Luzart;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Luzart
{
    public class BoomerangProjectile : ProjectileEntity, IBoomerangProjectile
    {
        private BoomerangProjectileConfig _boomerangConfig;
        private Dictionary<IEntity, float> _lastDamageTime = new Dictionary<IEntity, float>();
        private MoveProjectileBoomerangBehavior _moveProjectileBoomerangBehavior;
        public BoomerangProjectile(BoomerangProjectileConfig projectileConfig, IEntity owner) : base(projectileConfig, owner)
        {
            _boomerangConfig = projectileConfig;
        }
        public override void Initialize()
        {
            base.Initialize();
            _collisionHandlerBehavior = new ProjectileCollisionHandlerBehavior(this, _owner);
            AddBehavior(_collisionHandlerBehavior);
            // Add boomerang movement behavior with proper owner reference
            _moveProjectileBoomerangBehavior = new MoveProjectileBoomerangBehavior(this, _boomerangConfig, _owner);
            AddBehavior(_moveProjectileBoomerangBehavior);
        }
        public override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);
            CleanupDamageCooldowns();
        }
        public override void OnCollision(IEntity hitEntity)
        {
            // Don't destroy on collision, just apply damage if cooldown allows
            if (CanDamageEntity(hitEntity))
            {
                TakeDamage(hitEntity);
                _lastDamageTime[hitEntity] = Time.time;
            }
        }
        private bool CanDamageEntity(IEntity entity)
        {
            if (!_lastDamageTime.ContainsKey(entity))
            {
                return true;
            }
            float timeSinceLastDamage = Time.time - _lastDamageTime[entity];
            return timeSinceLastDamage >= _boomerangConfig.DamageCooldown;
        }
        private void CleanupDamageCooldowns()
        {
            var keysToRemove = new List<IEntity>();
            foreach (var kvp in _lastDamageTime)
            {
                if (kvp.Key == null || kvp.Key.IsDead || 
                    Time.time - kvp.Value > _boomerangConfig.DamageCooldown * 2f)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            foreach (var key in keysToRemove)
            {
                _lastDamageTime.Remove(key);
            }
        }
        void INormalProjectile.SetDirectAndSpeedProjectile(double speed, Vector3 dir)
        {
            _moveProjectileBoomerangBehavior.SetDirectAndSpeedProjectile(speed, dir);
        }
    }
    public interface IBoomerangProjectile : INormalProjectile
    {
    }
}
