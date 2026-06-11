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
        public void GetAtkHp(out double atk, out double hp)
        {
            atk = 0; hp = 0;
            var pairs = ItemConfig.GetCurrentModifierPairs();
            if (pairs == null) return;
            for (int i = 0; i < pairs.Count; i++)
            {
                var pair = pairs[i];
                if (pair == null || pair.Factor == null) continue;
                double val = pair.Factor.Value != null ? pair.Factor.Value.Value : 0;
                string key = StatKey(pair);
                if (key.Contains("ATK")) atk += val;
                else if (key.Contains("HP")) hp += val;
            }
        }

        public string GetAtkText() { GetAtkHp(out double atk, out _); return "ATK  +" + Fmt(atk); }
        public string GetHpText() { GetAtkHp(out _, out double hp); return "HP  +" + Fmt(hp); }
        private static string Fmt(double v) => v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

        private static string StatKey(IModifierPair pair)
        {
            string s = "";
            var d = pair.Factor != null ? pair.Factor.Definition as UnityEngine.Object : null;
            if (d != null) s += d.name + " ";
            if (pair.Modifier != null)
            {
                var md = pair.Modifier.Definition as UnityEngine.Object;
                if (md != null) s += md.name + " ";
                var mo = pair.Modifier as UnityEngine.Object;
                if (mo != null) s += mo.name + " ";
            }
            return s;
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
