using System.Collections.Generic;
using Luzart;
using UnityEngine;

/// <summary>
/// Applies the player's persisted equipment bonuses (chosen in the Equipment menu) to the
/// player's StatsBehavior at the START of each run (ClassicModeController.StartGame). The
/// Equipment menu only records which items are equipped (SV_PlayerInventory); the actual stat
/// math happens here — analogous to IO_Training activating an equipped item's modifier group.
///
/// Idempotent: tracks the bonuses it applied last time and removes them before re-applying, so
/// repeated runs (or equipment changes between runs) never stack. Mirrors the delta semantics
/// of ZSkillBehavior_Stat.
/// </summary>
public static class SV_EquipmentStatApplier
{
    private struct Bonus
    {
        public StatType Stat;
        public double Amount;
        public StatsBehavior.StatBonusMode Mode;
    }

    private static StatsBehavior _lastStats;
    private static readonly List<Bonus> _lastApplied = new List<Bonus>();

    public static void ApplyTo(StatsBehavior stats)
    {
        if (stats == null) return;

        // Undo the previous application (only meaningful if it was the same StatsBehavior).
        if (ReferenceEquals(_lastStats, stats))
        {
            foreach (var b in _lastApplied)
                stats.RemoveStatBonus(b.Stat, b.Amount, b.Mode);
        }
        _lastApplied.Clear();
        _lastStats = stats;

        var catalog = SV_ItemCatalog.Load();
        if (catalog == null) return;

        var inv = SV_PlayerInventory.Instance;
        foreach (var kv in inv.Equipped)
        {
            var entry = catalog.GetById(kv.Value);
            if (entry == null) continue;
            double amount = SV_PlayerInventory.EffectiveStatAmount(entry, inv.GetLevel(entry.id));
            stats.ApplyStatBonus(entry.statType, amount, entry.mode);
            _lastApplied.Add(new Bonus { Stat = entry.statType, Amount = amount, Mode = entry.mode });
        }

        if (_lastApplied.Count > 0)
        {
            // Reflect any HPMax bonus in the starting HP (player spawns at full, boosted HP).
            stats.RestoreHP();
            Debug.Log($"[SV_Equipment] Applied {_lastApplied.Count} equipped item bonus(es) to player stats.");
        }
    }
}
