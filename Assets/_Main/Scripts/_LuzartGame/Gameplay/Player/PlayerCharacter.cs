using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Luzart
{
    public class PlayerCharacter : CharacterBase
    {
        private List<RuntimeStat> _runtimeStat;
        public StatsConfig StatsConfigDefault => entityBluePrint.statsConfig;
        public PlayerCharacter(EntityBluePrint entityBluePrint)
        {
            this.entityBluePrint = entityBluePrint;
        }
        public EntityBluePrint entityBluePrint;
        public override void Initialize()
        {
            base.Initialize();
            InitStats(StatsConfigDefault.AssetStats);
        }
        public List<IStat> CalculateStatToInitGame()
        {
            using var pool = new ListPoolHelper<IStat>(null);
            var list = pool.List;
            var baseStats = StatsConfigDefault.AssetStats;
            for (int i = 0; i < baseStats.Count; i++)
            {
                var statRuntime = StatExtension.ConvertToRuntimeStat(baseStats[i]);
                list.Add(statRuntime);
            }
            return list;
        }
    }
}
