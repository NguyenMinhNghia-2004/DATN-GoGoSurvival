using UnityEngine;

/// <summary>
/// ScriptableObject định nghĩa data cho 1 loại trang bị.
/// Tạo asset: Right-click > Create > GoGo > Equipment Data
/// Mỗi loại trang bị (VD: Kunai, Full Metal Suit) là 1 EquipmentData.
/// Quality có thể thay đổi runtime (qua merge).
/// </summary>
[CreateAssetMenu(fileName = "NewEquipment", menuName = "GoGo/Equipment Data")]
public class EquipmentData : ScriptableObject
{
    [Header("=== Basic Info ===")]
    public string equipId;
    public string equipName;
    public Sprite icon;

    [TextArea(2, 3)]
    public string description;

    [Header("=== Classification ===")]
    public EquipSlot slot;

    [Header("=== Base Stats theo Quality (index 0=Normal ... 6=Relic) ===")]
    [Tooltip("Base ATK tại mỗi quality tier")]
    public float[] atkByQuality = new float[7];

    [Tooltip("Base HP tại mỗi quality tier")]
    public float[] hpByQuality = new float[7];

    [Header("=== Enhance System ===")]
    public int maxEnhanceLevel = 10;

    [Tooltip("Mỗi level enhance tăng thêm bao nhiêu % stat")]
    public float enhanceBonusPerLevel = 0.05f;

    [Tooltip("Chi phí coins cho mỗi level enhance")]
    public long[] enhanceCostCoins = new long[10];

    [Header("=== Grade Skills (unlock tại quality milestones) ===")]
    [Tooltip("Grade Skill unlock tại Excellent quality")]
    public GradeSkill gradeSkillExcellent;

    [Tooltip("Grade Skill unlock tại Epic quality")]
    public GradeSkill gradeSkillEpic;

    [Tooltip("Grade Skill unlock tại Legendary quality")]
    public GradeSkill gradeSkillLegendary;

    [Header("=== Weapon → Starting Skill (chỉ slot Weapon) ===")]
    [Tooltip("Active skill bắt đầu trận khi equip weapon này")]
    public SkillData linkedStartingSkill;

    // ---- Helpers ----

    /// <summary>Lấy ATK base tại quality + enhance level.</summary>
    public float GetATKAtQuality(EquipQuality quality, int enhanceLevel = 0)
    {
        int idx = Mathf.Clamp((int)quality, 0, atkByQuality.Length - 1);
        return atkByQuality[idx] * (1f + enhanceBonusPerLevel * enhanceLevel);
    }

    /// <summary>Lấy HP base tại quality + enhance level.</summary>
    public float GetHPAtQuality(EquipQuality quality, int enhanceLevel = 0)
    {
        int idx = Mathf.Clamp((int)quality, 0, hpByQuality.Length - 1);
        return hpByQuality[idx] * (1f + enhanceBonusPerLevel * enhanceLevel);
    }

    /// <summary>Lấy chi phí coins cho enhance level tiếp theo.</summary>
    public long GetEnhanceCost(int currentLevel)
    {
        if (currentLevel >= maxEnhanceLevel) return -1;
        int idx = Mathf.Clamp(currentLevel, 0, enhanceCostCoins.Length - 1);
        return enhanceCostCoins[idx];
    }

    /// <summary>Lấy danh sách Grade Skills đã unlock tại quality hiện tại.</summary>
    public GradeSkill[] GetUnlockedGradeSkills(EquipQuality quality)
    {
        var list = new System.Collections.Generic.List<GradeSkill>();
        if (quality >= EquipQuality.Excellent && !string.IsNullOrEmpty(gradeSkillExcellent.skillName))
            list.Add(gradeSkillExcellent);
        if (quality >= EquipQuality.Epic && !string.IsNullOrEmpty(gradeSkillEpic.skillName))
            list.Add(gradeSkillEpic);
        if (quality >= EquipQuality.Legendary && !string.IsNullOrEmpty(gradeSkillLegendary.skillName))
            list.Add(gradeSkillLegendary);
        return list.ToArray();
    }
}

// ============================================================
// Enums & Structs
// ============================================================

/// <summary>6 equipment slots theo Survivorio.</summary>
public enum EquipSlot
{
    Weapon,     // ATK-focused, quyết định starting skill
    Armor,      // HP-focused
    Belt,       // HP-focused
    Shoes,      // HP + Speed
    Necklace,   // ATK-focused
    Gloves      // ATK-focused
}

/// <summary>
/// Quality tiers cho equipment. Merge 3 cùng tier → tier tiếp theo.
/// </summary>
public enum EquipQuality
{
    Normal,     // White  — index 0
    Good,       // Green  — index 1
    Better,     // Blue   — index 2
    Excellent,  // Purple — index 3 (unlock Grade Skill 1)
    Epic,       // Yellow — index 4 (unlock Grade Skill 2)
    Legendary,  // Red    — index 5 (unlock Grade Skill 3)
    Relic       // SS     — index 6
}

/// <summary>
/// Grade Skill — passive bonus unlock tại mốc quality nhất định.
/// Mỗi equipment có 3 grade skills (Excellent, Epic, Legendary).
/// </summary>
[System.Serializable]
public struct GradeSkill
{
    [Tooltip("Tên grade skill")]
    public string skillName;

    [TextArea(1, 2)]
    [Tooltip("Mô tả effect")]
    public string description;

    [Tooltip("Loại stat buff")]
    public PassiveStatType statType;

    [Tooltip("Giá trị buff (VD: 0.1 = +10% ATK, 0.3 = +30% Kunai damage)")]
    public float value;
}
