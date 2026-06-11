namespace Luzart
{
    /// <summary>
    /// Pure, deterministic weighted bucket selection. No UnityEngine dependency so it is
    /// unit-testable and lives in the standalone IOPort.Logic assembly.
    /// Caller supplies the random roll in [0, sum(weights)).
    /// </summary>
    public static class ItemCardPicker
    {
        /// <summary>
        /// Returns the index of the bucket that <paramref name="roll"/> lands in,
        /// where roll is in [0, total). Returns -1 if all weights are non-positive.
        /// </summary>
        public static int PickWeightedIndex(float[] weights, float roll)
        {
            if (weights == null || weights.Length == 0) return -1;
            float total = 0f;
            for (int i = 0; i < weights.Length; i++)
                if (weights[i] > 0f) total += weights[i];
            if (total <= 0f) return -1;
            if (roll < 0f) roll = 0f;
            if (roll >= total) roll = total - 0.0001f;
            float acc = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] <= 0f) continue;
                acc += weights[i];
                if (roll < acc) return i;
            }
            for (int i = weights.Length - 1; i >= 0; i--)
                if (weights[i] > 0f) return i;
            return -1;
        }
    }

    /// <summary>Pure affordability check, separated for unit testing.</summary>
    public static class ShopBuyLogic
    {
        public static bool CanAfford(double gold, int price) => gold >= price;
    }
}
