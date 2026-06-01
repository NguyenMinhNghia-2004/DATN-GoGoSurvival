using UnityEngine;
namespace Luzart
{
    public interface IZSkillBehavior_ProjectileBuilder
    {
        void BuildProjectile(IProjectile projectile);
    }
    public sealed class ZSkillBehaviorConfig_CreateProjectile : ZSkillBehaviorConfig_Projectile
    {
        public override IZSkillBehavior CreateBehavior(IZSkill skill)
            => SpawnOrAdd<ZSkillBehavior_CreateProjectile, ZSkillBehaviorConfig_CreateProjectile>(skill, this);
    }

    public class ZSkillBehavior_CreateATKProjectile : ZSkillBehavior<ZSkillBehaviorConfig>
    {
        // Empty placeholder — Bind is inherited.
    }
}
