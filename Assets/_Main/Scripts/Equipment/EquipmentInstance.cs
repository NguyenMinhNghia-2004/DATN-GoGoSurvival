using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime instance của 1 equipment item trong inventory.
/// Mỗi item có unique instanceId, quality riêng, enhance level riêng.
/// Không phải MonoBehaviour — chỉ là data container.
/// </summary>
[System.Serializable]
public class EquipmentInstance
{
    [Tooltip("GUID unique cho mỗi instance")]
    public string instanceId;

    [Tooltip("Reference tới EquipmentData SO (định nghĩa base stats)")]
    public EquipmentData data;

    [Tooltip("Quality hiện tại (có thể tăng qua merge)")]
    public EquipQuality currentQuality;

    [Tooltip("Enhance level hiện tại (0 = chưa enhance)")]
    public int enhanceLevel;

    [Tooltip("Đang được trang bị hay không")]
    public bool isEquipped;

    // ---- Constructor ----

    public EquipmentInstance(EquipmentData equipData, EquipQuality quality = EquipQuality.Normal)
    {
        instanceId = System.Guid.NewGuid().ToString();
        data = equipData;
        currentQuality = quality;
        enhanceLevel = 0;
        isEquipped = false;
    }

    // ---- Stat Getters ----

    /// <summary>ATK tại quality + enhance level hiện tại.</summary>
    public float GetATK()
    {
        return data != null ? data.GetATKAtQuality(currentQuality, enhanceLevel) : 0f;
    }

    /// <summary>HP tại quality + enhance level hiện tại.</summary>
    public float GetHP()
    {
        return data != null ? data.GetHPAtQuality(currentQuality, enhanceLevel) : 0f;
    }

    /// <summary>Có thể dùng làm material merge không (chưa equipped).</summary>
    public bool CanMerge => !isEquipped;

    /// <summary>Có thể enhance thêm không.</summary>
    public bool CanEnhance => data != null && enhanceLevel < data.maxEnhanceLevel;

    /// <summary>Quality tiếp theo sau merge (nếu chưa max).</summary>
    public EquipQuality NextQuality
    {
        get
        {
            if (currentQuality >= EquipQuality.Relic)
                return EquipQuality.Relic;
            return currentQuality + 1;
        }
    }

    /// <summary>Lấy danh sách Grade Skills đã unlock.</summary>
    public GradeSkill[] GetUnlockedGradeSkills()
    {
        return data != null ? data.GetUnlockedGradeSkills(currentQuality) : new GradeSkill[0];
    }

    // ---- Enhance ----

    /// <summary>Tăng enhance level. Trả true nếu thành công.</summary>
    public bool TryEnhance()
    {
        if (!CanEnhance) return false;
        enhanceLevel++;
        return true;
    }

    // ---- Save Data ----

    /// <summary>Serialize thành save data.</summary>
    public EquipmentSaveEntry ToSaveEntry()
    {
        return new EquipmentSaveEntry
        {
            instanceId = instanceId,
            equipId = data != null ? data.equipId : "",
            quality = (int)currentQuality,
            enhanceLevel = enhanceLevel,
            isEquipped = isEquipped
        };
    }
}

/// <summary>
/// Serializable data cho save/load 1 equipment instance.
/// </summary>
[System.Serializable]
public struct EquipmentSaveEntry
{
    public string instanceId;
    public string equipId;
    public int quality;
    public int enhanceLevel;
    public bool isEquipped;
}

/// <summary>
/// Wrapper cho save/load toàn bộ inventory.
/// </summary>
[System.Serializable]
public class EquipmentSaveData
{
    public List<EquipmentSaveEntry> items = new List<EquipmentSaveEntry>();
}
