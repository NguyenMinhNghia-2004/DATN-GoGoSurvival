using System.Buffers;
using UnityEngine;
namespace Luzart
{
    /// <summary>
    /// Entity representing a dropped item that can be picked up
    /// Follows the same pattern as ProjectileEntity
    /// </summary>
    public abstract class DropEntity : EntityBase, IDrop
    {
        protected DropConfig _dropConfig;
        protected float _lifeTimer;
        // Entity lifecycle
        private bool _isPickedUp = false;
        // Behaviors
        protected RenderBehavior _renderBehavior;
        protected AnimationBehavior _animationBehavior;
        protected ColliderBehavior _colliderBehavior;
        protected CollisionHandlerBehavior collisionHandlerBehavior;
        protected DropManager _dropManager;
        protected IEntity _owner;
        public DropConfig Config => _dropConfig;
        public DropEntity(DropConfig config, IEntity owner) : base()
        {
            _dropConfig = config;
            _lifeTimer = config.LifeTime;
            _owner = owner;
            // Set initial position
            ConfigTransform(owner.Transform.Position.Value);
        }
        public override void Initialize()
        {
            base.Initialize();
            if (_dropConfig == null)
            {
                return;
            }
            Id = $"Drop_{_dropConfig.Id}_{GetHashCode()}";
            // Setup behaviors
            SetupBehaviors();
        }
        protected virtual void SetupBehaviors()
        {
            // Transform and Scale
            Transform.SetScale(_dropConfig.Scale);
            _colliderBehavior = new ColliderBehavior(this, SpatialLayer.Drop, _dropConfig.ColliderRadius);
            AddBehavior(_colliderBehavior);
            _renderBehavior = new RenderBehavior(this);
            AddBehavior(_renderBehavior);
            if (_dropConfig.Material != null)
            {
                _renderBehavior.Configure(_dropConfig.Material, SortingLayerRender.Items);
            }
            if (_dropConfig.AnimationConfig != null)
            {
                _animationBehavior = new AnimationBehavior(this);
                _animationBehavior.Configure(_dropConfig.AnimationConfig);
                AddBehavior(_animationBehavior);
            }
            _dropManager = MyDomain.Get<DropManager>();
        }
        public override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);
            // Update lifetime
            _lifeTimer -= dt;
            if (_lifeTimer <= 0f)
            {
                _isDead = true;
                return;
            }
        }
        public void OnPickup(IEntity picker)
        {
            if (_isPickedUp) return;
            _isPickedUp = true;
            OnPickupImpl(picker);
            OnDeath();
        }
        public bool CanBePickedUp(IEntity picker)
        {
            if (_isPickedUp) return false;
            return CanPickupImpl(picker);
        }
        public bool IsInPickupRange(IEntity entity)
        {
            if (entity?.Transform == null) return false;
            Vector3 entityPos = entity.Transform.Position.Value;
            Vector3 dropPos = Transform.Position.Value;
            float distance = Vector3.Distance(entityPos, dropPos);
            return distance <= _dropConfig.PickupRange;
        }
        public override void Terminate()
        {
            _isPickedUp = true;
            base.Terminate();
        }
        protected abstract void OnPickupImpl(IEntity picker);
        protected abstract bool CanPickupImpl(IEntity picker);
    }
    public interface IDrop : IEntity
    {
        DropConfig Config { get; }
        void OnPickup(IEntity picker);
        bool CanBePickedUp(IEntity picker);
        bool IsInPickupRange(IEntity entity);
    }
}