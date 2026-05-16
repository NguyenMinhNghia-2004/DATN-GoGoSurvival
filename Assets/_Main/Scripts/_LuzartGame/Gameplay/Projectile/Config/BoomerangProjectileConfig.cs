using System.Collections.Generic;
using UnityEngine;
namespace Luzart
{
    [CreateAssetMenu(fileName = "Projectile_Boomerang", menuName = "Luzart/Projectiles/Boomerang", order = 1)]
    public class BoomerangProjectileConfig : ProjectileConfig
    {
        [Header("Boomerang Flight Settings")]
        [SerializeField] private float _acceration = 0.5f;
        [SerializeField] private float _accerationReturn = 12f;
        [Header("Boomerang Rotation Settings")]
        [SerializeField] private float _rotationSpeed = 720f;
        [Header("Boomerang Damage Settings")]
        [SerializeField] private float _damageCooldown = 0.5f;
        public float Acceration => _acceration;
        public float DamageCooldown => _damageCooldown;
        public float RotationSpeed => _rotationSpeed;
        public float AccerationReturn => _accerationReturn;
        public override ProjectileEntity CreateProjectile(IEntity owner)
        {
            return new BoomerangProjectile(this, owner);
        }
    }
}