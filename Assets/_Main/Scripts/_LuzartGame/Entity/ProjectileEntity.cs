using Luzart;
using System.Collections.Generic;
using System.Threading;
using UnityEditor.SearchService;
using UnityEngine;
namespace Luzart
{
    /// <summary>
    /// Projectile entity implementation
    /// </summary>
    public class ProjectileEntity : EntityBase, IProjectile
    {
        protected ProjectileConfig _projectileConfig;
        protected IEntity _owner;
        protected RenderBehavior _renderBehavior;
        protected AnimationBehavior _animationBehavior;
        protected StatsBehavior _statBehavior;
        protected ColliderBehavior _colliderBehavior;
        protected ProjectileCollisionHandlerBehavior _collisionHandlerBehavior;
        protected CancellationTokenSource _cancellationTokenSource;
        protected float time = 0f;
        public ProjectileEntity(ProjectileConfig projectileConfig, IEntity owner)
        {
            this._projectileConfig = projectileConfig;
            this._owner = owner;
            // Set unique ID for projectile
            this.Id = $"Projectile_{projectileConfig.Id}_{UnityEngine.Random.Range(1000, 9999)}";
            _cancellationTokenSource = new CancellationTokenSource();
        }
        public override void Initialize()
        {
            base.Initialize();
            Transform.SetScale(_projectileConfig.Scale);
            _renderBehavior = new RenderBehavior(this);
            _renderBehavior.Configure(_projectileConfig.Material, SortingLayerRender.Projectiles);
            AddBehavior(_renderBehavior);
            _animationBehavior = new AnimationBehavior(this);
            AddBehavior(_animationBehavior);
            _animationBehavior.Configure(_projectileConfig.AnimationConfig);
            _statBehavior = new StatsBehavior(this);
            AddBehavior(_statBehavior);
            _statBehavior.Add(_projectileConfig.Stats);
            SpatialLayer spatial = SpatialLayer.Projectiles_Enemy;
            if (_owner is PlayerCharacter)
            {
                spatial = SpatialLayer.Projectiles_Player;
            }
            var number = _statBehavior.Get(StatType.RadiusCollider).Value;
            _colliderBehavior = new ColliderBehavior(this, spatial, number);
            AddBehavior(_colliderBehavior);
            time = 0f;
        }
        public override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);
            time += dt;
            if (time > _projectileConfig.LifeTime)
            {
                OnDeath();
            }
        }
        public virtual void OnCollision(IEntity hitEntity)
        {
            TakeDamage(hitEntity);
            OnDeath();
        }
        public virtual void TakeDamage(IEntity hitEntity)
        {
            if (hitEntity is CharacterBase character)
            {
                double atk = _statBehavior.Get(StatType.ATK).Value;
                if (atk > 0)
                {
                    character.TakeDamage(atk);
                }
            }
        }
        public override void Terminate()
        {
            base.Terminate();
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }
        }
    }
}