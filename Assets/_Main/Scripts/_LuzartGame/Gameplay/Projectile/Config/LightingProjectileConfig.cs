using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace Luzart
{
    [CreateAssetMenu(fileName = "Projectile_Lighting", menuName = "Luzart/Projectiles/LightingProjectile", order = 0)]
    public class LightingProjectileConfig : ProjectileConfig
    {
        [SerializeField] private RenderConfig _explosionEffect;
        [SerializeField] private float _lightningRadiusFind = 5f;
        [Space, Header("Lighting Projectile Other")]
        [SerializeField] private ProjectileConfig _projectileConfigOther;
        [SerializeField] private float _speedProjectileOther;
        [SerializeField] private int _amount = 3;
        public ProjectileConfig ProjectileConfigOther=> _projectileConfigOther;
        public float LightningRadiusFind => _lightningRadiusFind;
        public int Amount => _amount;
        public RenderConfig ExplosionEffect => _explosionEffect;
        public float SpeedProjectileOther => _speedProjectileOther;
        public override ProjectileEntity CreateProjectile(IEntity owner)
        {
            return new LightingProjectile(this, owner);
        }
        public ZSkillUpgradeConfig ZSkillUpgradeConfig { get; private set; }
        public override void InitStat(IEntity owner, ZSkillUpgradeConfig zUpgrade)
        {
            this.ZSkillUpgradeConfig = zUpgrade;
            base.InitStat(owner, zUpgrade);
        }
    }
}
