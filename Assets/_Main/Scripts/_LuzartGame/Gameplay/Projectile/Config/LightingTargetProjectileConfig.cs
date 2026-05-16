using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace Luzart
{
    [CreateAssetMenu(fileName = "Projectile_LightingTarget", menuName = "Luzart/Projectiles/LightingTargetProjectile", order = 0)]
    public class LightingTargetProjectileConfig : ProjectileConfig
    {
        public override ProjectileEntity CreateProjectile(IEntity owner)
        {
            return new LightingTargetProjectile(this, owner);
        }
    }
}
