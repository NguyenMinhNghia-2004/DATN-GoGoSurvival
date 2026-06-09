using System.Collections.Generic;

namespace Luzart
{
    public class PopupShopView : ViewT<Data_Shop>
    {
        public InventoryItemData InventoryItemData => Data.InventoryItemData;
        public IReadOnlyList<IChest> Chests => Data.Chests;
    }
}
