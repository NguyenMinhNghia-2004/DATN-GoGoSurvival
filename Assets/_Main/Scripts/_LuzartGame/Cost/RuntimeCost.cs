using System;
namespace Luzart
{
    public abstract class RuntimeCost : ICost
    {
        IBool ICost.IsSpendable => DoGetIsSpendable();
        ICostVisualResolver ICost.VisualResolver => DoGetVisualResolver();
        bool ICost.TrySpend() => DoTrySpend();
        protected abstract IBool DoGetIsSpendable();
        protected abstract bool DoTrySpend();
        protected abstract ICostVisualResolver DoGetVisualResolver();
    }
}
