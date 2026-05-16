using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Luzart
{
    public class ZSkillBehaviorConfig_Lighting : ZSkillBehaviorConfig_Projectile
    {
        public override IZSkillBehavior CreateBehavior(IZSkill skill)
        {
            return new ZSkillBehavior_Lighting(skill, this);
        }
    }
}
