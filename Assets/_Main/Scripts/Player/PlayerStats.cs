using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton tổng hợp tất cả stat modifiers từ passive skills + equipment.
/// Các hệ thống khác (combat, movement, loot...) query stats qua đây.
/// Reset mỗi đầu trận chơi.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("=== Base Stats ===")]
    [Tooltip("ATK cơ bản (từ weapon equipment)")]
    public float BaseATK = 10f;

    [Tooltip("HP tối đa cơ bản")]
    public float BaseHP = 100f;

    [Tooltip("Tốc độ di chuyển cơ bản")]
    public float BaseSpeed = 5f;

    // ---- Passive Modifiers (from skills) ----
    private Dictionary<PassiveStatType, float> passiveModifiers = new Dictionary<PassiveStatType, float>();

    // ---- Equipment Bonuses ----
    private float equipAtkBonus;
    private float equipHPBonus;
    private float equipSpeedBonus;
    private float equipCDRBonus;

    // ---- Events ----
    public event Action OnStatsChanged;

    // ============================================================
    // Computed Final Stats
    // ============================================================

    /// <summary>ATK cuối cùng = (BaseATK + equipATK) * (1 + passive AttackRange bonus)</summary>
    public float FinalATK => (BaseATK + equipAtkBonus) * (1f + GetModifier(PassiveStatType.AttackRange));

    /// <summary>HP tối đa cuối cùng = (BaseHP + equipHP) * (1 + passive MaxHP bonus)</summary>
    public float FinalHP => (BaseHP + equipHPBonus) * (1f + GetModifier(PassiveStatType.MaxHP));

    /// <summary>Tốc độ cuối cùng = (BaseSpeed + equipSpeed) * (1 + passive MovementSpeed bonus)</summary>
    public float FinalSpeed => (BaseSpeed + equipSpeedBonus) * (1f + GetModifier(PassiveStatType.MovementSpeed));

    /// <summary>Cooldown multiplier = 1 - CDR (từ equipment + passive). Min 0.1 (giảm tối đa 90%).</summary>
    public float CooldownMultiplier => Mathf.Max(0.1f, 1f - equipCDRBonus - GetModifier(PassiveStatType.CooldownReduction));

    /// <summary>% giảm damage nhận được (từ Ronin Oyoroi)</summary>
    public float DamageReduction => Mathf.Clamp01(GetModifier(PassiveStatType.DamageReduction));

    /// <summary>Hệ số nhân gold loot (1 + passive bonus)</summary>
    public float GoldGainMultiplier => 1f + GetModifier(PassiveStatType.GoldGain);

    /// <summary>Hệ số nhân EXP (1 + passive bonus)</summary>
    public float EXPGainMultiplier => 1f + GetModifier(PassiveStatType.EXPGain);

    /// <summary>Hệ số nhân phạm vi loot item (1 + passive bonus)</summary>
    public float ItemLootRangeMultiplier => 1f + GetModifier(PassiveStatType.ItemLootRange);

    /// <summary>HP hồi mỗi giây (% MaxHP mỗi 5 giây → chuyển sang /giây)</summary>
    public float HPRegenPerSec => GetModifier(PassiveStatType.HPRegen) * FinalHP / 5f;

    /// <summary>Hệ số nhân tốc độ đạn (1 + passive bonus)</summary>
    public float BulletSpeedMultiplier => 1f + GetModifier(PassiveStatType.BulletSpeed);

    /// <summary>Hệ số nhân thời gian tồn tại skill (1 + passive bonus)</summary>
    public float SkillDurationMultiplier => 1f + GetModifier(PassiveStatType.SkillDuration);

    // ============================================================
    // Lifecycle
    // ============================================================

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ============================================================
    // Passive Modifier API (gọi bởi SkillManager)
    // ============================================================

    /// <summary>Cập nhật modifier cho 1 passive stat type. Ghi đè giá trị cũ.</summary>
    public void SetPassiveModifier(PassiveStatType type, float value)
    {
        if (type == PassiveStatType.None) return;
        passiveModifiers[type] = value;
        OnStatsChanged?.Invoke();
    }

    /// <summary>Xóa modifier cho 1 passive stat type.</summary>
    public void RemovePassiveModifier(PassiveStatType type)
    {
        if (passiveModifiers.Remove(type))
            OnStatsChanged?.Invoke();
    }

    /// <summary>Xóa tất cả passive modifiers (đầu trận mới).</summary>
    public void ClearAllPassiveModifiers()
    {
        passiveModifiers.Clear();
        OnStatsChanged?.Invoke();
    }

    /// <summary>Lấy giá trị modifier hiện tại cho 1 stat type. Trả 0 nếu chưa có.</summary>
    public float GetModifier(PassiveStatType type)
    {
        return passiveModifiers.TryGetValue(type, out float value) ? value : 0f;
    }

    // ============================================================
    // Equipment Bonus API (gọi bởi EquipmentManager)
    // ============================================================

    /// <summary>Áp dụng tổng bonus từ equipment. Gọi khi thay đổi equipment.</summary>
    public void ApplyEquipmentBonuses(float atkBonus, float hpBonus, float speedBonus, float cdrBonus)
    {
        equipAtkBonus = atkBonus;
        equipHPBonus = hpBonus;
        equipSpeedBonus = speedBonus;
        equipCDRBonus = Mathf.Clamp01(cdrBonus);
        OnStatsChanged?.Invoke();
    }

    /// <summary>Reset toàn bộ stats về mặc định (đầu trận mới).</summary>
    public void ResetForNewMatch()
    {
        ClearAllPassiveModifiers();
        // Equipment bonuses giữ nguyên (persistent giữa các trận)
    }
}
