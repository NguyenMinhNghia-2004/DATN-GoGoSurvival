using Luzart;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
namespace Luzart
{
    public sealed class AssetEquipmentSlot : AbstractScriptableContentSaveable, IEquipmentSlot
    {
        [SerializeField] ETypeItem eTypeItem;
        [SerializeField] BaseAssetBool isUnlocked;
        public ETypeItem ETypeItem => eTypeItem;
        [SerializeField]
        ItemConfig _equippedItem;
        // Item đang được trang bị trong slot này
        ItemConfig IEquipmentSlot.EquippedItem => _equippedItem;
        // Đã unlock chưa
        IBool IEquipmentSlot.IsUnlocked => isUnlocked == null ? new BoolValue(true) : isUnlocked;
        // Sự kiện khi item được trang bị thay đổi
        private event Action<IEquipmentSlot> _onEquippedItemChanged;
        event Action<IEquipmentSlot> IEquipmentSlot.Changed
        {
            add
            {
                _onEquippedItemChanged += value;
            }
            remove
            {
                _onEquippedItemChanged -= value;
            }
        }
        protected override void DoStartContent()
        {
            base.DoStartContent();
            if (_equippedItem != null)
                EquipItem(_equippedItem);
        }
        public void EquipItem(ItemConfig itemConfig)
        {
            this._equippedItem = itemConfig;
            this._equippedItem.Level.Changed -= OnLevelUp;
            this._equippedItem.Level.Changed += OnLevelUp;
            ActiveModifierFactor(itemConfig, true);
            _onEquippedItemChanged?.Invoke(this);
        }
        public void UnEquipItem(ItemConfig itemConfig = null)
        {
            this._equippedItem.Level.Changed -= OnLevelUp;
            ActiveModifierFactor(_equippedItem, false);
            this._equippedItem = null;
            _onEquippedItemChanged.Invoke(this);
        }
        private void OnLevelUp(ILevelable iLevelable)
        {
            if (_equippedItem != null)
            {
                if (iLevelable.LevelIndex > 0)
                {
                    int preLevel = iLevelable.LevelIndex - 1;
                    ActiveModifierFactor(_equippedItem, false, preLevel);
                }
                ActiveModifierFactor(_equippedItem, true);
            }
        }
        private void ActiveModifierFactor(ItemConfig itemConfig, bool isEquipped, int level = -1)
        {
            var equippedItem = itemConfig;
            if (level == -1)
            {
                level = equippedItem.Level.LevelIndex;
            }
            if (equippedItem == null || equippedItem.EquippedModifierFactorGroups == null || equippedItem.EquippedModifierFactorGroups.Count == 0 || level >= equippedItem.EquippedModifierFactorGroups.Count)
            {
                return;
            }
            var needToActivateModifierPairs = equippedItem.EquippedModifierFactorGroups[level];
            if (isEquipped)
            {
                needToActivateModifierPairs.Activate();
            }
            else
            {
                needToActivateModifierPairs.Deactivate();
            }
        }
        protected override IEnumerable<SaveItem> DoSave()
        {
            if(_equippedItem != null)
            {
                yield return new SaveItem("ItemEquip", _equippedItem.Id);
            }
            else
            {
                yield return new SaveItem("ItemEquip", string.Empty);
            }
        }
        protected override void DoLoad(IEnumerable<SaveItem> saveItems)
        {
            foreach (var item in saveItems)
            {
                if (item.key == "ItemEquip")
                {
                    _equippedItem = this.MyDomain.Get<ItemConfig>(item.stringValue);
                }
            }
        }
    }
}
