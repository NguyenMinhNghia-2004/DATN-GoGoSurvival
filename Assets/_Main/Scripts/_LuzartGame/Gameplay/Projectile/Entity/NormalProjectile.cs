using UnityEngine;
namespace Luzart
{
    /// <summary>
    /// NormalProjectile - Viên đạn bắn ra từ NormalSkill
    /// </summary>
    public class NormalProjectile : ProjectileEntity, INormalProjectile
    {
        protected MoveProjectileBehavior _moveProjectileBehavior;        
        public NormalProjectile(ProjectileConfig projectileConfig, IEntity owner) : base(projectileConfig, owner)
        {
        }
        public override void Initialize()
        {
            base.Initialize();
            _collisionHandlerBehavior = new ProjectileCollisionHandlerBehavior(this, _owner);
            AddBehavior(_collisionHandlerBehavior);
            _moveProjectileBehavior = new MoveProjectileBehavior(this);
            AddBehavior(_moveProjectileBehavior);
        }
        void INormalProjectile.SetDirectAndSpeedProjectile(double speed, Vector3 dir)
        {
            DoSetDirectAndSpeedProjectile(speed, dir);
        }
        private void DoSetDirectAndSpeedProjectile(double speed, Vector3 dir)
        {
            _moveProjectileBehavior.Speed = speed;
            _moveProjectileBehavior.Direction = dir;
        }
    }
    public interface INormalProjectile 
    {
        void SetDirectAndSpeedProjectile(double speed, Vector3 dir);
    }
}