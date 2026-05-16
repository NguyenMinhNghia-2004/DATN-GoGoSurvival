using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Luzart
{
    public class ZSkillBehaviorConfig_AddStat : ZSkillBehaviorConfig
    {
        public override IZSkillBehavior CreateBehavior(IZSkill skill)
        {
            return new ZSkillBehavior_AddStat(skill,this);
        }
    }
}
