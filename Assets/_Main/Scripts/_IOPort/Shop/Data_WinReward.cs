using System.Collections.Generic;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Grants a random reward when a Classic run is WON. Listens for the
    /// <see cref="Data_ClassicEndGame"/> broadcast (mirrors <c>SV_EndGameBridge</c>).
    ///
    /// Roll: with <see cref="cardChancePercent"/> probability, unlock a random still-locked
    /// card directly; otherwise grant a random amount of shards to a random shard pool.
    /// The grant is silent — the unlocked card simply appears in the shop, shards accumulate
    /// toward the existing Gold+Shard unlock cost.
    /// </summary>
    public class Data_WinReward : AbstractScriptableContent
    {
        [Header("Card vs shard roll")]
        [Range(0f, 100f)]
        [SerializeField] private float cardChancePercent = 15f;

        [Header("Shard reward (when not a card)")]
        [SerializeField] private int shardMin = 3;
        [SerializeField] private int shardMax = 8;
        [SerializeField] private List<ResourcePool> shardPools = new List<ResourcePool>();

        [Header("Refs")]
        [SerializeField] private InventoryItemData inventoryItemData;

        private bool _registered;

        protected override void DoInitialize()
        {
            base.DoInitialize();
            if (inventoryItemData == null && _domain != null)
                inventoryItemData = _domain.Get<InventoryItemData>();
            Broadcaster.Register<Data_ClassicEndGame>(OnEndGame);
            _registered = true;
        }

        protected override void DoTerminate()
        {
            base.DoTerminate();
            if (_registered)
            {
                Broadcaster.Unregister<Data_ClassicEndGame>(OnEndGame);
                _registered = false;
            }
        }

        private void OnEndGame(Data_ClassicEndGame data)
        {
            if (!data.IsWin) return;

            bool wantCard = Random.Range(0f, 100f) < cardChancePercent;
            if (wantCard && TryGrantRandomCard()) return;
            GrantRandomShards();
        }

        private bool TryGrantRandomCard()
        {
            if (inventoryItemData == null) return false;
            var locked = inventoryItemData.GetAllItemConfigDontBuy();
            if (locked == null || locked.Count == 0) return false;
            var card = locked[Random.Range(0, locked.Count)];
            if (card == null || card.Unlockable == null) return false;
            card.Unlockable.IsUnlocked.Set(true);
            return true;
        }

        private void GrantRandomShards()
        {
            if (shardPools == null || shardPools.Count == 0) return;
            var pool = shardPools[Random.Range(0, shardPools.Count)];
            if (pool == null) return;
            int lo = Mathf.Min(shardMin, shardMax);
            int hi = Mathf.Max(shardMin, shardMax);
            int amount = Random.Range(lo, hi + 1);
            if (amount <= 0) return;
            pool.Add(amount);
        }
    }
}
