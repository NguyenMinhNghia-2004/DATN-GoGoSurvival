namespace Luzart
{
    /// <summary>
    /// Enemy-specific collision handler
    /// </summary>
    public class EnemyCollisionHandlerBehavior : CollisionHandlerBehavior
    {
        public EnemyCollisionHandlerBehavior(IEntity owner) : base(owner)
        {
        }
        protected override void HandleCollisionWithEntity(IEntity hitEntity, ICollider hitCollider)
        {
            base.HandleCollisionWithEntity(hitEntity, hitCollider);
            bool isPlayer = hitCollider.Layer == SpatialLayer.Player;
            bool isInterfacceCharacter = hitEntity is ICharacter;
            if (!isPlayer || !isInterfacceCharacter) return;
            var enemy = hitEntity as ICharacter;
            if (Owner is CharacterBase playerCharacter)
            {
                double damage = playerCharacter.Stats.Get(StatType.ATK).Value;
                enemy.Stats.TakeDamage(damage);
            }
        }
    }
}