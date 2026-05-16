using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Luzart
{
    public class AssetLevel : AbstractScriptableContentSaveable, ILevelable
    {
        [SerializeField] private int levelIndex;
        [SerializeField] private int maxLevelIndex;
        [SerializeField] private List<ListSerializableCostCreator_CommonlyUsed> levelUpCosts = new();
        List<List<ICost>> _levelUpCosts;
        int ILevelable.LevelIndex => levelIndex;
        int ILevelable.MaxLevelIndex => maxLevelIndex;
        event Action<ILevelable> _changed;
        event Action<ILevelable> ILevelable.Changed
        {
            add => _changed += value;
            remove => _changed -= value;
        }
        protected override void DoInitialize()
        {
            base.DoInitialize();
            _levelUpCosts = levelUpCosts.Select(list => list.List.OfType<ICostCreator>().Select(x => x.CreateCost(MyDomain)).ToList()).ToList();
        }
        void ILevelable.SetLevelIndex(int levelIndex)
        {
            DoSetLevelIndex(levelIndex);
        }
        protected void DoSetLevelIndex(int levelIndex)
        {
            this.levelIndex = levelIndex;
            _changed?.Invoke(this);
        }
        void ILevelable.SetMaxLevelIndex(int maxLevelIndex)
        {
            DoSetMaxLevelIndex(maxLevelIndex);
        }
        protected void DoSetMaxLevelIndex(int maxLevelIndex)
        {
            this.maxLevelIndex = maxLevelIndex;
        }
        protected override IEnumerable<SaveItem> DoSave()
        {
            yield return new SaveItem("LevelIndex", levelIndex);
        }
        protected override void DoLoad(IEnumerable<SaveItem> saveItems)
        {
            foreach (var item in saveItems)
            {
                if (item.key.Equals("LevelIndex"))
                {
                    levelIndex = item.intValue;
                }
            }
        }
        IReadOnlyList<ICost> ILevelable.GetLevelUpCost(int levelIndex = -1)
        {
            if(levelIndex == -1)
            {
                levelIndex = this.levelIndex;
            }
            if (_levelUpCosts != null && levelIndex >= 0 && levelIndex < _levelUpCosts.Count)
            {
                return _levelUpCosts[levelIndex];
            }
            return null;
        }
        public int LevelIndexEditor
        {
            get => levelIndex;
            set => levelIndex = value;
        }
        public int MaxLevelIndexEditor
        {
            get => maxLevelIndex;
            set => maxLevelIndex = value;
        }
        public List<ListSerializableCostCreator_CommonlyUsed> LevelUpCostsEditor
        {
            get => levelUpCosts;
            set => levelUpCosts = value;
        }
    }
    [Serializable]
    public class ListSerializableCostCreator_CommonlyUsed
    {
        public List<SerializableCostCreator_CommonlyUsed> List = new List<SerializableCostCreator_CommonlyUsed>();
    }
}
