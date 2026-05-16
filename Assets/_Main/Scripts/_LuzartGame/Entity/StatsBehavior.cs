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
        public INumber Get(StatType key)
        {
            if (!_statDefaultDict.ContainsKey(key))
            {
                return new Number(0);
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
