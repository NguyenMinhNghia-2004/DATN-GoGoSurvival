using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Luzart
{
    public class PopupItemInventoryView : ViewT<InventoryItemData>
    {
        public List<AssetEquipmentSlot> AssetEquipmentSlotsRight => GetAssetEquipmentSlotRight();
        public List<AssetEquipmentSlot> AssetEquipmentSlotsLeft => GetAssetEquipmentSlotLeft();
        public List<FrameStatInventoryVM> FrameStatInventoryVMs => Data.FrameStatInventoryVM;

        // Accessors so ViewChilding-driven children can consume the slots/inventory.
        // UIItemSpawnerGeneric needs IEnumerable<object>; ScrollViewItemInventoryData needs the InventoryItemData itself.
        public IEnumerable<object> SlotsRightObj => AssetEquipmentSlotsRight.Cast<object>();
        public IEnumerable<object> SlotsLeftObj => AssetEquipmentSlotsLeft.Cast<object>();
        public IEnumerable<object> AllSlotsObj => Data.AssetEquipmentSlots.Cast<object>();
        public IEnumerable<object> FrameStatsObj => FrameStatInventoryVMs.Cast<object>();
        public InventoryItemData Inventory => Data;

        private List<AssetEquipmentSlot> _assetEquipmentSlotsLeft = null;
        private List<AssetEquipmentSlot> GetAssetEquipmentSlotLeft()
        {
            if (_assetEquipmentSlotsLeft == null || _assetEquipmentSlotsLeft.Count == 0)
            {
                _assetEquipmentSlotsLeft = new List<AssetEquipmentSlot>();
                int count = Data.AssetEquipmentSlots.Count / 2;
                for (int i = count; i < Data.AssetEquipmentSlots.Count; i++)
                {
                    var slot = Data.AssetEquipmentSlots[i];
                    if (slot != null)
                    {
                        _assetEquipmentSlotsLeft.Add(slot);
                    }
                }
            }
            return _assetEquipmentSlotsLeft;
        }

        private List<AssetEquipmentSlot> _assetEquipmentSlotsRight = null;
        private List<AssetEquipmentSlot> GetAssetEquipmentSlotRight()
        {
            if (_assetEquipmentSlotsRight == null || _assetEquipmentSlotsRight.Count == 0)
            {
                _assetEquipmentSlotsRight = new List<AssetEquipmentSlot>();
                int count = Data.AssetEquipmentSlots.Count / 2;
                for (int i = 0; i < count; i++)
                {
                    var slot = Data.AssetEquipmentSlots[i];
                    if (slot != null)
                    {
                        _assetEquipmentSlotsRight.Add(slot);
                    }
                }
            }
            return _assetEquipmentSlotsRight;
        }
    }
}
