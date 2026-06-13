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

        /// <summary>ATK + HP this item contributes at its current level (one is usually 0 since each
        /// item grants a single stat). Lets the player see which stat the item boosts.</summary>
        public void GetAtkHp(out double atk, out double hp) => ItemStatUtil.GetAtkHp(ItemConfig, out atk, out hp);
        public string GetAtkText() { GetAtkHp(out double atk, out _); return "ATK  +" + ItemStatUtil.Fmt(atk); }
        public string GetHpText() { GetAtkHp(out _, out double hp); return "HP  +" + ItemStatUtil.Fmt(hp); }

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
