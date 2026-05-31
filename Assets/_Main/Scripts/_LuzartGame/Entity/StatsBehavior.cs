using Luzart;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Luzart
{
    public class StatsBehavior : BehaviorBase
    {
        public StatsBehavior(IEntity owner) : base(owner)
        {
        }
        private Dictionary<StatType, INumber> _statDefaultDict = new();
        private static readonly HashSet<string> _nullStatWarned = new();
        //
        private Dictionary<StatType, INumberWithSet> _dictRuntime_Stat = new Dictionary<StatType, INumberWithSet>()
        {
            {StatType.Runtime_Gold, new Number(0) },
            {StatType.Runtime_XP, new Number(0) },
            {StatType.Runtime_HP, new Number(0) }
        };
        public void Add(IEnumerable<IStat> listStat)
        {
            _statDefaultDict.Clear();
            foreach (var stat in listStat)
            {
                if (stat.Value == null)
                {
                    string key = stat.Definition != null ? stat.Definition.ToString() : "<null-def>";
                    if (_nullStatWarned.Add(key))
                        Debug.LogWarning($"[StatsBehavior] Stat {key} has null value — fix the AssetStatDefinition asset (logged once).");
                    continue;
                }
                _statDefaultDict[stat.Definition.StatType] = stat.Value;
            }
        }
        // Fallback defaults when the AssetStats config doesn't define a stat.
        // Without these, HPMax=0 makes the HP bar always empty and TotalSkill=0
        // makes UpgradeSkillManager skip the level-up popup.
        private static readonly Dictionary<StatType, double> _missingStatFallback = new()
        {
            { StatType.HPMax, 100 },
            { StatType.TotalSkill, 3 },
            { StatType.ATK, 10 },
            { StatType.Speed, 5 },
        };

        public INumber Get(StatType key)
        {
            if (!_statDefaultDict.ContainsKey(key))
            {
                double fallback = 0;
                _missingStatFallback.TryGetValue(key, out fallback);
                return new Number(fallback);
            }
            return _statDefaultDict[key];
        }
        public INumberWithSet GetRuntime(StatType key)
        {
            if (!_dictRuntime_Stat.ContainsKey(key))
            {
                throw new Exception($" Khong co StatType {key}");
            }
            return _dictRuntime_Stat[key];
        }
        public void TakeDamage(double dmg)
        {
            var hp = GetRuntime(StatType.Runtime_HP);
            hp.Set(Math.Max(0, hp.Value - dmg));
        }
        public bool IsDead
        {
            get
            {
                return GetRuntime(StatType.Runtime_HP).Value > 0;
            }
        }
        public void RestoreHP()
        {
            var hp = GetRuntime(StatType.Runtime_HP);
            double value = Get(StatType.HPMax).Value;
            hp.Set(value);
        }
        public void AddHP(double amount)
        {
            var hp = GetRuntime(StatType.Runtime_HP);
            double newValue = Math.Min(Get(StatType.HPMax).Value, hp.Value + amount);
            hp.Set(newValue);
        }
        public void AddXP(double amount)
        {
            var xp = GetRuntime(StatType.Runtime_XP);
            double newValue = xp.Value + amount;
            xp.Set(newValue);
        }
        public void AddGold(double amount)
        {
            var gold = GetRuntime(StatType.Runtime_Gold);
            double newValue = gold.Value + amount;
            gold.Set(newValue);
        }

        // ──────────────────────────────────────────────────────────────────
        // Passive stat bonus API (2026-06-01).
        //
        // Used by `ZSkillBehavior_Stat.UpgradeStat()` to apply passive skill
        // effects (e.g. Fitness Guide +20% Max HP). Mutates the stored INumber
        // reference for the stat so subsequent `Get(key)` reads see the new
        // value. Caller is responsible for tracking + undoing previous level's
        // contribution before applying a new level.
        //
        // This is a *minimal* alternative to the full Luzart modifier pipeline
        // (RuntimeModifierFactorGroup + AssetModifier_ContributeToAggregatedNumber)
        // which would require authoring AssetModifier SOs per stat. The delta
        // semantics here match a simple "apply factor on top of current value"
        // model.
        // ──────────────────────────────────────────────────────────────────
        public enum StatBonusMode
        {
            Additive,           // newValue = current + factor
            PercentMultiply,    // newValue = current × (1 + factor)
            PercentSubtract,    // newValue = current × (1 - factor) — for cooldown
        }

        public void ApplyStatBonus(StatType key, double factor, StatBonusMode mode)
        {
            if (factor == 0) return;
            var current = Get(key);
            double oldValue = current.Value;
            double newValue = mode switch
            {
                StatBonusMode.Additive => oldValue + factor,
                StatBonusMode.PercentMultiply => oldValue * (1 + factor),
                StatBonusMode.PercentSubtract => oldValue * (1 - factor),
                _ => oldValue,
            };
            _statDefaultDict[key] = new Number(newValue);
            // Note: events on the old INumber instance won't fire because Action
            // events can't be invoked from outside the declaring type. UI reading
            // via Get(key) on demand will see the new value.
        }

        public void RemoveStatBonus(StatType key, double factor, StatBonusMode mode)
        {
            if (factor == 0) return;
            var current = Get(key);
            double oldValue = current.Value;
            double newValue = mode switch
            {
                StatBonusMode.Additive => oldValue - factor,
                StatBonusMode.PercentMultiply => oldValue / (1 + factor),
                StatBonusMode.PercentSubtract => oldValue / (1 - factor),
                _ => oldValue,
            };
            _statDefaultDict[key] = new Number(newValue);
            try { current.Changed?.Invoke(_statDefaultDict[key]); } catch { /* same */ }
        }

        protected override void DoUpdate(float dt)
        {
            base.DoUpdate(dt);
            double heal = Get(StatType.Heal).Value;
            if (heal > 0)
            {
                AddHP(heal * dt);
            }
        }
    }
}
