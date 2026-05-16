using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Luzart
{
    public class LightingTargetProjectile : ProjectileEntity,ITargetProjectile
    {
        public LightingTargetProjectile(LightingTargetProjectileConfig projectileConfig, IEntity owner) : base(projectileConfig, owner)
        {
        }
        private IEntity _target;
        private MoveTargetBehavior _moveTargetBehavior;
        public void SetTarget(IEntity target)
        {
            this._target = target;
            _moveTargetBehavior.SetTarget(target);
        }
        public void SetSpeed(float speed)
        {
            _moveTargetBehavior.SetSpeed(speed);
        }
        public override void Initialize()
        {
            base.Initialize();
            _collisionHandlerBehavior = new ProjectileCollisionHandlerBehavior(this, _owner);
            AddBehavior(_collisionHandlerBehavior);
            _moveTargetBehavior = new MoveTargetBehavior(this);
            AddBehavior(_moveTargetBehavior);
        }
        public override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);
            if(_target != null && _target.IsDead)
            {
                OnDeath();
            }
        }
    }
}
