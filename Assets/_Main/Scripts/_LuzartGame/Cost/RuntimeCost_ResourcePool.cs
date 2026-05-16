using System;
using UnityEngine.UIElements;
namespace Luzart
{
    public class RuntimeCost_ResourcePool : RuntimeCost, IResourceCost
    {
        private IResourcePool _resourcePool;
        private INumber _amount;
        private IBool _isSpendable;
        IResourcePool IResourceCost.ResourcePool => _resourcePool;
        INumber IResourceCost.Amount => _amount;
        public RuntimeCost_ResourcePool(IResourcePool resourcePool, INumber spendAmount)
        {
            _resourcePool = resourcePool;
            _amount = spendAmount;
            _isSpendable = new Runtime_BoolCompareNumber(spendAmount, ComparisonMode.LEqual, resourcePool.Value);
        }
        protected override IBool DoGetIsSpendable()
        {
            return _isSpendable;
        }
        protected override bool DoTrySpend()
        {
            double valueResource = _resourcePool.Value.Value;
            double valueSpend = _amount.Value;
            bool isSpendable = _isSpendable.Value;
            if (isSpendable)
            {
                double newValue = valueResource - valueSpend;
                _resourcePool.Set(newValue);
            }
            return isSpendable;
        }
        protected override ICostVisualResolver DoGetVisualResolver()
        {
            var icostVisualResolver =  _resourcePool.Definition.GetVisualResolver();
            return icostVisualResolver;
        }
    }
}
