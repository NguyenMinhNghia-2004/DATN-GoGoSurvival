using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Luzart
{
    /// <summary>
    /// One item-card offer card in the shop. Shows the item icon over a rarity-colored frame,
    /// the player's current card count for that item, a Gold price (with Gold icon), and a Buy
    /// button. Buying spends Gold and adds cards to the item's own card pool.
    /// The shop prefab layout is unchanged; Icon + GoldIcon are optional additive elements.
    /// </summary>
    public class ShopBuyShardView : ViewT<ShopShardOffer>
    {
        [SerializeField] private Image imFrame;    // rarity-colored backing (existing)
        [SerializeField] private Image imIcon;     // NEW: the item card image
        [SerializeField] private Image imGoldIcon; // NEW: gold coin next to price
        [SerializeField] private Sprite goldSprite;
        [SerializeField] private TMP_Text txtCount;
        [SerializeField] private TMP_Text txtPrice;

        private INumber _cardNumber;

        protected override void OnSetup()
        {
            base.OnSetup();
            if (imFrame != null)
            {
                var resolver = FindRarityResolver();
                if (resolver != null) imFrame.sprite = resolver.GetSpriteByRarity(Data.Rarity);
            }
            if (imIcon != null) imIcon.sprite = Data.Icon;
            if (imGoldIcon != null && goldSprite != null) imGoldIcon.sprite = goldSprite;
            if (txtPrice != null) txtPrice.text = $"{Data.Price}";

            if (Data.CardPool != null)
            {
                _cardNumber = ((IResourcePool)Data.CardPool).Value;
                if (_cardNumber != null) _cardNumber.Changed += OnCardChanged;
            }
            RefreshCount();
        }

        protected override void OnTeardown()
        {
            base.OnTeardown();
            if (_cardNumber != null) _cardNumber.Changed -= OnCardChanged;
            _cardNumber = null;
        }

        private void OnCardChanged(INumber number) => RefreshCount();

        private void RefreshCount()
        {
            if (txtCount == null || Data == null || Data.CardPool == null) return;
            int count = (int)((IResourcePool)Data.CardPool).Value.Value;
            txtCount.text = $"x{count}";
        }

        public void OnClickBuy()
        {
            if (Data == null || Data.CardPool == null || Data.GoldPool == null) return;
            double gold = ((IResourcePool)Data.GoldPool).Value.Value;
            if (!ShopBuyLogic.CanAfford(gold, Data.Price)) return;
            if (!Data.GoldPool.TryRemove(Data.Price)) return;
            Data.CardPool.Add(Data.Amount);
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
