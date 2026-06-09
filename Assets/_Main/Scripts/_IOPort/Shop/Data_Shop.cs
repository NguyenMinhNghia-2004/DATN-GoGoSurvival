using System.Collections.Generic;
using UnityEngine;

namespace Luzart
{
    public class Data_Shop : AbstractScriptableContent
    {
        [SerializeField] private InventoryItemData inventoryItemData;
        [SerializeField] private List<Chest> chests;
        public InventoryItemData InventoryItemData => inventoryItemData;
        public IReadOnlyList<IChest> Chests => chests;

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
