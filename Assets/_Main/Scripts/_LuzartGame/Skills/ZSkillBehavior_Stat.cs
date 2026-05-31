using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Luzart
{
    /// <summary>
    /// Behavior for `eTypeSkill = Stat` (passive) skills. Applies the current
    /// upgrade config's stats to the owner's StatsBehavior on Start / level-up.
    ///
    /// Delta-tracked: stores the factor applied last call so a level-up can
    /// undo the previous tier and apply the new one without stacking.
    ///
    /// Stat-type semantics (matches GDD passive table):
    ///   HPMax/ATK/Speed/FireSpeed/RangeFind/XPMultiplier/Luck → percent-multiply (+x%)
    ///   Cooldown → percent-subtract (-x% cooldown = faster fire)
    ///   Armor → additive (damage reduction stacks)
    ///   Heal → additive (regen rate adds)
    ///   Runtime_HP / Runtime_Gold → still handled by the original AddHP/AddGold path
    /// </summary>
    public class ZSkillBehavior_Stat : ZSkillBehavior<ZSkillBehaviorConfig_Stat>
    {
        private StatsBehavior _statBehavior;

        // Per-stat-type bonus we previously applied; lets us cleanly remove on level-up.
        private readonly Dictionary<StatType, double> _appliedBonus = new();

        // Map (StatType → BonusMode) — drives runtime behavior of ApplyStatBonus.
        private static readonly (StatType type, StatsBehavior.StatBonusMode mode)[] s_passiveMappings =
        {
            (StatType.HPMax,             StatsBehavior.StatBonusMode.PercentMultiply),
            (StatType.ATK,               StatsBehavior.StatBonusMode.PercentMultiply),
            (StatType.Speed,             StatsBehavior.StatBonusMode.PercentMultiply),
            (StatType.FireSpeed,         StatsBehavior.StatBonusMode.PercentMultiply),
            (StatType.RangeFind,         StatsBehavior.StatBonusMode.PercentMultiply),
            (StatType.PhanTramXPTangLen, StatsBehavior.StatBonusMode.PercentMultiply),
            (StatType.Luck,              StatsBehavior.StatBonusMode.PercentMultiply),
            (StatType.Cooldown,          StatsBehavior.StatBonusMode.PercentSubtract),
            (StatType.Armor,             StatsBehavior.StatBonusMode.Additive),
            (StatType.Heal,              StatsBehavior.StatBonusMode.Additive),
        };

        public ZSkillBehavior_Stat(IZSkill skill, ZSkillBehaviorConfig_Stat behaviorConfig) : base(skill, behaviorConfig)
        {
            _statBehavior = _owner.GetBehavior<StatsBehavior>();
        }

        protected override void DoStart()
        {
            base.DoStart();
            UpgradeStat();
        }

        // Called by base ZSkillBehavior.RefreshNumbers after level-up.
        protected override void RefreshNumbers()
        {
            base.RefreshNumbers();
            UpgradeStat();
        }

        private void UpgradeStat()
        {
            if (_zSkillUpgradeConfig == null || _statBehavior == null) return;

            // --- Existing runtime stat-deltas (HP and Gold drops) ---
            var valueHP = _zSkillUpgradeConfig.GetStatCalculator(StatType.Runtime_HP);
            if (valueHP > 0) _statBehavior.AddHP(valueHP);
            var valueGold = _zSkillUpgradeConfig.GetStatCalculator(StatType.Runtime_Gold);
            if (valueGold > 0) _statBehavior.AddGold(valueGold);

            // --- Passive stat bonuses (delta-tracked) ---
            foreach (var (type, mode) in s_passiveMappings)
            {
                double newFactor = _zSkillUpgradeConfig.GetStatCalculator(type);
                _appliedBonus.TryGetValue(type, out double oldFactor);

                if (oldFactor != 0)
                    _statBehavior.RemoveStatBonus(type, oldFactor, mode);

                if (newFactor != 0)
                {
                    _statBehavior.ApplyStatBonus(type, newFactor, mode);
                    _appliedBonus[type] = newFactor;
                }
                else
                {
                    _appliedBonus.Remove(type);
                }
            }
        }

        protected override void DoDispose()
        {
            // Undo any bonuses we applied (e.g. when run ends + skill is torn down).
            if (_statBehavior != null)
            {
                foreach (var kvp in _appliedBonus)
                {
                    var mapping = Array.Find(s_passiveMappings, m => m.type == kvp.Key);
                    _statBehavior.RemoveStatBonus(kvp.Key, kvp.Value, mapping.mode);
                }
                _appliedBonus.Clear();
            }
            base.DoDispose();
        }
    }
}
