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
        {
            return new ZSkillBehavior_CreateProjectile(skill, this);
        }
    }
    public class ZSkillBehavior_CreateATKProjectile : ZSkillBehavior<ZSkillBehaviorConfig>
    {
        public ZSkillBehavior_CreateATKProjectile(IZSkill skill, ZSkillBehaviorConfig behaviorConfig) : base(skill, behaviorConfig)
        {
        }
    }
}