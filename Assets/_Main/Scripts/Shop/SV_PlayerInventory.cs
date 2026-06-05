using System;
using System.Collections.Generic;
using Luzart;
using UnityEngine;

/// <summary>
/// Runtime player inventory: which items are owned and which are equipped per slot.
/// Persisted to PlayerPrefs (JSON) so purchases/equips survive across sessions.
/// Plain C# singleton — no Domain dependency, safe to use from Main Menu UI.
/// Modeled on IO_Training's ItemConfigsOwned + AssetEquipmentSlot, simplified.
/// </summary>
public class SV_PlayerInventory
{
    private const string SaveKey = "sv_inventory_v1";

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

    /// <summary>Fires whenever ownership or equipped state changes.</summary>
    public event Action OnChanged;

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

    /// <summary>Spend coins via CurrencyManager and mark item owned. Returns false if
    /// already owned or insufficient coins.</summary>
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

    // ── persistence ────────────────────────────────────────────────
    [Serializable]
    private class SaveData
    {
        public List<string> owned = new List<string>();
        public List<string> equipSlots = new List<string>(); // parallel arrays
        public List<string> equipIds = new List<string>();
    }

    public void Save()
    {
        var d = new SaveData();
        d.owned.AddRange(Owned);
        foreach (var kv in Equipped)
        {
            d.equipSlots.Add(kv.Key.ToString());
            d.equipIds.Add(kv.Value);
        }
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
            }
        }
        return inv;
    }
}
