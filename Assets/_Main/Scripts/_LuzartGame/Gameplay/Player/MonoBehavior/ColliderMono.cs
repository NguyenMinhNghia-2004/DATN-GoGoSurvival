using UnityEngine;
namespace Luzart
{
    public class ColliderMono : AbstractMonoBehaviorContent, IEntityBehaviorProvider
    {
        [SerializeField]
        private float radius = 0.5f;
        private IEntity _entity;
        private EntityBluePrint _entityBluePrint;
        public void CreateBehavior(IEntity entity)
        {
            _entity = entity;
            var colliderBehavior = new ColliderBehavior(entity, SpatialLayer.Player, radius);
            entity.AddBehavior(colliderBehavior);
            var collisionHandler = new PlayerCollisionHandlerBehavior(entity);
            entity.AddBehavior(collisionHandler);
        }
        public void InitEntityBluePrint(EntityBluePrint entity)
        {
            _entityBluePrint = entity;
        }
    }
}