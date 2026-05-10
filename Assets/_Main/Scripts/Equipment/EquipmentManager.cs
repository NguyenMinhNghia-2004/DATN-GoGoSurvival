using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton quản lý trang bị đang equipped của player (persistent giữa các màn).
/// - 6 slot: Weapon, Armor, Belt, Shoes, Necklace, Gloves
/// - Weapon slot quyết định starting Active skill
/// - Enhance system (tăng cấp trang bị bằng coins)
/// - Grade Skills evaluate từ quality
/// - Tính tổng stat bonus cho PlayerStats
/// - Save/Load qua DataManager (JSON)
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }

    // ---- Runtime State (6 equipped slots) ----
    private EquipmentInstance[] equippedSlots = new EquipmentInstance[6];
    // Index: 0=Weapon, 1=Armor, 2=Belt, 3=Shoes, 4=Necklace, 5=Gloves

    // ---- Events ----
    public event Action OnEquipmentChanged;

    // ---- Properties ----
    public EquipmentInstance Weapon => equippedSlots[(int)EquipSlot.Weapon];
    public EquipmentInstance Armor => equippedSlots[(int)EquipSlot.Armor];
    public EquipmentInstance Belt => equippedSlots[(int)EquipSlot.Belt];
    public EquipmentInstance Shoes => equippedSlots[(int)EquipSlot.Shoes];
    public EquipmentInstance Necklace => equippedSlots[(int)EquipSlot.Necklace];
    public EquipmentInstance Gloves => equippedSlots[(int)EquipSlot.Gloves];

    // ============================================================
    // Lifecycle
    // ============================================================

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadEquipment();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ============================================================
    // Weapon → Starting Skill
    // ============================================================

    /// <summary>Lấy starting Active skill từ weapon đang equipped.</summary>
    public SkillData GetStartingSkill()
    {
        var weapon = equippedSlots[(int)EquipSlot.Weapon];
        return weapon?.data?.linkedStartingSkill;
    }

    // ============================================================
    // Stat Calculations
    // ============================================================

    /// <summary>Tổng ATK bonus từ tất cả equipment.</summary>
    public float GetTotalAttackBonus()
    {
        float total = 0f;
        foreach (var slot in equippedSlots)
        {
            if (slot != null)
                total += slot.GetATK();
        }
        return total;
    }

    /// <summary>Tổng HP bonus từ tất cả equipment.</summary>
    public float GetTotalHPBonus()
    {
        float total = 0f;
        foreach (var slot in equippedSlots)
        {
            if (slot != null)
                total += slot.GetHP();
        }
        return total;
    }

    /// <summary>Tổng ATK bonus từ Grade Skills.</summary>
    public float GetGradeSkillAttackBonus()
    {
        float total = 0f;
        foreach (var slot in equippedSlots)
        {
            if (slot == null) continue;
            var gradeSkills = slot.GetUnlockedGradeSkills();
            foreach (var gs in gradeSkills)
            {
                if (gs.statType == PassiveStatType.AttackRange)
                    total += gs.value;
            }
        }
        return total;
    }

    /// <summary>Áp dụng tất cả equipment bonuses lên PlayerStats.</summary>
    public void ApplyToPlayerStats()
    {
        if (PlayerStats.Instance == null) return;

        float atkBonus = GetTotalAttackBonus();
        float hpBonus = GetTotalHPBonus();
        float speedBonus = 0f; // Shoes có thể thêm speed
        float cdrBonus = 0f;

        // Grade Skills cũng contribute thêm
        foreach (var slot in equippedSlots)
        {
            if (slot == null) continue;
            var gradeSkills = slot.GetUnlockedGradeSkills();
            foreach (var gs in gradeSkills)
            {
                switch (gs.statType)
                {
                    case PassiveStatType.MovementSpeed:
                        speedBonus += gs.value;
                        break;
                    case PassiveStatType.CooldownReduction:
                        cdrBonus += gs.value;
                        break;
                }
            }
        }

        PlayerStats.Instance.ApplyEquipmentBonuses(atkBonus, hpBonus, speedBonus, cdrBonus);
    }

    // ============================================================
    // Equip / Unequip
    // ============================================================

    /// <summary>Trang bị item vào slot tương ứng. Trả item cũ (nếu có).</summary>
    public EquipmentInstance Equip(EquipmentInstance item)
    {
        if (item == null || item.data == null) return null;

        int slotIndex = (int)item.data.slot;
        var previousItem = equippedSlots[slotIndex];

        // Unequip item cũ
        if (previousItem != null)
            previousItem.isEquipped = false;

        // Equip item mới
        item.isEquipped = true;
        equippedSlots[slotIndex] = item;

        SaveEquipment();
        ApplyToPlayerStats();
        OnEquipmentChanged?.Invoke();

        return previousItem;
    }

    /// <summary>Tháo trang bị tại slot.</summary>
    public EquipmentInstance Unequip(EquipSlot slot)
    {
        int slotIndex = (int)slot;
        var item = equippedSlots[slotIndex];
        if (item == null) return null;

        item.isEquipped = false;
        equippedSlots[slotIndex] = null;

        SaveEquipment();
        ApplyToPlayerStats();
        OnEquipmentChanged?.Invoke();

        return item;
    }

    /// <summary>Lấy item đang equipped tại slot.</summary>
    public EquipmentInstance GetEquipped(EquipSlot slot)
    {
        return equippedSlots[(int)slot];
    }

    // ============================================================
    // Enhance
    // ============================================================

    /// <summary>Nâng cấp trang bị tại slot. Trả true nếu thành công.</summary>
    public bool TryEnhance(EquipSlot slot)
    {
        var item = equippedSlots[(int)slot];
        if (item == null || !item.CanEnhance) return false;

        long coinCost = item.data.GetEnhanceCost(item.enhanceLevel);
        if (coinCost < 0) return false;

        // Kiểm tra đủ tài nguyên
        if (CurrencyManager.Instance == null) return false;
        if (CurrencyManager.Instance.Coins < coinCost) return false;

        // Trừ tài nguyên
        CurrencyManager.Instance.SpendCoins(coinCost);

        // Tăng level
        item.TryEnhance();

        SaveEquipment();
        ApplyToPlayerStats();
        OnEquipmentChanged?.Invoke();
        return true;
    }

    // ============================================================
    // Save / Load
    // ============================================================

    private void SaveEquipment()
    {
        var slotIds = new string[6];
        for (int i = 0; i < 6; i++)
        {
            slotIds[i] = equippedSlots[i]?.instanceId ?? "";
        }

        string json = JsonUtility.ToJson(new EquipSlotSaveData { slotInstanceIds = slotIds });

        if (DataManager.Instance != null)
        {
            DataManager.Instance.SetEquipmentSlots(json);
            DataManager.Instance.SaveData();
        }
        else
        {
            PlayerPrefs.SetString("equip_slots", json);
            PlayerPrefs.Save();
        }
    }

    private void LoadEquipment()
    {
        string json = "";
        if (DataManager.Instance != null)
            json = DataManager.Instance.GetEquipmentSlots();
        else
            json = PlayerPrefs.GetString("equip_slots", "");

        if (string.IsNullOrEmpty(json)) return;

        try
        {
            var saveData = JsonUtility.FromJson<EquipSlotSaveData>(json);
            if (saveData == null || saveData.slotInstanceIds == null) return;

            // Resolve instance IDs từ inventory
            if (EquipmentInventory.Instance == null) return;

            for (int i = 0; i < 6 && i < saveData.slotInstanceIds.Length; i++)
            {
                if (string.IsNullOrEmpty(saveData.slotInstanceIds[i])) continue;
                var item = EquipmentInventory.Instance.FindItem(saveData.slotInstanceIds[i]);
                if (item != null)
                {
                    item.isEquipped = true;
                    equippedSlots[i] = item;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[EquipmentManager] Failed to load equipment: {e.Message}");
        }
    }
}

/// <summary>Save data cho 6 equipped slots (chỉ lưu instanceId reference).</summary>
[System.Serializable]
public class EquipSlotSaveData
{
    public string[] slotInstanceIds = new string[6];
}
