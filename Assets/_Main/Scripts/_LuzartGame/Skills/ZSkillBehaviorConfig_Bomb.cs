using UnityEngine;
namespace Luzart
{
    public class ZSkillBehaviorConfig_Bomb : ZSkillBehaviorConfig_Projectile
    {
        public override IZSkillBehavior CreateBehavior(IZSkill skill)
            => SpawnOrAdd<ZSkillBehavior_Bomb, ZSkillBehaviorConfig_Bomb>(skill, this);
    }
}
