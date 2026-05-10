using UnityEngine;

/// <summary>
/// Runtime state của 1 skill đang active trong gameplay.
/// Không phải MonoBehaviour — chỉ là data container.
/// Hỗ trợ Active (behavior), Passive (stat buff), và EVO skills.
/// </summary>
[System.Serializable]
public class SkillInstance
{
    public SkillData data;
    public int currentLevel = 1;

    public SkillInstance(SkillData skillData)
    {
        data = skillData;
        currentLevel = 1;
    }

    // ---- Stat Getters (delegate tới SkillData) ----

    public float AtkMultiplier => data.GetAtkMultiplier(currentLevel);
    public float Cooldown => data.GetCooldown(currentLevel);
    public float Duration => data.GetDuration(currentLevel);
    public float Radius => data.GetRadius(currentLevel);
    public int ProjectileCount => data.GetProjectileCount(currentLevel);
    public string Description => data.GetDescription(currentLevel);
    public float PassiveValue => data.GetPassiveValue(currentLevel);

    // ---- Category Helpers ----

    public bool IsActive => data.category == SkillCategory.Active;
    public bool IsPassive => data.category == SkillCategory.Passive;
    public bool IsEVO => data.category == SkillCategory.EVO;

    // ---- Level Management ----

    public bool CanLevelUp => currentLevel < data.maxLevel;
    public bool IsMaxLevel => currentLevel >= data.maxLevel;

    public void LevelUp()
    {
        if (CanLevelUp)
            currentLevel++;
    }
}
