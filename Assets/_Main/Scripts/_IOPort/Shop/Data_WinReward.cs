using System.Collections.Generic;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// On a WON Classic run, grants cards of a random item, weighted by the item's rarity.
    /// Cards accumulate toward that item's unlock cost and, once unlocked, its upgrade costs.
    /// Silent grant (no popup).
    /// </summary>
    public class Data_WinReward : AbstractScriptableContent
    {
        [Header("Card grant (per win)")]
        [SerializeField] private int cardMin = 3;
        [SerializeField] private int cardMax = 8;

        [Header("Rarity weights (Rare / Epic / Legend)")]
        [SerializeField] private float weightRare = 70f;
        [SerializeField] private float weightEpic = 25f;
        [SerializeField] private float weightLegend = 5f;

        private bool _registered;

        protected override void DoInitialize()
        {
            base.DoInitialize();
            Broadcaster.Register<Data_ClassicEndGame>(OnEndGame);
            _registered = true;
        }

        protected override void DoTerminate()
        {
            base.DoTerminate();
            if (_registered) { Broadcaster.Unregister<Data_ClassicEndGame>(OnEndGame); _registered = false; }
        }

        private void OnEndGame(Data_ClassicEndGame data)
        {
            if (!data.IsWin) return;
            GrantRandomItemCards();
        }

        private void GrantRandomItemCards()
        {
            if (_domain == null) return;
            var all = _domain.GetAll<ItemConfig>();
            if (all == null || all.Count == 0) return;

            // pick a rarity bucket by weight
            float[] weights = { weightRare, weightEpic, weightLegend };
            float total = weights[0] + weights[1] + weights[2];
            if (total <= 0f) return;
            int rarityIdx = ItemCardPicker.PickWeightedIndex(weights, Random.Range(0f, total));
            if (rarityIdx < 0) return;
            var rarity = (ERarity)rarityIdx;

            // collect items of that rarity that have a card pool; fall back to any item with a pool
            var pool = new List<ItemConfig>();
            for (int i = 0; i < all.Count; i++)
                if (all[i] != null && all[i].Rarity == rarity && all[i].CardPool != null) pool.Add(all[i]);
            if (pool.Count == 0)
                for (int i = 0; i < all.Count; i++)
                    if (all[i] != null && all[i].CardPool != null) pool.Add(all[i]);
            if (pool.Count == 0) return;

            var item = pool[Random.Range(0, pool.Count)];
            int lo = Mathf.Min(cardMin, cardMax);
            int hi = Mathf.Max(cardMin, cardMax);
            int amount = Random.Range(lo, hi + 1);
            if (amount <= 0) return;
            item.CardPool.Add(amount);
        }
    }
}
