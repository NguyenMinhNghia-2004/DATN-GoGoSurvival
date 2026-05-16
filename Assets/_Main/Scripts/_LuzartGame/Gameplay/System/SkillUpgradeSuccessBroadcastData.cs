namespace Luzart
{
    public readonly struct SkillUpgradeSuccessBroadcastData : IBroadcastData
    {
        public readonly ZSkillConfig SkillConfig;
        public readonly int LevelIndex;
        public SkillUpgradeSuccessBroadcastData(ZSkillConfig skillConfig, int levelIndex)
        {
            SkillConfig = skillConfig;
            LevelIndex = levelIndex;
        }
    }
}