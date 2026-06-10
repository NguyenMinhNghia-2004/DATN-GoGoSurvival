using UnityEngine;
namespace Luzart
{
    /// <summary>
    /// Projectile that bounces off camera edges (and optionally enemies). Used by
    /// Drill Shot (pierce + edge bounce + re-launch), Durian (bounce + knockback),
    /// and Soccer Ball (bounce on enemies). 3 variants via flags rather than 3 classes.
    /// </summary>
    [CreateAssetMenu(fileName = "BouncingProjectile", menuName = "Luzart/Projectiles/Bouncing Projectile")]
    public class BouncingProjectileConfig : ProjectileConfig
    {
        [Header("Bouncing")]
        [Tooltip("Max screen-edge bounces before the projectile dies. -1 = infinite (LifeTime governs).")]
        [SerializeField] private int _maxBounces = -1;

        [Tooltip("True: passes through enemies (Drill). False: bounces off them (Durian/Soccer).")]
        [SerializeField] private bool _pierceEnemies = false;

        [Tooltip("Apply knockback impulse on enemy hit (Durian).")]
        [SerializeField] private bool _knockbackOnHit = false;

        [Tooltip("Knockback magnitude in world units (Durian).")]
        [SerializeField] private float _knockbackForce = 1.5f;

        [Tooltip("Per-entity damage cooldown so a piercing/bouncing projectile doesn't hit the " +
                 "same enemy every frame. Seconds.")]
        [SerializeField] private float _damageCooldown = 0.3f;

        [Tooltip("Rotate the projectile to face its movement direction.")]
        [SerializeField] private bool _rotateTowardsDirection = false;

        public int MaxBounces => _maxBounces;
        public bool PierceEnemies => _pierceEnemies;
        public bool KnockbackOnHit => _knockbackOnHit;
        public float KnockbackForce => _knockbackForce;
        public float DamageCooldown => _damageCooldown;
        public bool RotateTowardsDirection => _rotateTowardsDirection;

        public override ProjectileEntity CreateProjectile(IEntity owner)
            => new BouncingProjectile(this, owner);
    }
}
