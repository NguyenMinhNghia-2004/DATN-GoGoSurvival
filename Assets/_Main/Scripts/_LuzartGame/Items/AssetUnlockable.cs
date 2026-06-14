using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Luzart
{
    public class AssetUnlockable : AbstractScriptableContentSaveable, IUnlockable
    {
        private const string IS_UNLOCKED = "IsUnlocked";
        [SerializeField] List<SerializableCostCreator_CommonlyUsed> unlockCosts;
        List<ICost> _unlockCosts;
        private IBoolSet _isUnlocked;
        IBoolSet IUnlockable.IsUnlocked => TryGetUnlockable();
        IReadOnlyList<ICost> IUnlockable.UnlockCosts => _unlockCosts;
        protected override void DoInitialize()
        {
            base.DoInitialize();
            _unlockCosts = unlockCosts.OfType<ICostCreator>().Select(s => s.CreateCost(MyDomain)).ToList();
        }
        protected override void DoTerminate()
        {
            base.DoTerminate();
            _isUnlocked.Changed -= IsUnlockedChanged;
        }
        private void IsUnlockedChanged(IBool obj)
        {
            DoSave();
        }
        protected override void DoLoad(IEnumerable<SaveItem> saveItems)
        {
            foreach (var item in saveItems)
            {
                // Was `item.Equals(IS_UNLOCKED)` — compared the SaveItem struct to a string, so it
                // NEVER matched and unlock state never restored (breaks cloud/local sync of owned
                // items). Match on the key, and SET the existing IBool when present so subscribed
                // views (shop/inventory) refresh via .Changed instead of dangling on a new ref.
                if (item.key == IS_UNLOCKED)
                {
                    if (_isUnlocked == null) _isUnlocked = new BoolSet(item.boolValue);
                    else _isUnlocked.Set(item.boolValue);
                }
            }
            _isUnlocked ??= new BoolSet(false);
            _isUnlocked.Changed -= IsUnlockedChanged;
            _isUnlocked.Changed += IsUnlockedChanged;
        }
        protected override IEnumerable<SaveItem> DoSave()
        {
            yield return new SaveItem(IS_UNLOCKED, _isUnlocked.Value);
        }
        private IBoolSet TryGetUnlockable()
        {
            if(_isUnlocked == null)
            {
                _isUnlocked = new BoolSet(false);
            }
            return _isUnlocked;
        }
        public List<SerializableCostCreator_CommonlyUsed> UnlockCostEditors
        {
            get => unlockCosts;
            set => unlockCosts = value;
        }
        public bool isUnlockedEditor => _isUnlocked?.Value ?? false;
    }
}
