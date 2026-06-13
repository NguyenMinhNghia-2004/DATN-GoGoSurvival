using System.Collections.Generic;
using UnityEngine;

namespace Luzart
{
    public class ShopPopupUnlockedView : ViewT<InventoryItemData>
    {
        public IReadOnlyList<ItemConfig> ItemConfigs => Data.GetAllItemConfigDontBuy();
        [SerializeField] private UIItemSpawnerGeneric uIItemSpawnerGeneric;

        protected override void OnSetup()
        {
            base.OnSetup();
            Data_OnItemOwnedChanged();
            Data.OnItemOwnedChanged += Data_OnItemOwnedChanged;
        }

        protected override void OnTeardown()
        {
            base.OnTeardown();
            Data.OnItemOwnedChanged -= Data_OnItemOwnedChanged;
            if (uIItemSpawnerGeneric != null) uIItemSpawnerGeneric.Teardown();
        }

        private void Data_OnItemOwnedChanged()
        {
            if (uIItemSpawnerGeneric == null) return;
            uIItemSpawnerGeneric.Teardown();
            uIItemSpawnerGeneric.Setup(ItemConfigs);
        }
    }
}
