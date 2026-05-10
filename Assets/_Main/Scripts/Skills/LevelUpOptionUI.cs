using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Component cho mỗi card chọn skill (3 cards trong level-up panel).
/// Hiển thị: icon, name, description, star rating (level).
/// Matching layout hiện tại: Current Name, icon ở giữa, description, 5 stars bên dưới.
/// </summary>
public class LevelUpOptionUI : MonoBehaviour
{
    [Header("=== UI Elements ===")]
    [SerializeField] private Text nameText;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("=== Star Rating (5 stars) ===")]
    [SerializeField] private GameObject[] stars = new GameObject[5];

    [Header("=== Visual Feedback ===")]
    [SerializeField] private Image cardBackground;
    [SerializeField] private Color newSkillColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color upgradeColor = new Color(1f, 0.8f, 0.2f, 1f);

    [Header("=== Optional ===")]
    [SerializeField] private GameObject upgradeLabel;
    [SerializeField] private GameObject newLabel;

    /// <summary>
    /// Cập nhật card với thông tin skill.
    /// </summary>
    /// <param name="data">Data của skill</param>
    /// <param name="currentOwnedLevel">Level hiện tại nếu đã sở hữu (0 = chưa có)</param>
    public void Setup(SkillData data, int currentOwnedLevel)
    {
        if (data == null) return;

        bool isUpgrade = currentOwnedLevel > 0;
        int displayLevel = isUpgrade ? currentOwnedLevel + 1 : 1;

        // Name
        if (nameText != null)
            nameText.text = data.skillName;

        // Icon
        if (iconImage != null)
            iconImage.sprite = data.icon;

        // Description (theo level sẽ nhận)
        if (descriptionText != null)
            descriptionText.text = data.GetDescription(displayLevel);

        // Stars — hiển thị level skill sẽ đạt được
        UpdateStars(displayLevel);

        // Background color — phân biệt new vs upgrade
        if (cardBackground != null)
            cardBackground.color = isUpgrade ? upgradeColor : newSkillColor;

        // Labels
        if (upgradeLabel != null)
            upgradeLabel.SetActive(isUpgrade);
        if (newLabel != null)
            newLabel.SetActive(!isUpgrade);
    }

    /// <summary>
    /// Hiển thị số sao tương ứng với level.
    /// </summary>
    private void UpdateStars(int level)
    {
        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] != null)
                stars[i].SetActive(i < level);
        }
    }
}
