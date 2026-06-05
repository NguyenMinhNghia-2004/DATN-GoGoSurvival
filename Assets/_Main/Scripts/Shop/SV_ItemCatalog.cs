using System;
using System.Collections.Generic;
using Luzart;
using UnityEngine;

/// <summary>
/// Data-driven catalog of shop/equipment items (the IO_Training "ItemConfig list" lesson,
/// flattened to what GoGoSurvival actually needs). Authorable in the Inspector and loaded
/// at runtime from Resources so no scene wiring is required.
/// Path: Assets/_Main/Resources/Shop/SV_ItemCatalog.asset (loaded as "Shop/SV_ItemCatalog").
/// </summary>
[Serializable]
public class SV_ItemEntry
{
    public string id;                  // stable key, e.g. "Eq_Kunai"
    public string displayName;
    public Sprite icon;
    public ETypeItem slot;             // Weapon/Armor/Necklace/Belt/Gloves/Shoes
    public ERarity rarity;
    public int priceCoins = 100;
    public StatType statType = StatType.ATK;
    public double statAmount = 10;
    public StatsBehavior.StatBonusMode mode = StatsBehavior.StatBonusMode.Additive;
}

[CreateAssetMenu(fileName = "SV_ItemCatalog", menuName = "SurvivorV2/Item Catalog")]
public class SV_ItemCatalog : ScriptableObject
{
    [SerializeField] private List<SV_ItemEntry> entries = new List<SV_ItemEntry>();
    public IReadOnlyList<SV_ItemEntry> Entries => entries;

    private static SV_ItemCatalog _cached;

    /// <summary>Lazy-load the singleton catalog asset from Resources.</summary>
    public static SV_ItemCatalog Load()
    {
        if (_cached == null)
            _cached = Resources.Load<SV_ItemCatalog>("Shop/SV_ItemCatalog");
        return _cached;
    }

    public SV_ItemEntry GetById(string id)
    {
        for (int i = 0; i < entries.Count; i++)
            if (entries[i] != null && entries[i].id == id) return entries[i];
        return null;
    }
}
