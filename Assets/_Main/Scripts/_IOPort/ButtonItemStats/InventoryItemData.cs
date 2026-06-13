using System.Collections.Generic;
using UnityEngine;

namespace Luzart
{
    public class InventoryItemData : AbstractScriptableContent
    {
        [SerializeField] private List<FrameStatInventoryVM> _frameStatInventoryVM;
        [SerializeField] private List<AssetEquipmentSlot> _assetEquipmentSlots;
        [SerializeField] ICostVisualResolver itemUpgradeCostVisualResolver;

        public List<FrameStatInventoryVM> FrameStatInventoryVM => _frameStatInventoryVM;
        public List<AssetEquipmentSlot> AssetEquipmentSlots => _assetEquipmentSlots;

        private Dictionary<ETypeItem, AssetEquipmentSlot> _dictAssetEquipmentSlot = new Dictionary<ETypeItem, AssetEquipmentSlot>();

        private IItemInventory _itemConfigsOwned;

        public event System.Action OnItemOwnedChanged;
        public event System.Action OnAnyEquipmentSlotChanged;

        protected override void DoInitialize()
        {
            base.DoInitialize();
            _itemConfigsOwned = _domain.Get<ItemConfigsOwned>();
            _dictAssetEquipmentSlot.Clear();
            for (int i = 0; i < _assetEquipmentSlots.Count; i++)
            {
                var slot = _assetEquipmentSlots[i];
                IEquipmentSlot equipmentSlot = slot;
                equipmentSlot.Changed += EquipmentSlot_Changed;
                _dictAssetEquipmentSlot[slot.ETypeItem] = slot;
            }

            if (_itemConfigsOwned != null)
                _itemConfigsOwned.OnItemOwnedChanged += ItemConfigsOwned_OnItemOwnedChanged;
        }

        private void ItemConfigsOwned_OnItemOwnedChanged()
        {
            OnItemOwnedChanged?.Invoke();
        }

        protected override void DoTerminate()
        {
            base.DoTerminate();
            for (int i = 0; i < _assetEquipmentSlots.Count; i++)
            {
                var slot = _assetEquipmentSlots[i];
                IEquipmentSlot equipmentSlot = slot;
                equipmentSlot.Changed -= EquipmentSlot_Changed;
            }
            _dictAssetEquipmentSlot.Clear();
            if (_itemConfigsOwned != null)
                _itemConfigsOwned.OnItemOwnedChanged -= ItemConfigsOwned_OnItemOwnedChanged;
        }

        public IReadOnlyList<ItemConfig> GetAllItemConfigDontBuy()
        {
            List<ItemConfig> list = new List<ItemConfig>();
            var allItemConfig = _domain.GetAll<ItemConfig>();
            for (int i = 0; i < allItemConfig.Count; i++)
            {
                var item = allItemConfig[i];
                if (item.Unlockable != null && !item.Unlockable.IsUnlocked.Value)
                {
                    list.Add(item);
                }
            }
            return list;
        }

        private void EquipmentSlot_Changed(IEquipmentSlot obj)
        {
            OnAnyEquipmentSlotChanged?.Invoke();
        }

        public void RefreshEquipItem(ItemConfig itemConfig, bool isEquipped)
        {
            if (!_dictAssetEquipmentSlot.TryGetValue(itemConfig.TypeItem, out var slot)) return;
            IEquipmentSlot iEquipped = slot;
            var itemCurrentInSlot = iEquipped.EquippedItem;
            if (itemCurrentInSlot != null)
            {
                slot.UnEquipItem(itemCurrentInSlot);
            }
            if (isEquipped)
            {
                slot.EquipItem(itemConfig);
            }
        }

        public void OnClickButtonStatsShow()
        {
            var popupService = _domain.GetService<PopupService>();
            if (popupService != null)
            {
                popupService.ShowPopup<PopupItemInventory, InventoryItemData>(PopupLayer.Overlay, this);
            }
        }

        public List<IItem> GetDataOwnedIgnoreEquipment()
        {
            var listOwned = _itemConfigsOwned.GetAllItems();
            var listEquip = GetItemConfigEquipped();
            List<IItem> list = new List<IItem>();
            for (int i = 0; i < listOwned.Count; i++)
            {
                var item = listOwned[i];
                if (!listEquip.Contains(item as ItemConfig))
                {
                    list.Add(item);
                }
            }
            return list;
        }

        public List<ItemConfig> GetItemConfigEquipped()
        {
            List<ItemConfig> list = new List<ItemConfig>();
            int length = _assetEquipmentSlots.Count;
            for (int i = 0; i < length; i++)
            {
                IEquipmentSlot slot = _assetEquipmentSlots[i];
                if (slot.EquippedItem != null)
                {
                    list.Add(slot.EquippedItem);
                }
            }
            return list;
        }

        // NOTE: IO_Training read this from GameManagerData.FinalStatOutGame, which does not
        // exist in GoGo. The FrameStat header reads its INumber directly via NumberPicker,
        // so this accessor is unused by the views; kept for parity, returns 0.
        public double GetStat(StatType statType)
        {
            return 0;
        }

        public void UnlockItem(ItemConfig itemConfig)
        {
        }
    }
}
