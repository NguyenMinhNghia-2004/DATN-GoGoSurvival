namespace Luzart
{
    public class ZSkillBehaviorConfig_AddStat : ZSkillBehaviorConfig
    {
        public override IZSkillBehavior CreateBehavior(IZSkill skill)
            => SpawnOrAdd<ZSkillBehavior_AddStat, ZSkillBehaviorConfig_AddStat>(skill, this);
    }
}
