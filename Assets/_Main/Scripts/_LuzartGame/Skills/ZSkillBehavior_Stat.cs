using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Luzart
{
    public class ZSkillBehavior_Stat : ZSkillBehavior<ZSkillBehaviorConfig_Stat>
    {
        private StatsBehavior _statBehavior;
        public ZSkillBehavior_Stat(IZSkill skill, ZSkillBehaviorConfig_Stat behaviorConfig) : base(skill, behaviorConfig)
        {
            _statBehavior = _owner.GetBehavior<StatsBehavior>();
        }
        protected override void DoStart()
        {
            base.DoStart();
            UpgradeStat();
        }
        private void UpgradeStat()
        {
            if (_zSkillUpgradeConfig == null || _statBehavior == null) return;
            var hpMaxNow = _statBehavior.Get(StatType.HPMax);
            var valueHP = _zSkillUpgradeConfig.GetStatCalculator(StatType.Runtime_HP);
            {
                _statBehavior.AddHP(valueHP);
            }
            var gold = _statBehavior.GetRuntime(StatType.Runtime_Gold);
            var valueGold = _zSkillUpgradeConfig.GetStatCalculator(StatType.Runtime_Gold);
            if (valueGold > 0)
            {
                _statBehavior.AddGold(valueGold);
            }
        }
    }
}
