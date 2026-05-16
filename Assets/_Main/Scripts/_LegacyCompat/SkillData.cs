using UnityEngine;

// LEGACY STUB - kept for compile compat with Equipment.linkedStartingSkill field.
// Will be replaced in M5 when Equipment migrates to reference ZSkillConfig directly.
[CreateAssetMenu(fileName = "LegacySkillStub", menuName = "GoGo/Legacy/Skill Data (STUB)")]
public class SkillData : ScriptableObject
{
    [Tooltip("Stub placeholder. Migrate to ZSkillConfig in M5.")]
    public string skillId;
}
