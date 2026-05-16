using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.Pool;
namespace Luzart
{
    public class ZSkill : IZSkill
    {
        private IEntity _entity;
        private ZSkillConfig _skillConfig;
        private INumberWithSet _levelIndex = new Number(0);
        private List<IZSkillBehavior> _behaviors;
        IEntity IZSkill.Entity => _entity;
        ZSkillConfig IZSkill.Config => _skillConfig;
        INumberWithSet IZSkill.LevelIndex => _levelIndex;
        IReadOnlyList<IZSkillBehavior> IZSkill.Behaviors => _behaviors;
        IEntity IBehavior.Owner => _entity;
        public ZSkill(IEntity entity, ZSkillConfig skillConfig)
        {
            try
            {
                _entity = entity;
                _skillConfig = skillConfig;
                _levelIndex = new Number(0);
                _behaviors = new List<IZSkillBehavior>();
                foreach (var bc in skillConfig.BehaviorConfigs)
                {
                    _behaviors.Add(bc.CreateBehavior(this));
                }
            }
            catch(Exception e)
            {
                UnityEngine.Debug.LogError($"[ZSkill] CreateSkill {skillConfig.name} Bug : {e}");
            }
        }
        void IDisposable.Dispose()
        {
            foreach (var b in _behaviors)
            {
                b.Dispose();
            }
            //ListPool<IZSkillBehavior>.Release(_behaviors);
        }
        void IBehavior.Start()
        {
            DoStart();
        }
        void IBehavior.Update(float dt)
        {
            DoUpdate(dt);
        }
        void IBehavior.OnDestroy()
        {
            DoOnDestroy();
        }
        protected virtual void DoStart()
        {
            for (int i = 0; i < _behaviors.Count; i++)
            {
                IZSkillBehavior behavior = _behaviors[i];
                behavior.Start();
            }
        }
        protected virtual void DoUpdate(float dt)
        {
            for (int i = 0; i < _behaviors.Count; i++)
            {
                IZSkillBehavior behavior = _behaviors[i];
                behavior.Update(dt);
            }
        }
        protected virtual void DoOnDestroy()
        {
            foreach (var behavior in _behaviors)
            {
                behavior.OnDestroy();
            }
        }
        void IZSkill.UpgradeSkill()
        {
            _levelIndex.Set(_levelIndex.Value + 1);
        }
        bool IZSkill.IsUpgradeSkill()
        {
            return DoIsUpgradeSkill();
        }
        protected virtual bool DoIsUpgradeSkill()
        {
            int levelCurrent = (int)_levelIndex.Value;
            int maxLevel = _skillConfig.UpgradeConfigs.Count;
            bool isLevelCurrentSmallerMaxLevel = levelCurrent < maxLevel-1;
            if (isLevelCurrentSmallerMaxLevel)
            {
                bool isRequireLevel = _skillConfig.IsCanUpgradeLevel.Value;
                if (isRequireLevel)
                {
                    return true;
                }
            }
            return false;
        }
    }
}