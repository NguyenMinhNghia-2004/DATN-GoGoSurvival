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
            if (_itemConfig != null)
            {
                _itemConfig.Level.Changed -= OnLevelChanged;
            }

            if (itemConfig == null) return;

            this._itemConfig = itemConfig;
            this._itemConfig.Level.Changed += OnLevelChanged;
            RefreshView();
        }

        private void OnLevelChanged(ILevelable lvl)
        {
            RefreshView();
        }

        private void RefreshView()
        {
            if (_itemConfig == null) return;
            int typeItem = (int)_itemConfig.TypeItem;
            int level = _itemConfig.Level.LevelIndex;
            if (_bsTypeItem != null) _bsTypeItem.Select(typeItem);
            if (_itemView != null) _itemView.Setup(_itemConfig, level);
        }

        private void OnDestroy()
        {
            if (_itemConfig != null)
            {
                _itemConfig.Level.Changed -= OnLevelChanged;
            }
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
