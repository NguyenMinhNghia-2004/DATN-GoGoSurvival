using System;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// A single "buy item cards with Gold" offer shown in the shop: spend <see cref="price"/>
    /// Gold from <see cref="goldPool"/> to gain <see cref="amount"/> cards of <see cref="item"/>
    /// (added to <c>item.CardPool</c>). Rarity and icon are read from the item.
    /// </summary>
    [Serializable]
    public class ShopShardOffer
    {
        [SerializeField] private ItemConfig item;
        [SerializeField] private ResourcePool goldPool;
        [SerializeField] private int price = 100;
        [SerializeField] private int amount = 5;

        public ItemConfig Item => item;
        public ResourcePool GoldPool => goldPool;
        public ResourcePool CardPool => item != null ? item.CardPool : null;
        public ERarity Rarity => item != null ? item.Rarity : ERarity.Rare;
        public Sprite Icon => item != null ? item.Sprite : null;
        public int Price => price;
        public int Amount => amount;

        public ShopShardOffer() { }

        public ShopShardOffer(ItemConfig item, ResourcePool goldPool, int price, int amount)
        {
            this.item = item;
            this.goldPool = goldPool;
            this.price = price;
            this.amount = amount;
        }
    }
}
