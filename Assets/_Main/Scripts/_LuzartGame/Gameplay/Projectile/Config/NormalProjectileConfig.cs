using System.Collections.Generic;
using UnityEngine;
namespace Luzart
{
    [CreateAssetMenu(fileName = "NormalProjectile", menuName = "Luzart/Projectiles/Normal Projectile")]
    public class NormalProjectileConfig : ProjectileConfig
    {
        public override ProjectileEntity CreateProjectile(IEntity owner)
        {
            return new NormalProjectile(this, owner);
        }
    }
}