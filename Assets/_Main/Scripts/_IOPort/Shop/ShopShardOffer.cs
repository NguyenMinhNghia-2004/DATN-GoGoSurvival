using System;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// A single "buy shards with coin (Gold)" offer shown in the shop: spend
    /// <see cref="price"/> from <see cref="goldPool"/> to gain <see cref="amount"/> shards
    /// of <see cref="shardPool"/>.
    /// </summary>
    [Serializable]
    public class ShopShardOffer
    {
        [SerializeField] private ResourcePool shardPool;
        [SerializeField] private ResourcePool goldPool;
        [SerializeField] private ERarity rarity;
        [SerializeField] private int price = 100;
        [SerializeField] private int amount = 1;

        public ResourcePool ShardPool => shardPool;
        public ResourcePool GoldPool => goldPool;
        public ERarity Rarity => rarity;
        public int Price => price;
        public int Amount => amount;

        public ShopShardOffer() { }

        public ShopShardOffer(ResourcePool shardPool, ResourcePool goldPool, ERarity rarity, int price, int amount)
        {
            this.shardPool = shardPool;
            this.goldPool = goldPool;
            this.rarity = rarity;
            this.price = price;
            this.amount = amount;
        }
    }
}
