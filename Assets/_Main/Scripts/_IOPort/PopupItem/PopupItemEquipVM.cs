using System.Collections.Generic;

namespace Luzart
{
    public class PopupItemEquipVM
    {
        public InventoryItemData Inventory;
        public ItemConfig ItemConfig;

        public bool IsButtonInList()
        {
            List<ItemConfig> itemConfigsCurrent = Inventory.GetItemConfigEquipped();
            for (int i = 0; i < itemConfigsCurrent.Count; i++)
            {
                var item = itemConfigsCurrent[i];
                if (item == ItemConfig)
                {
                    return true;
                }
            }
            return false;
        }

        public void RefreshEquippedItem(bool isEquipped)
        {
            Inventory.RefreshEquipItem(ItemConfig, isEquipped);
        }

        public string GetUpgradeDetail()
        {
            return UpgradeDetailContent.GetUpgradeDetailModifierPair(ItemConfig.GetCurrentModifierPairs());
        }

        public void UpgradeItem()
        {
            int level = ItemConfig.Level.LevelIndex;
            var listCost = ItemConfig.Level.GetLevelUpCost(level);
            for (int i = 0; i < listCost.Count; i++)
            {
                var cost = listCost[i];
                cost.TrySpend();
            }
            ItemConfig.UpgradeItem();
        }
    }
}
