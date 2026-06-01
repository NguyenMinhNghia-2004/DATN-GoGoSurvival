using UnityEngine;
namespace Luzart
{
    public class ZSkillBehaviorConfig_Lighting : ZSkillBehaviorConfig_Projectile
    {
        public override IZSkillBehavior CreateBehavior(IZSkill skill)
            => SpawnOrAdd<ZSkillBehavior_Lighting, ZSkillBehaviorConfig_Lighting>(skill, this);
    }
}
