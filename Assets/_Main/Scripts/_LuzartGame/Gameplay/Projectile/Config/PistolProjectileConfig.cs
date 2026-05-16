using System.Collections.Generic;
using UnityEngine;
namespace Luzart
{
    [CreateAssetMenu(fileName = "Projectile_Pistol", menuName = "Luzart/Projectiles/Pistol")]
    public class PistolProjectileConfig : ProjectileConfig
    {        
        [Header("Visual Effects")]
        [SerializeField] private RenderConfig renderConfig; // Optional explosion effect
        public RenderConfig RenderConfig => renderConfig;
        public override ProjectileEntity CreateProjectile(IEntity owner)
        {
            return new PistolProjectile(this, owner);
        }
    }
}