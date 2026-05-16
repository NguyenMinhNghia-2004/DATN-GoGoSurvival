using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Luzart
{
    public class ZSkillBehavior_AddStat : ZSkillBehavior<ZSkillBehaviorConfig_AddStat>
    {
        private StatsBehavior _statBehavior;
        public ZSkillBehavior_AddStat(IZSkill skill, ZSkillBehaviorConfig_AddStat behaviorConfig) : base(skill, behaviorConfig)
        {
        }
        protected override void DoStart()
        {
            base.DoStart();
            //ReplaceStat();
        }
        //private List<StatCalculator> _oldStatCalculators = new List<StatCalculator>();
        //private void ReplaceStat()
        //{
        //    _statBehavior = _owner.GetBehavior<StatsBehavior>();
        //    int preLevelIndex = (int)_skill.LevelIndex.Value - 1;
        //    if (preLevelIndex >= 0)
        //    {
        //        _oldStatCalculators = _skill.Config.UpgradeConfigs[preLevelIndex].StatCalculators;
        //    }
        //    _statBehavior.ReplaceStat(_oldStatCalculators, _zSkillUpgradeConfig.StatCalculators);
        //}
        //protected override void RefreshNumbers()
        //{
        //    base.RefreshNumbers();
        //    ReplaceStat();
        //}
    }
}
