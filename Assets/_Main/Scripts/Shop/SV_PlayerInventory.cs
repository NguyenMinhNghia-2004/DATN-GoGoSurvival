using System;
using System.Collections.Generic;
using Luzart;
using UnityEngine;

/// <summary>
/// Runtime player inventory: owned items, equipped item per slot, per-item upgrade LEVEL, and
/// per-rarity SHARD stock. Persisted to PlayerPrefs (JSON). Plain C# singleton — usable from
/// menu UI without the Domain. Models IO_Training's ItemConfigsOwned + AssetEquipmentSlot +
/// AssetLevel (level + level-up cost), simplified to coins + rarity shards.
/// </summary>
public class SV_PlayerInventory
{
    private const string SaveKey = "sv_inventory_v2";
    private const int StartingShardsPerRarity = 40; // seeded once so upgrades are testable

    private static SV_PlayerInventory _instance;
    public static SV_PlayerInventory Instance
    {
        get
        {
            if (_instance == null) _instance = BuildFromDisk();
            return _instance;
        }
    }

    public readonly HashSet<string> Owned = new HashSet<string>();
    public readonly Dictionary<ETypeItem, string> Equipped = new Dictionary<ETypeItem, string>();
    public readonly Dictionary<string, int> Levels = new Dictionary<string, int>();   // id → level
    public readonly Dictionary<ERarity, int> Shards = new Dictionary<ERarity, int>(); // rarity → count

    public event Action OnChanged;

    // ── ownership / equip ───────────────────────────────────────────
    public bool IsOwned(string id) => !string.IsNullOrEmpty(id) && Owned.Contains(id);

    public bool IsEquipped(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        foreach (var kv in Equipped)
            if (kv.Value == id) return true;
        return false;
    }

    public string GetEquipped(ETypeItem slot) =>
        Equipped.TryGetValue(slot, out var id) ? id : null;

    public int GetLevel(string id) => Levels.TryGetValue(id, out var lv) ? lv : 0;

    public int GetShards(ERarity rarity) => Shards.TryGetValue(rarity, out var n) ? n : 0;

    public void AddShards(ERarity rarity, int amount)
    {
        if (amount == 0) return;
        Shards[rarity] = Mathf.Max(0, GetShards(rarity) + amount);
        Save();
        OnChanged?.Invoke();
    }

    /// <summary>Unlock (buy) an item by spending coins.</summary>
    public bool TryBuy(SV_ItemEntry entry)
    {
        if (entry == null || IsOwned(entry.id)) return false;
        var cm = CurrencyManager.Instance;
        if (cm.Coins < entry.priceCoins) return false;
        cm.AddCoin(-entry.priceCoins);
        Owned.Add(entry.id);
        Save();
        OnChanged?.Invoke();
        return true;
    }

    public void Equip(SV_ItemEntry entry)
    {
        if (entry == null || !IsOwned(entry.id)) return;
        Equipped[entry.slot] = entry.id;
        Save();
        OnChanged?.Invoke();
    }

    public void Unequip(ETypeItem slot)
    {
        if (Equipped.Remove(slot))
        {
            Save();
            OnChanged?.Invoke();
        }
    }

    // ── upgrade (level + cost: coins + rarity shards) ───────────────
    public static int UpgradeCoinCost(int level) => 50 * (level + 1);
    public static int UpgradeShardCost(int level) => 2 * (level + 1);

    public bool IsMaxLevel(SV_ItemEntry entry) => entry != null && GetLevel(entry.id) >= entry.maxLevel;

    public bool CanUpgrade(SV_ItemEntry entry)
    {
        if (entry == null || IsMaxLevel(entry)) return false;
        int level = GetLevel(entry.id);
        return CurrencyManager.Instance.Coins >= UpgradeCoinCost(level)
               && GetShards(entry.rarity) >= UpgradeShardCost(level);
    }

    public bool TryUpgrade(SV_ItemEntry entry)
    {
        if (!CanUpgrade(entry)) return false;
        int level = GetLevel(entry.id);
        CurrencyManager.Instance.AddCoin(-UpgradeCoinCost(level));
        Shards[entry.rarity] = GetShards(entry.rarity) - UpgradeShardCost(level);
        Levels[entry.id] = level + 1;
        Save();
        OnChanged?.Invoke();
        return true;
    }

    /// <summary>Effective stat bonus at a given level (base scaled +20% per level).</summary>
    public static double EffectiveStatAmount(SV_ItemEntry entry, int level) =>
        entry == null ? 0 : entry.statAmount * (1.0 + 0.2 * level);

    // ── persistence ────────────────────────────────────────────────
    [Serializable]
    private class SaveData
    {
        public List<string> owned = new List<string>();
        public List<string> equipSlots = new List<string>();
        public List<string> equipIds = new List<string>();
        public List<string> levelIds = new List<string>();
        public List<int> levelValues = new List<int>();
        public List<string> shardRarities = new List<string>();
        public List<int> shardCounts = new List<int>();
        public bool seeded;
    }

    public void Save()
    {
        var d = new SaveData { seeded = true };
        d.owned.AddRange(Owned);
        foreach (var kv in Equipped) { d.equipSlots.Add(kv.Key.ToString()); d.equipIds.Add(kv.Value); }
        foreach (var kv in Levels) { d.levelIds.Add(kv.Key); d.levelValues.Add(kv.Value); }
        foreach (var kv in Shards) { d.shardRarities.Add(kv.Key.ToString()); d.shardCounts.Add(kv.Value); }
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(d));
        PlayerPrefs.Save();
    }

    private static SV_PlayerInventory BuildFromDisk()
    {
        var inv = new SV_PlayerInventory();
        var json = PlayerPrefs.GetString(SaveKey, "");
        if (!string.IsNullOrEmpty(json))
        {
            var d = JsonUtility.FromJson<SaveData>(json);
            if (d != null)
            {
                foreach (var o in d.owned) inv.Owned.Add(o);
                for (int i = 0; i < d.equipSlots.Count && i < d.equipIds.Count; i++)
                    if (Enum.TryParse<ETypeItem>(d.equipSlots[i], out var slot))
                        inv.Equipped[slot] = d.equipIds[i];
                for (int i = 0; i < d.levelIds.Count && i < d.levelValues.Count; i++)
                    inv.Levels[d.levelIds[i]] = d.levelValues[i];
                for (int i = 0; i < d.shardRarities.Count && i < d.shardCounts.Count; i++)
                    if (Enum.TryParse<ERarity>(d.shardRarities[i], out var r))
                        inv.Shards[r] = d.shardCounts[i];
                if (d.seeded) return inv;
            }
        }
        // First ever run — seed shards so upgrades are testable.
        foreach (ERarity r in Enum.GetValues(typeof(ERarity)))
            inv.Shards[r] = StartingShardsPerRarity;
        inv.Save();
        return inv;
    }
}
