using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace Luzart
{
    [CreateAssetMenu(fileName = "Projectile_Bomb", menuName = "Luzart/Projectiles/Bomb", order = 0)]
    public class BombProjectileConfig : ProjectileConfig
    {
        [SerializeField] private float _gravity = 20f;
        [SerializeField] private float _maxDistance = 4f;
        [SerializeField] private RenderConfig _explosionEffect;
        public float Gravity => _gravity;
        public RenderConfig ExplosionEffect => _explosionEffect;
        public float MaxDistance => _maxDistance;
        public override ProjectileEntity CreateProjectile(IEntity owner)
        {
            return new BombProjectile(this, owner);
        }
    }
}
