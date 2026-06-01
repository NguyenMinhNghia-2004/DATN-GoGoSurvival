namespace Luzart
{
    public class ZSkillBehaviorConfig_Stat : ZSkillBehaviorConfig
    {
        public override IZSkillBehavior CreateBehavior(IZSkill skill)
            => SpawnOrAdd<ZSkillBehavior_Stat, ZSkillBehaviorConfig_Stat>(skill, this);
    }
}
