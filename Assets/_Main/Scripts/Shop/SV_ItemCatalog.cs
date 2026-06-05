using System;
using System.Collections.Generic;
using Luzart;
using UnityEngine;

/// <summary>
/// Data-driven catalog of shop/equipment items (the IO_Training "ItemConfig list" lesson,
/// flattened to what GoGoSurvival needs). Authorable in the Inspector and loaded at runtime
/// from Resources. Path: Assets/_Main/Resources/Shop/SV_ItemCatalog.asset.
/// </summary>
[Serializable]
public class SV_ItemEntry
{
    public string id;                  // stable key, e.g. "Eq_Kunai"
    public string displayName;
    public Sprite icon;                // assigned later; null → fallback to template default
    public ETypeItem slot;             // Weapon/Armor/Necklace/Belt/Gloves/Shoes
    public ERarity rarity;
    public int priceCoins = 100;
    public int maxLevel = 5;           // level 0..maxLevel
    public StatType statType = StatType.ATK;
    public double statAmount = 10;     // base (level 0) bonus
    public StatsBehavior.StatBonusMode mode = StatsBehavior.StatBonusMode.Additive;
}

/// <summary>Per-rarity visual (frame sprite + tint). Sprites assigned later by the user;
/// a built-in fallback colour keeps items readable until then.</summary>
[Serializable]
public class SV_RarityVisual
{
    public ERarity rarity;
    public Sprite frame;
    public Color color = Color.white;
}

[CreateAssetMenu(fileName = "SV_ItemCatalog", menuName = "SurvivorV2/Item Catalog")]
public class SV_ItemCatalog : ScriptableObject
{
    [SerializeField] private List<SV_ItemEntry> entries = new List<SV_ItemEntry>();
    [SerializeField] private List<SV_RarityVisual> rarityVisuals = new List<SV_RarityVisual>();
    public IReadOnlyList<SV_ItemEntry> Entries => entries;

    private static SV_ItemCatalog _cached;

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

    public Sprite GetRarityFrame(ERarity rarity)
    {
        for (int i = 0; i < rarityVisuals.Count; i++)
            if (rarityVisuals[i] != null && rarityVisuals[i].rarity == rarity) return rarityVisuals[i].frame;
        return null;
    }

    /// <summary>Configured tint, or a sensible built-in fallback colour per rarity.</summary>
    public Color GetRarityColor(ERarity rarity)
    {
        for (int i = 0; i < rarityVisuals.Count; i++)
            if (rarityVisuals[i] != null && rarityVisuals[i].rarity == rarity && rarityVisuals[i].frame != null)
                return rarityVisuals[i].color;
        return FallbackRarityColor(rarity);
    }

    public static Color FallbackRarityColor(ERarity rarity)
    {
        switch (rarity)
        {
            case ERarity.Epic:   return new Color(0.55f, 0.30f, 0.75f, 1f); // purple
            case ERarity.Legend: return new Color(0.85f, 0.65f, 0.20f, 1f); // gold
            default:             return new Color(0.30f, 0.55f, 0.80f, 1f); // blue (Rare)
        }
    }
}
