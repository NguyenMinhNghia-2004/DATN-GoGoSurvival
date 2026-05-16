namespace Luzart
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using Unity.VisualScripting.Antlr3.Runtime.Misc;
    using UnityEngine;
    public interface IProjectile : IEntity
    {
    }
    public interface IProjectileDefinitionProvider
    {
        public ProjectileEntity CreateProjectile(IEntity owner);
    }
    public abstract class ProjectileConfig : EntityConfigScriptableObject, IProjectileDefinitionProvider
    {
        [Header("Visuals")]
        [SerializeField] private AnimationConfig animationConfig;
        [Header("Stats")]
        [SerializeField] private List<AssetStatDefinition> statDefinitions;
        public AnimationConfig AnimationConfig => animationConfig;
        public abstract ProjectileEntity CreateProjectile(IEntity owner);
        private Dictionary<StatType,INumber> _statDictionary = new();
        public Dictionary<StatType, INumber> StatAfterCalculator => _statDictionary;
        private List<IStat> _stats = new();
        public List<IStat> Stats => _stats;
        public virtual void InitStat(IEntity owner, ZSkillUpgradeConfig zUpgrade)
        {
            _stats.Clear();
            _statDictionary.Clear();
            for (int i = 0; i < statDefinitions.Count; i++)
            {
                var statDefinition = statDefinitions[i];
                IStatDefinition iStatDefinition = statDefinition;
                var number = zUpgrade.GetStat(iStatDefinition.StatType);
                _statDictionary[iStatDefinition.StatType] = number;
                _stats.Add(new RuntimeStat(iStatDefinition, number));
            }
        }
        public INumber GetStat(StatType statType)
        {
            if(_statDictionary.TryGetValue(statType, out var number))
            {
                return number;
            }
            return new Number(0);
        }
    }
}