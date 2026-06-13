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
            SeedItemCards(seedShard);
        }

        private void SeedPool(string id, double amount)
        {
            var pool = _domain.Get<ResourcePool>(id);
            if (pool == null) return;
            IResourcePool ip = pool;
            if (ip.Value.Value <= 0) ip.Set(amount);
        }

        // Per-item card economy: seed each item's own card pool (only when empty) so the
        // unlock/upgrade loop is testable. Set seedShard to 0 to ship a fully-earned economy.
        private void SeedItemCards(double amount)
        {
            if (amount <= 0) return;
            var all = _domain.GetAll<ItemConfig>();
            if (all == null) return;
            for (int i = 0; i < all.Count; i++)
            {
                var item = all[i];
                if (item == null || item.CardPool == null) continue;
                IResourcePool ip = item.CardPool;
                if (ip.Value.Value <= 0) ip.Set(amount);
            }
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

        /// <summary>Open the Shop popup (does not toggle off if already open).</summary>
        public void OpenShop()
        {
            bool wasOpen = FindObjectOfType<PopupShop>() != null;
            if (wasOpen) return;
            CloseAllPopups();
            var ps = _domain.GetService<PopupService>();
            var data = _domain.Get<Data_Shop>();
            if (ps != null && data != null) ps.ShowPopup<PopupShop, Data_Shop>(PopupLayer.Overlay, data);
        }

        /// <summary>Open the Equipment popup (does not toggle off if already open).</summary>
        public void OpenEquipment()
        {
            bool wasOpen = FindObjectOfType<PopupItemInventory>() != null;
            if (wasOpen) return;
            CloseAllPopups();
            var ps = _domain.GetService<PopupService>();
            var data = _domain.Get<InventoryItemData>();
            if (ps != null && data != null) ps.ShowPopup<PopupItemInventory, InventoryItemData>(PopupLayer.Overlay, data);
        }

        /// <summary>Robustly close every open popup (event bus + any orphan GameObjects).</summary>
        public void CloseAllPopups()
        {
            var ps = _domain != null ? _domain.GetService<PopupService>() : null;
            if (ps != null) ps.HideAllPopups();
            foreach (var p in FindObjectsOfType<Popup>())
            {
                if (p != null) Destroy(p.gameObject);
            }
        }
    }
}
