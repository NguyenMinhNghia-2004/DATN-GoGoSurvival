using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Luzart
{
       [CreateAssetMenu(fileName = "Projectile_Laser", menuName = "Luzart/Projectiles/Laser")]
    public class LaserProjectileConfig : ProjectileConfig
    {        
        public override ProjectileEntity CreateProjectile(IEntity owner)
        {
            return new LaserProjectile(this, owner);
        }
    }
}
