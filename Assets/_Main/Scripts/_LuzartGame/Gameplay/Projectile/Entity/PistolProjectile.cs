using UnityEngine;
namespace Luzart
{
    public class PistolProjectile : NormalProjectile
    {
        private PistolProjectileConfig _pistolConfig;
        private TargetProvider _targetProvider;
        public PistolProjectile(ProjectileConfig projectileConfig, IEntity owner) : base(projectileConfig, owner)
        {
            _pistolConfig = projectileConfig as PistolProjectileConfig;
        }
        public override void Initialize()
        {
            base.Initialize();
            _targetProvider = MyDomain.Get<TargetProvider>();
        }
        public override void OnDeath()
        {
            base.OnDeath();
            ApplySplashDamage(Transform.Position.Value);
        }
        public void ApplySplashDamage(Vector3 impactPoint)
        {
            if (_pistolConfig == null) return;
            // Get my current damage
            var statsBehavior = GetBehavior<StatsBehavior>();
            double mainDamage = statsBehavior.Get(StatType.ATK).Value;
            double splashDamage = mainDamage * _statBehavior.Get(StatType.ATKMultiplierExplosion).Value;
            using var poolEntity = new ListPoolHelper<IEntity>(null);
            var results = poolEntity.List;
            SpatialLayer spatial = SpatialLayer.Enemies;
            if (_owner is PlayerCharacter)
            {
                spatial = SpatialLayer.Projectiles_Player;
            }
            float radius = (float)_statBehavior.Get(StatType.RadiusExplosion).Value;
            _targetProvider.GetTargetsInRange(Transform.Position.Value, radius, spatial, results);
            _pistolConfig.RenderConfig.CreateRender(_owner.MyDomain, impactPoint);
            foreach (var entity in results)
            {
                if (entity is CharacterBase character && !character.IsDead)
                {
                    float distance = Vector3.Distance(impactPoint, character.Transform.Position.Value);
                    float damageMultiplier = Mathf.Clamp01(1f - (distance / radius));
                    double finalDamage = splashDamage * damageMultiplier;
                    character.TakeDamage(finalDamage);
                }
            }
        }
    }
}