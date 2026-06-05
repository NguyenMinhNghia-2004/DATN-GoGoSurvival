using Luzart;
using UnityEngine;

/// <summary>
/// Applies the player's persisted equipment bonuses (chosen in the Equipment menu) to the
/// player's StatsBehavior at gameplay start. The Equipment menu only records which items are
/// equipped (SV_PlayerInventory); the actual stat math happens here, once, when the player
/// entity initializes — analogous to IO_Training activating an equipped item's modifier group.
/// </summary>
public static class SV_EquipmentStatApplier
{
    public static void ApplyTo(StatsBehavior stats)
    {
        if (stats == null) return;
        var catalog = SV_ItemCatalog.Load();
        if (catalog == null) return;

        var inv = SV_PlayerInventory.Instance;
        int applied = 0;
        foreach (var kv in inv.Equipped)
        {
            var entry = catalog.GetById(kv.Value);
            if (entry == null) continue;
            stats.ApplyStatBonus(entry.statType, entry.statAmount, entry.mode);
            applied++;
        }

        if (applied > 0)
        {
            // Reflect any HPMax bonus in the starting HP (player spawns at full, boosted HP).
            stats.RestoreHP();
            Debug.Log($"[SV_Equipment] Applied {applied} equipped item bonus(es) to player stats.");
        }
    }
}
