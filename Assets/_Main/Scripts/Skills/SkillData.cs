using UnityEngine;

/// <summary>
/// ScriptableObject định nghĩa data cho 1 skill.
/// Tạo asset: Right-click > Create > GoGo > Skill Data
/// Hỗ trợ 3 category: Active (có behavior), Passive (stat buff), EVO (evolved active).
/// Active/EVO có 5★ levels + EVO level. Passive có 5★ levels.
/// </summary>
[CreateAssetMenu(fileName = "NewSkill", menuName = "GoGo/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("=== Basic Info ===")]
    public string skillId;
    public string skillName;
    public Sprite icon;

    [TextArea(2, 4)]
    public string[] levelDescriptions = new string[6]; // 5★ + EVO

    [Header("=== Classification ===")]
    public SkillCategory category;

    [Header("=== Level Scaling (index 0=★1 ... index 4=★5, index 5=EVO) ===")]
    public int maxLevel = 5;

    [Tooltip("ATK Multiplier tại mỗi level — damage = BaseATK * atkMultiplier")]
    public float[] atkMultiplier = new float[6]; // 5★ + EVO

    [Tooltip("Cooldown (giây) giữa mỗi lần trigger")]
    public float[] cooldown = new float[5];

    [Tooltip("Thời gian tồn tại của skill effect (giây)")]
    public float[] duration = new float[5];

    [Tooltip("Bán kính / phạm vi ảnh hưởng")]
    public float[] radius = new float[5];

    [Tooltip("Số projectile / object tại mỗi level")]
    public int[] projectileCount = new int[5];

    [Header("=== Passive Buff (chỉ dùng cho category = Passive) ===")]
    [Tooltip("Loại stat mà passive này buff")]
    public PassiveStatType passiveStatType;

    [Tooltip("Giá trị buff tại mỗi level (VD: 0.2 = +20%, 0.1 = +10%)")]
    public float[] passiveValues = new float[5];

    [Header("=== Prefabs ===")]
    [Tooltip("Prefab chính của skill (VD: vùng lửa, spinner, drone...) — Active/EVO only")]
    public GameObject skillPrefab;

    [Tooltip("Prefab đạn/projectile nếu skill bắn đạn")]
    public GameObject projectilePrefab;

    [Header("=== Skill Behavior ===")]
    [Tooltip("Tên class behavior (VD: HomingArrowBehavior). Dùng để spawn đúng behavior khi activate.")]
    public string behaviorClassName;

    [Header("=== Evolution ===")]
    [Tooltip("Passive partner cần để evolution (Active cần Passive partner)")]
    public SkillData evolutionPartner;

    [Tooltip("Skill kết quả sau khi evolution")]
    public SkillData evolvedForm;

    // ---- Helpers ----

    /// <summary>Lấy mô tả theo level (1-based). Level 6 = EVO.</summary>
    public string GetDescription(int level)
    {
        int idx = Mathf.Clamp(level - 1, 0, levelDescriptions.Length - 1);
        return levelDescriptions[idx];
    }

    /// <summary>Lấy ATK Multiplier theo level (1-based). Level 6 = EVO.</summary>
    public float GetAtkMultiplier(int level)
    {
        int idx = Mathf.Clamp(level - 1, 0, atkMultiplier.Length - 1);
        return atkMultiplier[idx];
    }

    /// <summary>Lấy cooldown theo level (1-based)</summary>
    public float GetCooldown(int level)
    {
        int idx = Mathf.Clamp(level - 1, 0, cooldown.Length - 1);
        return cooldown[idx];
    }

    /// <summary>Lấy duration theo level (1-based)</summary>
    public float GetDuration(int level)
    {
        int idx = Mathf.Clamp(level - 1, 0, duration.Length - 1);
        return duration[idx];
    }

    /// <summary>Lấy radius theo level (1-based)</summary>
    public float GetRadius(int level)
    {
        int idx = Mathf.Clamp(level - 1, 0, radius.Length - 1);
        return radius[idx];
    }

    /// <summary>Lấy projectile count theo level (1-based)</summary>
    public int GetProjectileCount(int level)
    {
        int idx = Mathf.Clamp(level - 1, 0, projectileCount.Length - 1);
        return projectileCount[idx];
    }

    /// <summary>Lấy passive value theo level (1-based)</summary>
    public float GetPassiveValue(int level)
    {
        int idx = Mathf.Clamp(level - 1, 0, passiveValues.Length - 1);
        return passiveValues[idx];
    }

    /// <summary>Kiểm tra skill này có thể evolution không</summary>
    public bool HasEvolution => evolvedForm != null && evolutionPartner != null;
}
