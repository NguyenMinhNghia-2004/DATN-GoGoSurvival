using NUnit.Framework;
using Luzart;

public class ShopBuyShardLogicTests
{
    [Test]
    public void CanAfford_True_WhenGoldAtLeastPrice()
    {
        Assert.IsTrue(ShopBuyLogic.CanAfford(gold: 100, price: 100));
        Assert.IsTrue(ShopBuyLogic.CanAfford(gold: 150, price: 100));
    }

    [Test]
    public void CanAfford_False_WhenGoldBelowPrice()
    {
        Assert.IsFalse(ShopBuyLogic.CanAfford(gold: 99, price: 100));
    }
}
