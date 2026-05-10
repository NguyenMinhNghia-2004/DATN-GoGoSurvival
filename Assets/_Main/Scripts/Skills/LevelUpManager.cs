using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quản lý panel level-up khi player lên cấp.
///
/// Chức năng:
/// - Random 3 skill Active/Passive (không trùng, ưu tiên upgrade, EVO không xuất hiện)
/// - Hiển thị thông tin lên 3 cards (name, icon, desc, stars)
/// - Cập nhật 12 skill slots (6 active + 6 passive)
/// - Pause/resume game khi chọn
/// </summary>
public class LevelUpManager : MonoBehaviour
{
    [Header("=== References ===")]
    [SerializeField] private SkillManager skillManager;

    [Header("=== Level Up Panel ===")]
    [SerializeField] private GameObject levelUpPanel;

    [Header("=== 3 Option Cards ===")]
    [SerializeField] private LevelUpOptionUI[] optionCards = new LevelUpOptionUI[3];

    [Header("=== Skill Slots (6 Active trái + 6 Passive phải) ===")]
    [SerializeField] private SkillSlotUI[] activeSlots = new SkillSlotUI[6];
    [SerializeField] private SkillSlotUI[] passiveSlots = new SkillSlotUI[6];

    [Header("=== Continue Buttons (assign cùng card) ===")]
    [SerializeField] private Button btnOption1;
    [SerializeField] private Button btnOption2;
    [SerializeField] private Button btnOption3;

    // ---- State ----
    private SkillData[] currentChoices;
    private bool isShowing;

    // ============================================================
    // Lifecycle
    // ============================================================

    private void Start()
    {
        // Gắn button listeners
        if (btnOption1 != null) btnOption1.onClick.AddListener(() => OnOptionSelected(0));
        if (btnOption2 != null) btnOption2.onClick.AddListener(() => OnOptionSelected(1));
        if (btnOption3 != null) btnOption3.onClick.AddListener(() => OnOptionSelected(2));

        // Subscribe skill events để cập nhật slots
        if (skillManager != null)
        {
            skillManager.OnSkillAcquired += _ => RefreshSlots();
            skillManager.OnSkillUpgraded += _ => RefreshSlots();
            skillManager.OnSkillEvolved += (_, __) => RefreshSlots();
        }

        // Ẩn panel ban đầu
        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);
    }

    // ============================================================
    // Public API
    // ============================================================

    /// <summary>
    /// Hiển thị panel chọn skill. Gọi khi thanh EXP đầy.
    /// </summary>
    public void ShowLevelUp()
    {
        if (isShowing || skillManager == null) return;
        isShowing = true;

        // Random 3 skill (Active + Passive only, no EVO)
        var ownedDatas = skillManager.GetOwnedSkillDatas();
        var ownedLevels = skillManager.GetOwnedLevels();
        currentChoices = skillManager.Database.GetRandomSkills(3, ownedDatas, ownedLevels);

        // Cập nhật UI cho từng card
        for (int i = 0; i < optionCards.Length; i++)
        {
            if (i < currentChoices.Length && optionCards[i] != null)
            {
                int currentLv = 0;
                if (ownedLevels.ContainsKey(currentChoices[i].skillId))
                    currentLv = ownedLevels[currentChoices[i].skillId];

                optionCards[i].Setup(currentChoices[i], currentLv);
                optionCards[i].gameObject.SetActive(true);
            }
            else if (optionCards[i] != null)
            {
                optionCards[i].gameObject.SetActive(false);
            }
        }

        levelUpPanel.SetActive(true);
        Time.timeScale = 0f; // Pause game
    }

    /// <summary>
    /// Player chọn 1 trong 3 options.
    /// </summary>
    public void OnOptionSelected(int index)
    {
        if (!isShowing || currentChoices == null) return;
        if (index < 0 || index >= currentChoices.Length) return;

        // Acquire/upgrade skill
        skillManager.AcquireSkill(currentChoices[index]);

        // Ẩn panel, resume game
        levelUpPanel.SetActive(false);
        Time.timeScale = 1f;
        isShowing = false;
        currentChoices = null;
    }

    /// <summary>
    /// Cập nhật 12 skill slots hiển thị.
    /// Active slots (trái) + Passive slots (phải).
    /// EVO skills hiển thị ở Active slots (thay thế Active đã evolve).
    /// </summary>
    public void RefreshSlots()
    {
        // Active + EVO slots (trái)
        var activeSkills = skillManager.GetSkillsByCategory(SkillCategory.Active);
        var evoSkills = skillManager.GetSkillsByCategory(SkillCategory.EVO);
        var combinedActive = new List<SkillInstance>();
        combinedActive.AddRange(activeSkills);
        combinedActive.AddRange(evoSkills);

        for (int i = 0; i < activeSlots.Length; i++)
        {
            if (activeSlots[i] == null) continue;
            if (i < combinedActive.Count)
                activeSlots[i].SetSkill(combinedActive[i]);
            else
                activeSlots[i].ClearSlot();
        }

        // Passive slots (phải)
        var passiveSkills = skillManager.GetSkillsByCategory(SkillCategory.Passive);
        for (int i = 0; i < passiveSlots.Length; i++)
        {
            if (passiveSlots[i] == null) continue;
            if (i < passiveSkills.Count)
                passiveSlots[i].SetSkill(passiveSkills[i]);
            else
                passiveSlots[i].ClearSlot();
        }
    }

    /// <summary>
    /// Reset UI khi bắt đầu màn mới.
    /// </summary>
    public void ResetUI()
    {
        isShowing = false;
        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);
        Time.timeScale = 1f;

        foreach (var slot in activeSlots)
            if (slot != null) slot.ClearSlot();
        foreach (var slot in passiveSlots)
            if (slot != null) slot.ClearSlot();
    }
}
