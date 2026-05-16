using UnityEngine;
namespace Luzart
{
    public class CollisionHandlerBehavior : BehaviorBase
    {
        protected Mapping _mapping;
        protected ColliderBehavior _colliderBehavior;
        public CollisionHandlerBehavior(IEntity owner) : base(owner)
        {
        }
        protected override void DoStart()
        {
            base.DoStart();
            _mapping = MyDomain.Get<Mapping>();
            _colliderBehavior = Owner.GetBehavior<ColliderBehavior>();
            if (_colliderBehavior?.Collider != null)
            {
                _colliderBehavior.Collider.OnCollideWith += OnCollisionDetected;
            }
            else
            {
                Debug.LogWarning($"[CollisionHandlerBehavior] No ColliderBehavior found on {Owner.Id}");
            }
        }
        protected virtual void OnCollisionDetected(ICollider self, ICollider other)
        {
            // Get the entity that collided with this entity
            var hitEntity = _mapping.FindEntityWithCollider(other);
            if (hitEntity == null) return;
            // Delegate to specific collision handlers
            HandleCollisionWithEntity(hitEntity, other);
        }
        protected virtual void HandleCollisionWithEntity(IEntity hitEntity, ICollider hitCollider)
        {
        }
        protected override void DoDestroy()
        {
            // Unsubscribe from events
            if (_colliderBehavior?.Collider != null)
            {
                _colliderBehavior.Collider.OnCollideWith -= OnCollisionDetected;
            }
            base.DoDestroy();
        }
    }
}