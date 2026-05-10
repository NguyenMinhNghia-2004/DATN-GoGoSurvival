using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Component cho mỗi ô nhỏ hiển thị skill đã sở hữu.
/// Gắn trên mỗi slot UI (6 attack bên trái + 6 support bên phải).
/// Hiển thị icon skill + level indicator.
/// </summary>
public class SkillSlotUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject emptyIndicator;
    [SerializeField] private GameObject levelIndicator;
    [SerializeField] private Text levelText;

    [Header("Visual")]
    [SerializeField] private Color emptyColor = new Color(1, 1, 1, 0.2f);
    [SerializeField] private Color activeColor = Color.white;

    private SkillInstance currentSkill;

    /// <summary>
    /// Hiển thị skill trong slot này.
    /// </summary>
    public void SetSkill(SkillInstance skill)
    {
        currentSkill = skill;

        if (iconImage != null)
        {
            iconImage.sprite = skill.data.icon;
            iconImage.color = activeColor;
        }

        if (emptyIndicator != null)
            emptyIndicator.SetActive(false);

        if (levelIndicator != null)
            levelIndicator.SetActive(true);

        if (levelText != null)
            levelText.text = skill.currentLevel.ToString();
    }

    /// <summary>
    /// Xóa slot (chưa có skill).
    /// </summary>
    public void ClearSlot()
    {
        currentSkill = null;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.color = emptyColor;
        }

        if (emptyIndicator != null)
            emptyIndicator.SetActive(true);

        if (levelIndicator != null)
            levelIndicator.SetActive(false);
    }
}
