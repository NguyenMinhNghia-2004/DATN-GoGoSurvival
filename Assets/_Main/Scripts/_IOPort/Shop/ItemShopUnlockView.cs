using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Luzart.NewBase;

namespace Luzart
{
    public class ItemShopUnlockView : ViewT<ItemConfig>
    {
        [SerializeField] private BaseSelect bsUnlocked;
        [SerializeField] private BaseSelect bsItem;
        [SerializeField] private Transform parentSpawnCost;
        [SerializeField] private ObjectView objectView;
        [Header("Unlock button")]
        [SerializeField] private Button unlockButton;
        [SerializeField] private Image unlockButtonBg;
        [SerializeField] private Color canUnlockColor = new Color(1f, 0.78f, 0.15f, 1f);   // yellow
        [SerializeField] private Color cantUnlockColor = new Color(0.55f, 0.55f, 0.6f, 1f); // gray
        private List<View> _spawnerCostView = new List<View>();

        protected override void OnSetup()
        {
            base.OnSetup();
            if (objectView != null) objectView.Setup(new ObjectViewData(Data));
            SetUpAndSpawnItem();
            var data = Data.Unlockable.UnlockCosts;
            for (int i = 0; i < data.Count; i++)
            {
                var costItem = data[i];
                costItem.IsSpendable.Changed += GetOnCostChanged;
            }
        }

        protected override void OnTeardown()
        {
            base.OnTeardown();
            var data = Data.Unlockable.UnlockCosts;
            for (int i = 0; i < data.Count; i++)
            {
                var costItem = data[i];
                costItem.IsSpendable.Changed -= GetOnCostChanged;
            }
            DestroyItem();
        }

        private bool CanUnlock()
        {
            var data = Data.Unlockable.UnlockCosts;
            for (int i = 0; i < data.Count; i++)
                if (!data[i].IsSpendable.Value) return false;
            return true;
        }

        private void GetOnCostChanged(IBool ibool)
        {
            bool isCanUnlock = CanUnlock();
            if (bsUnlocked != null) bsUnlocked.Select(isCanUnlock);
            if (unlockButtonBg != null) unlockButtonBg.color = isCanUnlock ? canUnlockColor : cantUnlockColor;
            if (unlockButton != null) unlockButton.interactable = isCanUnlock;
        }

        private void SetUpAndSpawnItem()
        {
            GetOnCostChanged(null);
            if (bsItem != null) bsItem.Select((int)Data.TypeItem);
            DestroyItem();
            var allCost = Data.Unlockable.UnlockCosts;
            for (int i = 0; i < allCost.Count; i++)
            {
                var costItem = allCost[i];
                var visualResolver = costItem.VisualResolver;
                if (visualResolver == null) continue;
                var iView = visualResolver.GetCostView(costItem, EResourceCostView.SingleLine);
                var itemViewPrefab = iView as View;
                if (itemViewPrefab == null || parentSpawnCost == null) continue;
                var instance = Instantiate(itemViewPrefab, parentSpawnCost);
                IView iViewInstance = instance;
                iViewInstance.Setup(costItem);
                _spawnerCostView.Add(instance);
            }
        }

        private void DestroyItem()
        {
            for (int i = _spawnerCostView.Count - 1; i >= 0; i--)
            {
                if (_spawnerCostView[i] == null) continue;
                IView view = _spawnerCostView[i];
                view.Teardown();
                Destroy(_spawnerCostView[i].gameObject);
            }
            _spawnerCostView.Clear();
        }

        public void OnClickUnlock()
        {
            if (!CanUnlock()) return;
            var data = Data.Unlockable.UnlockCosts;
            for (int i = 0; i < data.Count; i++)
            {
                var costItem = data[i];
                costItem.TrySpend();
            }
            Data.Unlockable.IsUnlocked.Set(true);
            OnDoneCall();
        }

        private void OnDoneCall()
        {
        }
    }
}
