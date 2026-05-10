using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton quản lý tất cả skills đang active trong 1 màn chơi.
/// - Thêm skill mới hoặc upgrade skill đã có
/// - Active skills: spawn behavior prefab
/// - Passive skills: apply stat buff lên PlayerStats
/// - Evolution: Active max level + có Passive partner → EVO (Passive KHÔNG bị consume)
/// - Kích hoạt/hủy skill behaviors
/// - Reset khi bắt đầu màn mới
/// </summary>
public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }

    [Header("Database")]
    [SerializeField] private SkillDatabase database;

    [Header("Skill Containers (assign trong Inspector)")]
    [Tooltip("Parent object chứa các skill behavior instances")]
    [SerializeField] private Transform skillBehaviorParent;

    // ---- Runtime State ----
    private List<SkillInstance> activeSkills = new List<SkillInstance>();
    private Dictionary<string, GameObject> activeBehaviors = new Dictionary<string, GameObject>();

    // ---- Public Properties ----
    public IReadOnlyList<SkillInstance> ActiveSkills => activeSkills;
    public SkillDatabase Database => database;

    // ---- Events ----
    public event Action<SkillInstance> OnSkillAcquired;
    public event Action<SkillInstance> OnSkillUpgraded;
    public event Action<SkillInstance, SkillInstance> OnSkillEvolved; // (oldActive, newEvolved)

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
    // Core API
    // ============================================================

    /// <summary>
    /// Thêm skill mới hoặc upgrade nếu đã sở hữu.
    /// Gọi bởi LevelUpManager khi player chọn skill.
    /// Active → spawn behavior. Passive → apply stat buff.
    /// </summary>
    public void AcquireSkill(SkillData data)
    {
        if (data == null) return;

        var existing = FindSkill(data.skillId);

        if (existing != null)
        {
            // Đã sở hữu → upgrade
            if (existing.CanLevelUp)
            {
                existing.LevelUp();

                if (existing.IsPassive)
                {
                    // Cập nhật passive modifier
                    ApplyPassiveStats(existing);
                }
                else
                {
                    // Cập nhật behavior
                    UpdateBehavior(existing);
                }

                OnSkillUpgraded?.Invoke(existing);

                // Sau khi upgrade, check evolution cho Active skills
                if (existing.IsActive && existing.IsMaxLevel)
                    TryEvolve(existing);
            }
        }
        else
        {
            // Skill mới
            var instance = new SkillInstance(data);
            activeSkills.Add(instance);

            if (instance.IsPassive)
            {
                // Passive: chỉ apply stat buff, không cần behavior
                ApplyPassiveStats(instance);
            }
            else
            {
                // Active / EVO: spawn behavior prefab
                ActivateBehavior(instance);
            }

            OnSkillAcquired?.Invoke(instance);

            // Khi acquire passive mới, check xem có Active nào đã max level
            // mà chưa evolve và cần passive này không
            if (instance.IsPassive)
                CheckPendingEvolutions();
        }
    }

    /// <summary>
    /// Init starting skill từ equipped weapon. Gọi đầu trận.
    /// </summary>
    public void InitStartingSkill()
    {
        if (EquipmentManager.Instance == null) return;
        var startingSkill = EquipmentManager.Instance.GetStartingSkill();
        if (startingSkill != null)
            AcquireSkill(startingSkill);
    }

    /// <summary>
    /// Reset toàn bộ skills khi bắt đầu màn chơi mới.
    /// </summary>
    public void ResetAllSkills()
    {
        // Destroy tất cả behavior GameObjects
        foreach (var kvp in activeBehaviors)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value);
        }
        activeBehaviors.Clear();
        activeSkills.Clear();

        // Reset passive modifiers
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.ResetForNewMatch();
    }

    /// <summary>
    /// Lấy danh sách SkillData mà player đang sở hữu.
    /// </summary>
    public List<SkillData> GetOwnedSkillDatas()
    {
        var result = new List<SkillData>();
        foreach (var si in activeSkills)
            result.Add(si.data);
        return result;
    }

    /// <summary>
    /// Lấy dictionary skillId -> currentLevel cho owned skills.
    /// </summary>
    public Dictionary<string, int> GetOwnedLevels()
    {
        var dict = new Dictionary<string, int>();
        foreach (var si in activeSkills)
            dict[si.data.skillId] = si.currentLevel;
        return dict;
    }

    /// <summary>
    /// Lấy danh sách skill theo category.
    /// </summary>
    public List<SkillInstance> GetSkillsByCategory(SkillCategory category)
    {
        var result = new List<SkillInstance>();
        foreach (var si in activeSkills)
        {
            if (si.data.category == category)
                result.Add(si);
        }
        return result;
    }

    /// <summary>
    /// Tìm SkillInstance theo skillId, trả null nếu chưa sở hữu.
    /// </summary>
    public SkillInstance FindSkill(string skillId)
    {
        foreach (var si in activeSkills)
        {
            if (si.data.skillId == skillId)
                return si;
        }
        return null;
    }

    // ============================================================
    // Passive Stats
    // ============================================================

    /// <summary>
    /// Áp dụng passive stat buff lên PlayerStats.
    /// </summary>
    private void ApplyPassiveStats(SkillInstance passiveSkill)
    {
        if (PlayerStats.Instance == null) return;
        if (passiveSkill.data.passiveStatType == PassiveStatType.None) return;

        float value = passiveSkill.PassiveValue;
        PlayerStats.Instance.SetPassiveModifier(passiveSkill.data.passiveStatType, value);
    }

    // ============================================================
    // Behavior Management
    // ============================================================

    private void ActivateBehavior(SkillInstance skill)
    {
        if (skill.data.skillPrefab == null) return;

        // Instantiate prefab chứa behavior script
        var parent = skillBehaviorParent != null ? skillBehaviorParent : transform;
        var go = Instantiate(skill.data.skillPrefab, parent);
        go.name = $"Skill_{skill.data.skillId}";

        // Tìm và init behavior component
        var behavior = go.GetComponent<SkillBehaviorBase>();
        if (behavior != null)
        {
            behavior.Initialize(skill);
        }

        activeBehaviors[skill.data.skillId] = go;
    }

    private void UpdateBehavior(SkillInstance skill)
    {
        if (activeBehaviors.TryGetValue(skill.data.skillId, out var go) && go != null)
        {
            var behavior = go.GetComponent<SkillBehaviorBase>();
            if (behavior != null)
            {
                behavior.OnLevelChanged(skill.currentLevel);
            }
        }
    }

    private void DeactivateBehavior(string skillId)
    {
        if (activeBehaviors.TryGetValue(skillId, out var go) && go != null)
        {
            Destroy(go);
        }
        activeBehaviors.Remove(skillId);
    }

    // ============================================================
    // Evolution
    // ============================================================

    /// <summary>
    /// Kiểm tra evolution khi Active skill max level.
    /// Điều kiện: Active max level + Passive partner đã acquire (bất kỳ level).
    /// Passive KHÔNG bị consume — vẫn giữ buff.
    /// HE Fuel có thể enable cả Durian (Caltrops) VÀ RPG (Sharkmaw Gun).
    /// </summary>
    private void TryEvolve(SkillInstance maxedActiveSkill)
    {
        if (!maxedActiveSkill.data.HasEvolution) return;
        if (maxedActiveSkill.data.category != SkillCategory.Active) return;

        // Chỉ cần passive partner đã được acquire (bất kỳ level)
        var partner = FindSkill(maxedActiveSkill.data.evolutionPartner.skillId);
        if (partner == null) return;

        var evolvedData = maxedActiveSkill.data.evolvedForm;

        // Remove Active skill cũ (KHÔNG remove Passive partner)
        DeactivateBehavior(maxedActiveSkill.data.skillId);
        activeSkills.Remove(maxedActiveSkill);

        // Thêm EVO skill (bắt đầu từ level 1)
        var evolvedInstance = new SkillInstance(evolvedData);
        activeSkills.Add(evolvedInstance);
        ActivateBehavior(evolvedInstance);

        OnSkillEvolved?.Invoke(maxedActiveSkill, evolvedInstance);
    }

    /// <summary>
    /// Sau khi acquire passive mới, check tất cả Active đã max level
    /// xem có cái nào cần passive này để evolve.
    /// </summary>
    private void CheckPendingEvolutions()
    {
        // Copy list để avoid modification during iteration
        var snapshot = new List<SkillInstance>(activeSkills);
        foreach (var skill in snapshot)
        {
            if (skill.IsActive && skill.IsMaxLevel && skill.data.HasEvolution)
            {
                TryEvolve(skill);
            }
        }
    }
}
