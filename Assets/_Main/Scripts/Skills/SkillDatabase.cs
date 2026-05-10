using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject chứa registry tập trung tất cả skills trong game.
/// Tạo asset: Right-click > Create > GoGo > Skill Database
/// Chỉ cần 1 instance duy nhất.
/// </summary>
[CreateAssetMenu(fileName = "SkillDatabase", menuName = "GoGo/Skill Database")]
public class SkillDatabase : ScriptableObject
{
    [Header("Tất cả skills trong game (Active + Passive + EVO)")]
    public SkillData[] allSkills;

    /// <summary>
    /// Lấy danh sách skill theo category (Active, Passive, EVO).
    /// </summary>
    public List<SkillData> GetSkillsByCategory(SkillCategory category)
    {
        var result = new List<SkillData>();
        foreach (var skill in allSkills)
        {
            if (skill != null && skill.category == category)
                result.Add(skill);
        }
        return result;
    }

    /// <summary>
    /// Random N skill không trùng nhau cho level-up panel.
    /// CHỈ lấy Active + Passive (EVO không bao giờ xuất hiện trong pool random).
    /// Ưu tiên: skill đã sở hữu nhưng chưa max level (để upgrade).
    /// </summary>
    public SkillData[] GetRandomSkills(int count, List<SkillData> ownedSkills,
                                        Dictionary<string, int> ownedLevels)
    {
        var result = new List<SkillData>();
        var upgradeable = new List<SkillData>();
        var newSkills = new List<SkillData>();

        foreach (var skill in allSkills)
        {
            if (skill == null) continue;

            // EVO skills KHÔNG bao giờ xuất hiện trong pool random
            if (skill.category == SkillCategory.EVO) continue;

            if (ownedSkills.Contains(skill))
            {
                // Đã sở hữu → kiểm tra còn upgrade được không
                if (ownedLevels.ContainsKey(skill.skillId) &&
                    ownedLevels[skill.skillId] < skill.maxLevel)
                {
                    upgradeable.Add(skill);
                }
                // Nếu đã max level → không cho vào pool nữa
            }
            else
            {
                newSkills.Add(skill);
            }
        }

        // Shuffle cả 2 pool
        ShuffleList(upgradeable);
        ShuffleList(newSkills);

        // Ưu tiên upgradeable trước, rồi mới new skills
        foreach (var skill in upgradeable)
        {
            if (result.Count >= count) break;
            result.Add(skill);
        }
        foreach (var skill in newSkills)
        {
            if (result.Count >= count) break;
            result.Add(skill);
        }

        // Shuffle kết quả cuối để vị trí random
        ShuffleList(result);
        return result.ToArray();
    }

    /// <summary>
    /// Tìm skill theo ID.
    /// </summary>
    public SkillData FindById(string skillId)
    {
        foreach (var skill in allSkills)
        {
            if (skill != null && skill.skillId == skillId)
                return skill;
        }
        return null;
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
