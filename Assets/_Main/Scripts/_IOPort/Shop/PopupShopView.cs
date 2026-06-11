using System.Collections.Generic;

namespace Luzart
{
    public class PopupShopView : ViewT<Data_Shop>
    {
        public InventoryItemData InventoryItemData => Data.InventoryItemData;
        public IReadOnlyList<IChest> Chests => Data.Chests;
        public IReadOnlyList<ShopShardOffer> ShardOffers => Data.ShardOffers;
        public IEnumerable<object> ShardOffersObj
        {
            get
            {
                foreach (var o in Data.ShardOffers) yield return o;
            }
        }
    }
}
