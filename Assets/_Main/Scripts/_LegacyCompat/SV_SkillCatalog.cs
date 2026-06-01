using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight skill metadata registry — just identity + icon + tier scaling preview.
/// Use this for UI rendering (level-up popup, equip screen) without requiring the
/// deep `ZSkillConfig` SO hierarchy (StatDefinition + AssetNumber + UpgradeConfig).
///
/// Fill `ZSkillConfig` reference when you author the proper upgrade SOs.
/// Until then, this catalog is the source of truth for "what skills exist".
/// </summary>
[CreateAssetMenu(fileName = "SV_SkillCatalog", menuName = "GoGo/Skill Catalog")]
public class SV_SkillCatalog : ScriptableObject
{
    public List<SV_SkillEntry> activeSkills = new List<SV_SkillEntry>();
    public List<SV_SkillEntry> passiveSkills = new List<SV_SkillEntry>();

    /// <summary>Resolve a SV_SkillEntry by its skillId. Handles the Z-prefix mismatch
    /// internally so callers don't need to know the convention.
    ///
    /// <para>Convention: <c>ZSkillConfig.name</c> is <c>"ZSk_Kunai"</c> / <c>"ZPs_HiPowerMagnet"</c>
    /// (with the 'Z' to distinguish the deep Luzart config SO from this lightweight catalog).
    /// Catalog <c>skillId</c> is <c>"Sk_Kunai"</c> / <c>"Ps_HiPowerMagnet"</c> (no Z, matching
    /// the original survivor.io naming). FindById is the single point that bridges them.</para>
    /// </summary>
    public SV_SkillEntry FindById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        // 1) Exact match first.
        foreach (var e in activeSkills) if (e != null && e.skillId == id) return e;
        foreach (var e in passiveSkills) if (e != null && e.skillId == id) return e;

        // 2) Retry with the Luzart 'Z' framework prefix stripped.
        if (id.Length > 1 && (id.StartsWith("ZSk_") || id.StartsWith("ZPs_")))
        {
            string stripped = id.Substring(1);
            foreach (var e in activeSkills) if (e != null && e.skillId == stripped) return e;
            foreach (var e in passiveSkills) if (e != null && e.skillId == stripped) return e;
        }

        return null;
    }
}

[System.Serializable]
public class SV_SkillEntry
{
    [Header("Identity")]
    public string skillId;
    public string displayName;
    public Sprite icon;

    [TextArea(2, 4)]
    public string description;

    [Header("Per-star scaling (from GDD)")]
    [Tooltip("ATK Multiplier at each star level (★1..★5).")]
    public float[] atkMultiplier = new float[5];

    [Tooltip("Scale / size multiplier (★1..★5). 1.0 = base size.")]
    public float[] scaleMultiplier = new float[5] { 1, 1, 1, 1, 1 };

    [Tooltip("Speed (projectile/animation) multiplier (★1..★5).")]
    public float[] speedMultiplier = new float[5] { 1, 1, 1, 1, 1 };

    [TextArea(1, 3)]
    [Tooltip("Per-star progression description (e.g., '+1 Kunai', '+damage').")]
    public string[] perStarDescription = new string[5];

    [Header("Passive scaling (passive only)")]
    [Tooltip("Buff value per star (★1..★5). E.g., 0.20 = +20%.")]
    public float[] passiveValue = new float[5];

    [Tooltip("Stat that the passive buffs.")]
    public PassiveStatType passiveStatType = PassiveStatType.None;

    [Header("Optional deep reference")]
    [Tooltip("Set this when you author the full ZSkillConfig SO. Catalog still works without.")]
    public Object zSkillConfigRef; // type Object to avoid hard NinjaUI dependency
}
