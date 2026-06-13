using UnityEngine;

namespace Luzart
{
    public class ObjectView : ViewT<ObjectViewData>
    {
        public ItemConfigAndAmountData ItemConfigAndAmountData => Data.ItemConfigAndAmountData;

        public ItemResourcePoolData ItemShardData
        {
            get
            {
                if (Data.ItemResourcePoolData != null
                    && Data.ItemResourcePoolData.ResourcePool != null
                    && Data.ItemResourcePoolData.ResourcePool.Definition != null
                    && Data.ItemResourcePoolData.ResourcePool.Definition.EType != ETypeResourceDefinition.Shard)
                {
                    return null;
                }
                return Data.ItemResourcePoolData;
            }
        }

        public ItemResourcePoolData ItemResoucePoolData => Data.ItemResourcePoolData;

        [SerializeField] private Transform container;
        [SerializeField] private ItemConfigView itemConfigView;
        [SerializeField] private ItemDefinitionView itemDefinitionView;
        [SerializeField] private ItemShardView resourcePoolView;

        private Transform Container
        {
            get
            {
                if (container == null)
                {
                    container = this.transform;
                }
                return container;
            }
        }

        protected override void OnSetup()
        {
            base.OnSetup();
            if (ItemConfigAndAmountData != null && itemConfigView != null)
            {
                var view = Instantiate(itemConfigView, Container);
                view.Setup(ItemConfigAndAmountData);
            }
            if (ItemShardData != null && resourcePoolView != null)
            {
                var view = Instantiate(resourcePoolView, Container);
                view.Setup(ItemShardData);
            }
            else if (ItemResoucePoolData != null && itemDefinitionView != null)
            {
                var view = Instantiate(itemDefinitionView, Container);
                view.Setup(ItemResoucePoolData);
            }
        }
    }

    public class ObjectViewData
    {
        public ItemConfigAndAmountData ItemConfigAndAmountData { get; private set; }
        public ItemResourcePoolData ItemResourcePoolData { get; private set; }

        public ObjectViewData(ItemConfig itemConfig, int amount = -1)
        {
            ItemConfigAndAmountData = new ItemConfigAndAmountData(itemConfig, amount);
        }

        public ObjectViewData(ResourcePool resourcePool, double amount = -1)
        {
            ItemResourcePoolData = new ItemResourcePoolData(resourcePool, amount);
        }

        public ObjectViewData(IResourcePool resourcePool, INumber amount = null)
        {
            ItemResourcePoolData = new ItemResourcePoolData(resourcePool, amount);
        }
    }

    public class ItemResourcePoolData
    {
        public IResourcePool ResourcePool { get; }
        public double Amount { get; private set; } = -1;

        public ItemResourcePoolData(IResourcePool resourcePool, double amount = -1)
        {
            ResourcePool = resourcePool;
            Amount = amount;
        }

        public ItemResourcePoolData(IResourcePool resourcePool, INumber amount = null)
        {
            ResourcePool = resourcePool;
            if (amount != null)
            {
                Amount = amount.Value;
            }
        }
    }

    public class ItemConfigAndAmountData
    {
        public ItemConfig ItemConfig { get; }
        public double Amount { get; private set; } = -1;

        public ItemConfigAndAmountData(ItemConfig itemConfig, double amount = -1)
        {
            ItemConfig = itemConfig;
            Amount = amount;
        }
    }
}
