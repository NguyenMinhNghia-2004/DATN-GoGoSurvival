using UnityEngine;
namespace Luzart
{
    public abstract class ZSkillBehaviorConfig_Projectile : ZSkillBehaviorConfig
    {
        [SerializeField] protected ProjectileConfig projectileConfig;
        public ProjectileConfig ProjectileConfig => projectileConfig;
    }
}