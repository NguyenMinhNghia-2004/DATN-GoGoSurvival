namespace Luzart
{
    public class ZSkillBehaviorConfig_Stat : ZSkillBehaviorConfig
    {
        public override IZSkillBehavior CreateBehavior(IZSkill skill)
        {
            return new ZSkillBehavior_Stat(skill, this);
        }
    }
}
