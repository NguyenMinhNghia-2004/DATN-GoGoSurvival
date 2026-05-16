using System.Security.Principal;
using UnityEngine;
namespace Luzart
{
    /// <summary>
    /// Player-specific collision handler
    /// </summary>
    public class PlayerCollisionHandlerBehavior : CollisionHandlerBehavior
    {
        private StatsBehavior _statsBehavior;
        public PlayerCollisionHandlerBehavior(IEntity owner) : base(owner)
        {
        }
        protected override void DoStart()
        {
            base.DoStart();
            _statsBehavior = Owner.GetBehavior<StatsBehavior>();
        }
        protected override void HandleCollisionWithEntity(IEntity hitEntity, ICollider hitCollider)
        {
            base.HandleCollisionWithEntity(hitEntity, hitCollider);
            HandleDropCollision(hitEntity, hitCollider);
            HandleEnemyContact(hitEntity, hitCollider);
        }
        private void HandleEnemyContact(IEntity hitEntity, ICollider hitCollider)
        {
            bool isEnemy = hitCollider.Layer == SpatialLayer.Enemies;
            bool isLayerEnemey = hitEntity is ICharacter;
            if (!isEnemy|| !isLayerEnemey) return;
            var enemy = hitEntity as ICharacter;
            if (Owner is CharacterBase playerCharacter)
            {
                double damage = playerCharacter.Stats.Get(StatType.ATK).Value;
                enemy.Stats.TakeDamage(damage);
            }
        }
        protected void OnDropCollected(IEntity pickup)
        {
        }
        private void HandleDropCollision(IEntity hitEntity ,ICollider hitCollider)
        {
            if (hitCollider.Layer == SpatialLayer.Drop && hitEntity is IDrop dropEntity)
            {
                OnDropCollected(hitEntity);
                if (dropEntity.CanBePickedUp(Owner) && dropEntity.IsInPickupRange(Owner))
                {
                    dropEntity.OnPickup(Owner);
                }
            }
        }
    }
}