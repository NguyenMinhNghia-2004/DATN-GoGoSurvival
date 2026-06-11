using System.Collections.Generic;
using UnityEngine;

namespace Luzart
{
    public class Data_Shop : AbstractScriptableContent
    {
        [SerializeField] private InventoryItemData inventoryItemData;
        [SerializeField] private List<Chest> chests;
        [SerializeField] private List<ShopShardOffer> shardOffers = new List<ShopShardOffer>();
        public InventoryItemData InventoryItemData => inventoryItemData;
        public IReadOnlyList<IChest> Chests => chests;
        public IReadOnlyList<ShopShardOffer> ShardOffers => shardOffers;

        public void OnClickButtonShow()
        {
            var popupService = _domain.GetService<PopupService>();
            if (popupService != null)
            {
                popupService.ShowPopup<PopupShop, Data_Shop>(PopupLayer.Overlay, this);
            }
        }
    }
}
