using UnityEngine;
using Luzart.NewBase;

namespace Luzart
{
    public class SlotItemEquipmentView : ViewT<AssetEquipmentSlot>
    {
        [SerializeField] private ItemViewWithLevelInventory itemView;
        [SerializeField] private BaseSelect bsState;
        [SerializeField] private BaseSelect bsWeapon;

        private IEquipmentSlot iEquipmentSlot => Data;
        private IBool IsUnlocked => iEquipmentSlot.IsUnlocked;

        protected override void OnSetup()
        {
            base.OnSetup();
            if (bsWeapon != null) bsWeapon.Select((int)Data.ETypeItem);
            IEquipmentSlot_Changed(iEquipmentSlot);
            SetUnlock(iEquipmentSlot);
            IsUnlocked.Changed += SetUnlock;
            iEquipmentSlot.Changed += IEquipmentSlot_Changed;
        }

        private void IEquipmentSlot_Changed(IEquipmentSlot obj)
        {
            if (obj.EquippedItem == null)
            {
                if (bsState != null) bsState.Select(1);
            }
            else
            {
                if (bsState != null) bsState.Select(0);
                if (itemView != null) itemView.Setup(obj.EquippedItem, obj.EquippedItem.Level.LevelIndex);
            }
        }

        protected override void OnTeardown()
        {
            base.OnTeardown();
            IsUnlocked.Changed -= SetUnlock;
            iEquipmentSlot.Changed -= IEquipmentSlot_Changed;
        }

        private void SetUnlock(IBool iBool)
        {
            bool isUnlock = iBool.Value;
            if (!isUnlock)
            {
                if (bsState != null) bsState.Select(2);
            }
        }

        private void SetUnlock(IEquipmentSlot iEquipmentSlot)
        {
            SetUnlock(iEquipmentSlot.IsUnlocked);
        }

        public void OnClickItemView()
        {
            var Inventory = Data.MyDomain.Get<InventoryItemData>();
            var popupService = Data.MyDomain.GetService<PopupService>();
            var data = new PopupItemEquipVM();
            data.ItemConfig = iEquipmentSlot.EquippedItem;
            data.Inventory = Inventory;
            if (data.ItemConfig == null) return;
            popupService.ShowPopup<PopupItemEquip, PopupItemEquipVM>(PopupLayer.Overlay, data);
        }
    }
}
