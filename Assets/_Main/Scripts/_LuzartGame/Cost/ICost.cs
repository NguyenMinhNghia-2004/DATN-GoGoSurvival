using System;
namespace Luzart
{
    public interface ICost
    {
        ICostVisualResolver VisualResolver { get; }
        IBool IsSpendable { get; }
        bool TrySpend();
    }
    public interface IResourceCost : ICost
    {
        IResourcePool ResourcePool { get; }
        INumber Amount { get; }
    }
}
