using UnityEngine;
namespace Luzart
{
    /// <summary>
    /// Orbit pattern config (Guardian). Behavior spawns N "tops" that rotate around the
    /// player at a configured radius + angular velocity. Tops persist while skill active
    /// (no LifeTime expiry); count + spin speed adjust on level-up.
    /// </summary>
    public sealed class ZSkillBehaviorConfig_Orbit : ZSkillBehaviorConfig_Projectile
    {
        [Header("Orbit")]
        [Tooltip("Distance from owner at which the tops orbit (world units).")]
        [SerializeField] private float _orbitRadius = 1.5f;

        [Tooltip("Base degrees-per-second angular speed. Skill FireSpeed stat multiplies this.")]
        [SerializeField] private float _baseAngularSpeed = 90f;

        public float OrbitRadius => _orbitRadius;
        public float BaseAngularSpeed => _baseAngularSpeed;

        public override IZSkillBehavior CreateBehavior(IZSkill skill)
            => SpawnOrAdd<ZSkillBehavior_Orbit, ZSkillBehaviorConfig_Orbit>(skill, this);
    }
}
