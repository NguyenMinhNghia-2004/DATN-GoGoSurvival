using UnityEngine;
namespace Luzart
{
    /// <summary>
    /// Config for "fall from above" projectiles (Brick). Spawns the projectile at a position
    /// ABOVE the target (or random offset around the player when no target), with initial
    /// direction = down + gravity → drops onto the target.
    /// </summary>
    public sealed class ZSkillBehaviorConfig_Falling : ZSkillBehaviorConfig_Projectile
    {
        [Header("Falling Pattern")]
        [Tooltip("Height above the target at which the projectile spawns (world units).")]
        [SerializeField] private float _dropHeight = 5f;

        [Tooltip("When no target is found, brick falls at a random XY offset around the player " +
                 "within this radius. 0 = drop on player.")]
        [SerializeField] private float _randomFallbackRadius = 2.5f;

        public float DropHeight => _dropHeight;
        public float RandomFallbackRadius => _randomFallbackRadius;

        public override IZSkillBehavior CreateBehavior(IZSkill skill)
            => SpawnOrAdd<ZSkillBehavior_Falling, ZSkillBehaviorConfig_Falling>(skill, this);
    }
}
