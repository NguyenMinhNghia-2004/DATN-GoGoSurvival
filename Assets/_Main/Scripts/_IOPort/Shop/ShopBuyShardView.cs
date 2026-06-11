using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Luzart
{
    /// <summary>
    /// One shard offer card in the shop: shows the rarity frame, the player's current
    /// shard count for that rarity, the gold price, and a Buy button. Buying spends Gold
    /// from the offer's gold pool and adds shards to the offer's shard pool.
    /// </summary>
    public class ShopBuyShardView : ViewT<ShopShardOffer>
    {
        [SerializeField] private Image imFrame;
        [SerializeField] private TMP_Text txtCount;
        [SerializeField] private TMP_Text txtPrice;

        private INumber _shardNumber;

        protected override void OnSetup()
        {
            base.OnSetup();
            if (imFrame != null)
            {
                var resolver = FindRarityResolver();
                if (resolver != null) imFrame.sprite = resolver.GetSpriteByRarity(Data.Rarity);
            }
            if (txtPrice != null) txtPrice.text = $"{Data.Price}";

            if (Data.ShardPool != null)
            {
                _shardNumber = ((IResourcePool)Data.ShardPool).Value;
                if (_shardNumber != null) _shardNumber.Changed += OnShardChanged;
            }
            RefreshCount();
        }

        protected override void OnTeardown()
        {
            base.OnTeardown();
            if (_shardNumber != null) _shardNumber.Changed -= OnShardChanged;
            _shardNumber = null;
        }

        private void OnShardChanged(INumber number) => RefreshCount();

        private void RefreshCount()
        {
            if (txtCount == null || Data == null || Data.ShardPool == null) return;
            int count = (int)((IResourcePool)Data.ShardPool).Value.Value;
            txtCount.text = $"x{count}";
        }

        public void OnClickBuy()
        {
            if (Data == null || Data.ShardPool == null || Data.GoldPool == null) return;
            double gold = ((IResourcePool)Data.GoldPool).Value.Value;
            if (gold < Data.Price) return;
            if (!Data.GoldPool.TryRemove(Data.Price)) return;
            Data.ShardPool.Add(Data.Amount);
        }

        private RaritySpriteResolver FindRarityResolver()
        {
            if (SceneRootManager.Instance == null || SceneRootManager.Instance._domain == null) return null;
            var all = SceneRootManager.Instance._domain.GetAll<IVisualResolver>();
            foreach (var resolver in all)
            {
                if (resolver is RaritySpriteResolver r) return r;
            }
            return null;
        }
    }
}
