using UnityEngine;
namespace Luzart
{
    public class RenderEntity: EntityBase
    {
        private RenderConfig _renderConfig;
        private RenderBehavior _renderBehavior;
        private AnimationBehavior _animationBehavior;
        private EntityManager _entityManager;
        private Vector2 _position;
        private float _timer = 0f;
        public RenderEntity(RenderConfig renderConfig, Vector2 pos)
        {
            _renderConfig = renderConfig;
            _position = pos;
        }
        public override void Initialize()
        {
            base.Initialize();
            _entityManager = MyDomain.Get<EntityManager>();
            _renderBehavior = new RenderBehavior(this);
            AddBehavior(_renderBehavior);
            _animationBehavior = new AnimationBehavior(this);
            AddBehavior(_animationBehavior);
            _renderBehavior.SetMaterial(_renderConfig.Material, SortingLayerRender.Effects);
            _animationBehavior.Configure(_renderConfig.AnimationConfig);
            Transform.SetPosition(_position);
            Transform.SetScale(_renderConfig.Scale);
            _entityManager.Add(this);
        }
        public override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);
            _timer += dt;
            if (_timer > _renderConfig.LifeTime)
            {
                OnDeath();
            }
        }
    }
}