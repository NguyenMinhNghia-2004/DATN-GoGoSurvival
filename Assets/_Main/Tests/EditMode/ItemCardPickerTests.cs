using NUnit.Framework;
using Luzart;

public class ItemCardPickerTests
{
    // weights: index 0 (Rare)=70, 1 (Epic)=25, 2 (Legend)=5
    static readonly float[] W = { 70f, 25f, 5f };

    [Test]
    public void Pick_RollAtZero_ReturnsFirstBucket()
    {
        int idx = ItemCardPicker.PickWeightedIndex(W, 0f);
        Assert.AreEqual(0, idx);
    }

    [Test]
    public void Pick_RollInLastBucket_ReturnsLast()
    {
        // total=100; roll 97 -> Legend bucket (95..100)
        int idx = ItemCardPicker.PickWeightedIndex(W, 97f);
        Assert.AreEqual(2, idx);
    }

    [Test]
    public void Pick_RollInMiddleBucket_ReturnsMiddle()
    {
        // roll 80 -> Epic bucket (70..95)
        int idx = ItemCardPicker.PickWeightedIndex(W, 80f);
        Assert.AreEqual(1, idx);
    }

    [Test]
    public void Pick_AllZeroWeights_ReturnsMinusOne()
    {
        int idx = ItemCardPicker.PickWeightedIndex(new float[] { 0f, 0f }, 0.5f);
        Assert.AreEqual(-1, idx);
    }
}
