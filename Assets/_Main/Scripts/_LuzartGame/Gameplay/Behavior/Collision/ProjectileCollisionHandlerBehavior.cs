using UnityEngine;
namespace Luzart
{
    /// <summary>
    /// Projectile-specific collision handler
    /// </summary>
    public class ProjectileCollisionHandlerBehavior : CollisionHandlerBehavior
    {
        private ProjectileEntity _projectileEntity;
        private IEntity _projectileOwner;
        public ProjectileCollisionHandlerBehavior(IEntity _projectile, IEntity _character) : base(_projectile)
        {
            _projectileEntity = _projectile as ProjectileEntity;
            _projectileOwner = _character;
        }
        protected override void OnCollisionDetected(ICollider self, ICollider other)
        {
            base.OnCollisionDetected(self, other);
        }
        protected override void HandleCollisionWithEntity(IEntity hitEntity, ICollider hitCollider)
        {
            CollisionHit(hitEntity);
        }
        private void CollisionHit(IEntity hitEntity)
        {
            _projectileEntity.OnCollision(hitEntity);
        }
    }
}