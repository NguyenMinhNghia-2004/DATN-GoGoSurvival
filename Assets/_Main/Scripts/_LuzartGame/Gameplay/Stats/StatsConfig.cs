using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
namespace Luzart
{
    /// <summary>
    /// Class chứa cấu hình các chỉ số cơ bản của nhân vật
    /// </summary>
    [CreateAssetMenu(fileName = "StatsConfig", menuName = "Luzart/Stats/Stats Config")]
    public class StatsConfig : AbstractScriptableContent
    {
        //[SerializeField]
        //private List<Stat> baseStats;
        //public List<Stat> BaseStats => baseStats;
        [SerializeField]
        private List<AssetStat> assetStats;
        public List<AssetStat> AssetStats => assetStats;
        public List<RuntimeStat> GetRuntimeStat()
        {
            using var pool = new ListPoolHelper<RuntimeStat>();
            var list = pool.List;
            for (int i = 0; i < assetStats.Count; i++)
            {
                var stat = assetStats[i];
                var iStat = stat as IStat;
                list.Add(new RuntimeStat(iStat.Definition, iStat.Value));
            }
            return list;
        }
    }
    [System.Serializable]
    public class Stat : IStat
    {
        [SerializeField] AssetStatDefinition definition;
        [SerializeField] NumberPicker value;
        public StatType StatType => Definition.StatType;
        public INumber Value => value.PickNumber();
        public IStatDefinition Definition => definition;
        INumber IStat.Value => value.PickNumber();
        public NumberPicker Editor_Value
        {
            get => value;
            set => this.value = value;
        }
        public AssetStatDefinition Editor_Definition
        {
            get => definition;
            set => definition = value;
        }
    }
}