using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.AI;
namespace Luzart
{
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "Luzart/Level/Level Config", order = 1)]
    public class LevelConfig : AbstractScriptableContent
    {
        [SerializeField] private int level;
        [SerializeField] private string mapConfigId;
        [SerializeField] private List<DataWave> enemyWaves;
        [SerializeField] private List<ZSkillConfig> playerSkillCanReceives;
        [SerializeField] private List<ZSkillConfig> playerSkilStatConfigs;
        [SerializeField] private List<int> _listXPRequirePerLevel;
        public int Level => level;
        public string MapConfigId => mapConfigId;
        public List<DataWave> EnemyWaves => enemyWaves;
        public List<int> ListXPRequirePerLevel => _listXPRequirePerLevel;
        private Dictionary<SkillDefine, ZSkillConfig> dictSkill = new Dictionary<SkillDefine, ZSkillConfig>();
        public ZSkillConfig GetSkill(SkillDefine skillDefine, int level)
        {
            if(dictSkill.Count <= 0)
            {
                InitDictionary();
            }
            if (dictSkill.ContainsKey(skillDefine))
            {
                return dictSkill[skillDefine];
            }
            return null;
        }
        public List<ZSkillConfig> GetAllSkillInLevel()
        {
            var list = new List<ZSkillConfig>();
            list.AddRange(playerSkillCanReceives);
            list.AddRange(playerSkilStatConfigs);
            return list;
        }
        private void InitDictionary()
        {
            dictSkill.Clear();
            int length = playerSkillCanReceives.Count;
            for (int i = 0; i < length; i++)
            {
                var skillConfig = playerSkillCanReceives[i];
                dictSkill.Add(skillConfig.SkillDefine,skillConfig);
            }
            int lengthStats = playerSkilStatConfigs.Count;
            for (int i = 0; i < lengthStats; i++)
            {
                var skillConfig = playerSkilStatConfigs[i];
                dictSkill.Add(skillConfig.SkillDefine, skillConfig);
            }
        }
        private List<int> _listTimeWave = new List<int>();
        public List<int> TimeWaves
        {
            get
            {
                if (_listTimeWave !=null && _listTimeWave.Count > 0)
                {
                    return _listTimeWave;
                }
                _listTimeWave = new List<int>();
                int time = 0;
                for (int i = 0; i < enemyWaves.Count; i++)
                {
                    time += enemyWaves[i].TimeWave;
                    _listTimeWave.Add(time);
                }
                return _listTimeWave;
            }
        }
        protected override void DoInitialize()
        {
            base.DoInitialize();
            _listTimeWave = null;
        }
#if UNITY_EDITOR
        public void AutoListXPFibonacii(int start = 50, int space = 20, int length = 100)
        {
            _listXPRequirePerLevel.Clear();
            int currentValue = start;
            _listXPRequirePerLevel.Add(currentValue);
            for (int i = 0; i < length; i++)
            {
                int value = (currentValue + space) * i;
                _listXPRequirePerLevel.Add(value);
            }
        }
        private void AutoDefineData()
        {
            foreach (var wave in enemyWaves)
            {
                foreach (var item in wave.Waves)
                {
                    if(wave.TimeWave <= item.TotalTimeSpawn)
                    {
                        wave.TimeWave = (int)(item.TotalTimeSpawn * UnityEngine.Random.Range(1.5f, 2.5f));
                    }
                }
            }
        }
#endif
    }
    [Serializable]
    public class DataWave
    {
        [SerializeField] private int timeWave;
        [SerializeField] private List<EnemyWaveConfig> waves;
        private Dictionary<int, List<EnemyWaveConfig>> waveDictionary = new Dictionary<int, List<EnemyWaveConfig>>();
        public List<EnemyWaveConfig> Waves => waves;
        public int TimeWave
        {
            get => timeWave;
            set => timeWave = value;
        }
        public void SolveWave()
        {
            int length = waves.Count;
            for (int i = 0; i < length; i++)
            {
                var wave = waves[i];
                if (!waveDictionary.ContainsKey(wave.IndexWave))
                {
                    waveDictionary[wave.IndexWave] = new List<EnemyWaveConfig>();
                }
                waveDictionary[wave.IndexWave].Add(wave);
            }
        }
        public List<EnemyWaveConfig> GetListWaveEnemy(int indexWave)
        {
            if (waveDictionary.Count == 0)
            {
                SolveWave();
            }
            if (waveDictionary.ContainsKey(indexWave))
            {
                return waveDictionary[indexWave];
            }
            return new List<EnemyWaveConfig>();
        }
    }
    [Serializable]
    public class EnemyWaveConfig
    {
        [SerializeField] private EnemyDefinition enemyDefinition;
        [SerializeField] private StatsConfig statsConfig;
        [SerializeField] private DropConfigRequire dropConfigRequire;
        [SerializeField] private StatsConfig statsFactor;
        [SerializeField] private int indexWave;
        [SerializeField] private float timeIntervalSpawn;
        [SerializeField] private float totalTimeSpawn;
        [SerializeField] private float timeDelayBeforeSpawn;
        public EnemyDefinition EnemyDefinition => enemyDefinition;
        public StatsConfig StatsConfig => statsConfig;
        public DropConfigRequire DropConfigRequire => dropConfigRequire;
        public StatsConfig StatsFactor => statsFactor;
        public int IndexWave => indexWave;
        public float TotalTimeSpawn => totalTimeSpawn;
        public float TimeDelayBeforeSpawn => timeDelayBeforeSpawn;
        public float TimeIntervalSpawn => timeIntervalSpawn;
    }
}