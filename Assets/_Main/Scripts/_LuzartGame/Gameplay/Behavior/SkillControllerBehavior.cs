using Luzart;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Luzart
{
    public class SkillControllerBehavior : BehaviorBase
    {
        private List<ZSkillConfig> _skillConfigs = new();
        private readonly List<IZSkill> _iZSKills = new();
        private readonly Dictionary<ZSkillConfig, IZSkill> _skillInstances = new();   
        private readonly Dictionary<ZSkillConfig, INumberWithSet> _skillLevels = new();
        public SkillControllerBehavior(IEntity owner) : base(owner)
        {
        }
        protected override void DoStart()
        {
            base.DoStart();
            try
            {
                ClearSkills();
                LoadSkillsFromConfigs();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SkillControllerBehavior] Error: {e}");
            }
        }
        protected override void DoUpdate(float dt)
        {
            if (Owner == null) return;
            foreach (var skill in _iZSKills)
            {
                skill.Update(dt);
            }
        }
        private void ClearSkills()
        {
            foreach(var skill in _skillInstances)
            {
                skill.Value.OnDestroy();
            }
            _skillInstances.Clear();
        }
        private void LoadSkillsFromConfigs()
        {
            foreach (var config in _skillConfigs)
            {
                if (config == null)
                {
                    Debug.LogWarning("[SkillController] Null skill config found, skipping...");
                    continue;
                }
                CreateSkillFromConfig(config);
            }
        }
        public bool AddSkill(ZSkillConfig config)
        {
            if (config == null)
            {
                Debug.LogError("[SkillController] Cannot add null skill config");
                return false;
            }
            if(!IsStat(config))
            {
                _skillConfigs.Add(config);
            }
            return CreateSkillFromConfig(config);
        }
        private bool CreateSkillFromConfig(ZSkillConfig config)
        {
            try
            {
                var skill = config.CreateSkill(Owner);
                skill.Start();
                if (skill == null)
                {
                    Debug.LogError($"[SkillController] Failed to create skill from config: {config.Id}");
                    return false;
                }
                if (!IsStat(config))
                {
                    _skillInstances[config] = skill;
                    _skillLevels[config] = skill.LevelIndex;
                    _iZSKills.Add(skill);
                }
            }
            catch(Exception e)
            {
                Debug.LogError($"[SkillControllerBehavior] CreateSkill Error : {e}");
            }
            return true;
        }
        public int GetSkillLevel(ZSkillConfig config)
        {
            if (_skillLevels.ContainsKey(config))
            {
                return (int)_skillLevels[config].Value;
            }
            return 0;
        }
        private bool IsStat(ZSkillConfig config)
        {
            return config.SkillDefine == SkillDefine.Stat_Gold || config.SkillDefine == SkillDefine.Stat_HP;
        }
        public void UpgradeSkill(ZSkillConfig skillConfig)
        {
            if (_skillInstances.ContainsKey(skillConfig))
            {
                var skill = _skillInstances[skillConfig];
                skill.UpgradeSkill();
                Debug.Log($"[SkillControllerBehavior] Upgrade Skill {_skillLevels[skillConfig].Value}");
            }
            else
            {
                Debug.Log($"[SkillControllerBehavior] No Skill {skillConfig.SkillDefine} and Add ");
                AddSkill(skillConfig);
            }
        }
        public bool HasZSkillConfig(string skillId) => _skillConfigs.Any(c => c?.Id == skillId);
        public void InitZSkillConfigs(List<ZSkillConfig> configs)
        {
            _skillConfigs = configs ?? new List<ZSkillConfig>();
        }
        public List<ZSkillConfig> GetAllZSkillConfigs() => _skillConfigs;
        public List<IZSkill> GetAllZSkill() => _iZSKills;
        protected override void DoDestroy()
        {
            base.DoDestroy();
            ClearSkills();
            _skillConfigs.Clear();
            _iZSKills.Clear();
        }
    }
}
