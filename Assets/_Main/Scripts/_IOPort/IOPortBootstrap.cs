using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Runtime entry point for the faithful IO_Training shop/equipment port.
    /// - Seeds currency pools on first start (no SaveService persistence yet).
    /// - Instantiates the PopupOpener canvas and Setups it with this content so the
    ///   PopupService event bus can open popups.
    /// - Exposes OpenShop()/OpenEquipment() for nav buttons to call.
    /// Auto-discovered by SceneRootManager (it is an AbstractMonoBehaviorContent).
    /// </summary>
    public class IOPortBootstrap : AbstractMonoBehaviorContent
    {
        [SerializeField] private GameObject popupCanvasPrefab;
        [SerializeField] private double seedGold = 100000;
        [SerializeField] private double seedShard = 9999;

        private PopupOpener _opener;

        public override void DoStart()
        {
            base.DoStart();
            SeedCurrency();
            SetupCanvas();
        }

        private void SeedCurrency()
        {
            SeedPool("io_gold", seedGold);
            SeedPool("io_shard_0", seedShard);
            SeedPool("io_shard_1", seedShard);
            SeedPool("io_shard_2", seedShard);
        }

        private void SeedPool(string id, double amount)
        {
            var pool = _domain.Get<ResourcePool>(id);
            if (pool == null) return;
            IResourcePool ip = pool;
            if (ip.Value.Value <= 0) ip.Set(amount);
        }

        private void SetupCanvas()
        {
            if (popupCanvasPrefab == null)
            {
                Debug.LogError("[IOPortBootstrap] popupCanvasPrefab not assigned.");
                return;
            }
            var go = Instantiate(popupCanvasPrefab);
            go.name = "IOPortPopupCanvas";
            DontDestroyOnLoad(go);
            _opener = go.GetComponentInChildren<PopupOpener>();
            if (_opener != null)
            {
                ((IView)_opener).Setup(this);
            }
        }

        public void OpenShop()
        {
            var ps = _domain.GetService<PopupService>();
            var data = _domain.Get<Data_Shop>();
            if (ps != null && data != null)
                ps.ShowPopup<PopupShop, Data_Shop>(PopupLayer.Overlay, data);
        }

        public void OpenEquipment()
        {
            var ps = _domain.GetService<PopupService>();
            var data = _domain.Get<InventoryItemData>();
            if (ps != null && data != null)
                ps.ShowPopup<PopupItemInventory, InventoryItemData>(PopupLayer.Overlay, data);
        }
    }
}
