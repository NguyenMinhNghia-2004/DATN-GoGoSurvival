using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Luzart
{
    public class ZSkillBehaviorConfig_Bomb : ZSkillBehaviorConfig_Projectile
    {
        public override IZSkillBehavior CreateBehavior(IZSkill skill)
        {
            return new ZSkillBehavior_Bomb(skill, this);
        }
    }
}
