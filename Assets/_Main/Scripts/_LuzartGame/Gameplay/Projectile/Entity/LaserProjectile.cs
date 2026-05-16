using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Luzart
{
    public class LaserProjectile : NormalProjectile
    {
        private LaserProjectileConfig _laserConfig;
        public LaserProjectile(LaserProjectileConfig config, IEntity owner) : base(config, owner)
        {
            _laserConfig = config;
        }
        public override void Initialize()
        {
            base.Initialize();
            _collisionHandlerBehavior = new ProjectileCollisionHandlerBehavior(this, _owner);
            AddBehavior(_collisionHandlerBehavior);
        }
        public override void OnCollision(IEntity hitEntity)
        {
            TakeDamage(hitEntity);
        }
    }
}
