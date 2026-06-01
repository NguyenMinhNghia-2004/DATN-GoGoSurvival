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
        [Tooltip("When true, projectile explodes on contacting an enemy (rocket-style). " +
                 "When false (default), only ground-landing triggers explosion (bomb-style).")]
        [SerializeField] private bool _explodeOnImpact = false;
        public float Gravity => _gravity;
        public RenderConfig ExplosionEffect => _explosionEffect;
        public float MaxDistance => _maxDistance;
        public bool ExplodeOnImpact => _explodeOnImpact;
        public override ProjectileEntity CreateProjectile(IEntity owner)
        {
            return new BombProjectile(this, owner);
        }
    }
}
