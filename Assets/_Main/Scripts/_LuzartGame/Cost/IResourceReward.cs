namespace Luzart
{
    public interface IReward
    {
        ICostVisualResolver VisualResolver { get; }
    }
    public interface IResourceReward
    {
        IResourcePool ResourcePool { get; }
        INumber Amount { get; }
    }
    public class Runtime_ResourceReward : IResourceReward
    {
        public Runtime_ResourceReward(IResourcePool resourcePool, INumber amount)
        {
            this._resourcePool = resourcePool;
            this._amount = amount;
        }
        public Runtime_ResourceReward(IResourcePool resourcePool, double amount)
        {
            this._resourcePool = resourcePool;
            this._amount = new Number(amount);
        }
        private IResourcePool _resourcePool;
        private INumber _amount;
        IResourcePool IResourceReward.ResourcePool => _resourcePool;
        INumber IResourceReward.Amount => _amount;
    }
}
