using System.Collections.Generic;
using UnityEngine;

namespace Luzart
{
    public class ScrollViewItemInventoryData : ViewT<InventoryItemData>
    {
        [SerializeField] private Transform parentSpawn;
        [SerializeField] private ItemViewInventory itemPrefabs;
        private List<ItemViewInventory> itemViewInventories = new List<ItemViewInventory>();
        private List<IItem> _itemConfigsCache = new List<IItem>();

        protected override void OnSetup()
        {
            base.OnSetup();
            OnSlotEquipmentChanged();
            Data.OnAnyEquipmentSlotChanged += OnSlotEquipmentChanged;
            Data.OnItemOwnedChanged += OnSlotEquipmentChanged;
        }

        protected override void OnTeardown()
        {
            base.OnTeardown();
            Data.OnAnyEquipmentSlotChanged -= OnSlotEquipmentChanged;
            Data.OnItemOwnedChanged -= OnSlotEquipmentChanged;
        }

        private void OnSlotEquipmentChanged()
        {
            _itemConfigsCache = Data.GetDataOwnedIgnoreEquipment();
            MasterHelper.InitListObj(_itemConfigsCache.Count, itemPrefabs, itemViewInventories, parentSpawn, (item, index) =>
            {
                var iItem = _itemConfigsCache[index];
                var itemConfig = iItem as ItemConfig;
                item.gameObject.SetActive(true);
                item.Setup(itemConfig);
            });
        }
    }
}
