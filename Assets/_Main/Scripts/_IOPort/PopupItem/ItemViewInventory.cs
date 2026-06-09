using UnityEngine;
using Luzart.NewBase;

namespace Luzart
{
    public class ItemViewInventory : MonoBehaviour
    {
        [SerializeField] private BaseSelect _bsTypeItem;
        [SerializeField] private ItemViewWithLevelInventory _itemView;
        private ItemConfig _itemConfig;

        public void Setup(ItemConfig itemConfig)
        {
            if (itemConfig == null)
            {
                return;
            }
            this._itemConfig = itemConfig;
            int typeItem = (int)itemConfig.TypeItem;
            int level = itemConfig.Level.LevelIndex;
            if (_bsTypeItem != null) _bsTypeItem.Select(typeItem);
            if (_itemView != null) _itemView.Setup(itemConfig, level);
        }

        public void OnClickItemView()
        {
            var Inventory = SceneRootManager.Instance._domain.Get<InventoryItemData>();
            var popupService = SceneRootManager.Instance._domain.GetService<PopupService>();
            var data = new PopupItemEquipVM();
            data.ItemConfig = _itemConfig;
            data.Inventory = Inventory;
            popupService.ShowPopup<PopupItemEquip, PopupItemEquipVM>(PopupLayer.Overlay, data);
        }
    }
}
