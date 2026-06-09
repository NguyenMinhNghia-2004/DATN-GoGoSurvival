using System.Collections.Generic;
using UnityEngine;

namespace Luzart
{
    public class PopupItemInventoryView : ViewT<InventoryItemData>
    {
        public List<AssetEquipmentSlot> AssetEquipmentSlotsRight => GetAssetEquipmentSlotRight();
        public List<AssetEquipmentSlot> AssetEquipmentSlotsLeft => GetAssetEquipmentSlotLeft();
        public List<FrameStatInventoryVM> FrameStatInventoryVMs => Data.FrameStatInventoryVM;

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
