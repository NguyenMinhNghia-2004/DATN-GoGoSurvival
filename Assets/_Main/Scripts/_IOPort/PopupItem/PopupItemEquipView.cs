using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Luzart.NewBase;

namespace Luzart
{
    public class PopupItemEquipView : ViewT<PopupItemEquipVM>
    {
        [SerializeField] private TMP_Text txtName;
        [SerializeField] private TMP_Text txtDescription;
        [SerializeField] private ObjectView objectView;
        [SerializeField] private Button btnUnEquip, btnUpgrade, btnEquip;
        [SerializeField] private BaseSelect bsEquipButton;
        [SerializeField] private Transform parentSpawn;

        private List<View> spawnedCostViews = new();

        protected override void OnSetup()
        {
            base.OnSetup();
            Data.ItemConfig.Level.Changed += RefreshData;
            RefreshData(null);
        }

        protected override void OnTeardown()
        {
            base.OnTeardown();
            Data.ItemConfig.Level.Changed -= RefreshData;
            ClearSpawn();
        }

        private void RefreshData(ILevelable iLevelable)
        {
            if (txtName != null) txtName.text = Data.ItemConfig.NameItem;
            if (txtDescription != null) txtDescription.text = Data.GetUpgradeDetail();
            if (objectView != null) objectView.Setup(new ObjectViewData(Data.ItemConfig));
            if (bsEquipButton != null) bsEquipButton.Select(!Data.IsButtonInList());
            var levelUpCosts = Data.ItemConfig.Level.GetLevelUpCost();
            ClearSpawn();
            Spawner(levelUpCosts);
            bool isCanUpgradeLevel = Data.ItemConfig.Level.LevelIndex < Data.ItemConfig.Level.MaxLevelIndex - 1;
            bool isCanUpgradeSpendableCost = true;
            for (int i = 0; i < levelUpCosts.Count; i++)
            {
                var levelUp = levelUpCosts[i];
                bool isCanUpgradeEach = levelUp.IsSpendable.Value;
                if (!isCanUpgradeEach)
                {
                    isCanUpgradeSpendableCost = false;
                    break;
                }
            }
            bool isCanUpgrade = isCanUpgradeLevel && isCanUpgradeSpendableCost;
            if (btnUpgrade != null) btnUpgrade.gameObject.SetActive(isCanUpgrade);
        }

        private void Spawner(IReadOnlyList<ICost> levelUpCosts)
        {
            if (parentSpawn == null) return;
            int length = levelUpCosts.Count;
            for (int i = 0; i < length; i++)
            {
                var iVisual = levelUpCosts[i].VisualResolver;
                if (iVisual == null) continue;
                IView concreatePrefabs = iVisual.GetCostView(levelUpCosts[i], EResourceCostView.SingleLine);
                var item = concreatePrefabs as View;
                if (item == null) continue;
                var concreate = Instantiate(item, parentSpawn);
                IView view = concreate;
                view.Setup(levelUpCosts[i]);
                spawnedCostViews.Add(concreate);
            }
        }

        private void ClearSpawn()
        {
            for (int i = spawnedCostViews.Count - 1; i >= 0; i--)
            {
                IView view = spawnedCostViews[i];
                view.Teardown();
                Destroy(spawnedCostViews[i].gameObject);
            }
            spawnedCostViews.Clear();
        }

        public void OnClickUnEquip()
        {
            Data.RefreshEquippedItem(false);
        }

        public void OnClickEquip()
        {
            Data.RefreshEquippedItem(true);
        }

        public void OnClickUpgrade()
        {
            Data.UpgradeItem();
        }
    }
}
