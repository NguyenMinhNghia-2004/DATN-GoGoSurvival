using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace Luzart
{
    public class LightingProjectile : ProjectileEntity, ITargetProjectile
    {
        private TargetProvider _targetProvider = null;
        private LightingProjectileConfig _lightingProjectileConfig;
        private MoveTargetBehavior _moveTargetBehavior;
        private EntityManager _entityManager;
        public LightingProjectile(LightingProjectileConfig projectileConfig, IEntity owner) : base(projectileConfig, owner)
        {
            _lightingProjectileConfig = projectileConfig;
        }
        public override void Initialize()
        {
            base.Initialize();
            _isTakeDamage = false;
            _targetProvider = MyDomain.Get<TargetProvider>();
            _entityManager = MyDomain.Get<EntityManager>();
            _collisionHandlerBehavior = new ProjectileCollisionHandlerBehavior(this, _owner);
            AddBehavior(_collisionHandlerBehavior);
            _moveTargetBehavior = new MoveTargetBehavior(this,true,true);
            AddBehavior(_moveTargetBehavior);
        }
        private bool _isTakeDamage = false;
        public override void OnCollision(IEntity hitEntity)
        {
            if(_isTakeDamage) return;
            TakeDamage(hitEntity);
            Explode();
            _isTakeDamage = true;
        }
        public override void Start()
        {
            base.Start();
        }
        private void Explode()
        {
            // Create explosion effect
            if (_lightingProjectileConfig.ExplosionEffect != null)
            {
                _lightingProjectileConfig.ExplosionEffect.CreateRender(_owner.MyDomain, Transform.Position.Value);
            }
            using var poolEntity = new ListPoolHelper<IEntity>(null);
            var results = poolEntity.List;
            SpatialLayer spatial = SpatialLayer.Enemies;
            if (!(_owner is PlayerCharacter))
            {
                spatial = SpatialLayer.Player;
            }
            _targetProvider.GetTargetsInRange(Transform.Position.Value, _lightingProjectileConfig.LightningRadiusFind, spatial, results);
            int length = _lightingProjectileConfig.Amount;
            int min = Mathf.Min(length, results.Count);
            for (int i = 0; i < min; i++)
            {
                var entity = results[i];
                if (entity is CharacterBase character && !character.IsDead)
                {
                   var projectile = SpawnLightingProjectile(character);
                    _entityManager.Add(projectile);
                }
            }
        }
        private ProjectileEntity SpawnLightingProjectile(IEntity target)
        {
            _lightingProjectileConfig.ProjectileConfigOther.InitStat(_owner, _lightingProjectileConfig.ZSkillUpgradeConfig);
            var lightingProjectile = _lightingProjectileConfig.ProjectileConfigOther.CreateProjectile(_owner);
            lightingProjectile.Inject(_owner.MyDomain);
            lightingProjectile.Initialize();
            lightingProjectile.Start();
            lightingProjectile.ConfigTransform(Transform.Position.Value);
            if(lightingProjectile is LightingTargetProjectile lightingTargetProjectile)
            {
                lightingTargetProjectile.SetTarget(target);
                lightingTargetProjectile.SetSpeed(_lightingProjectileConfig.SpeedProjectileOther);
            }
            return lightingProjectile;
        }
        public void SetTarget(IEntity target)
        {
           _moveTargetBehavior.SetTarget(target);
        }
        public void SetSpeed(float speed)
        {
            _moveTargetBehavior.SetSpeed(speed);
        }
    }
    public interface ITargetProjectile
    {
        void SetTarget(IEntity target);
        void SetSpeed(float speed);
    }
}
