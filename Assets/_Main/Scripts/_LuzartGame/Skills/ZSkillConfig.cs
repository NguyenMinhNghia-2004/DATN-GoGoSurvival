using System;
using System.Collections.Generic;
using UnityEngine;
namespace Luzart
{
    [System.Serializable]
    public class ZSkillInformation
    {
        [SerializeField] private Sprite sprite;
        [SerializeField] private string skillName;
        [SerializeField] private string skillDetail;
        public Sprite Sprite => sprite;
        public string SkillDetail => skillDetail;
        public string SkillName => skillName;
#if UNITY_EDITOR
        public Sprite Editor_Sprite
        {
            get => sprite;
            set => sprite = value;
        }
        public string Editor_SkillDetail
        {
            get => skillDetail;
            set => skillDetail = value;
        }
        public string Editor_SkillName
        {
            get => skillName;
            set => skillName = value;
        }
#endif
    }
    // Class này không được kế thừa, nó là 1 cái base cho config kỹ năng để đa dụng cho tất cả
    public sealed class ZSkillConfig : AbstractScriptableContent
    {
        [SerializeField] private ETypeSkill eTypeSkill;
        [SerializeField] private SkillDefine skillDefine;
        // Các config nâng cấp của từng level kỹ năng
        [SerializeField] private List<ZSkillUpgradeConfig> upgradeConfigs;
        // Các Behavior của từng skill, nếu như skill được nâng cấp thì chỉ thay chỉ số trong behavior thôi. Nếu như đổi Behavior thì có thể tính sau
        [SerializeField] private List<ZSkillBehaviorConfig> behaviorConfigs;
        // Có thể nâng cấp kỹ năng hay không
        [SerializeField] private BaseAssetBool isCanUpgradeLevel;
        public ETypeSkill ETypeSkill => eTypeSkill;
        public SkillDefine SkillDefine => skillDefine;
        public IReadOnlyList<ZSkillUpgradeConfig> UpgradeConfigs => upgradeConfigs;
        public IReadOnlyList<ZSkillBehaviorConfig> BehaviorConfigs => behaviorConfigs;
        public IBool IsCanUpgradeLevel => isCanUpgradeLevel == null ? new BoolValue(true) : isCanUpgradeLevel;
        /// <summary>Create a runtime skill instance bound to <paramref name="entity"/>.
        /// ZSkill is a MonoBehaviour now, so we spawn a wrapper GameObject + AddComponent.
        /// Used by SkillControllerBehavior (legacy enemy skill path). For the player, use
        /// LuzartPlayerEntityRoot.AddSkill / SpawnSkill which create the GO under Player/Skills/.</summary>
        public IZSkill CreateSkill(IEntity entity)
        {
            var go = new GameObject($"ZSkill_{name}");
            var z = go.AddComponent<ZSkill>();
            z.Bind(entity, this);
            return z;
        }
    }
    public interface IZSkillBehavior : IBehavior, IDisposable
    {
    }
    public enum ETypeSkill
    {
        Stat = 0,
        Active = 1,
        Deactive = 2,
    }
}