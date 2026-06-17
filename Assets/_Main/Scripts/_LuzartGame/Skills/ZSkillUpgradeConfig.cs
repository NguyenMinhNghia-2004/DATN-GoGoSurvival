using Luzart;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
namespace Luzart
{
    // Chỗ này chỉ config Stat khi nâng cấp kỹ năng : Vdu : MultiplyATK, ProjectileSpeed,...
    public sealed class ZSkillUpgradeConfig : AbstractScriptableContent
    {
        [SerializeField] ZSkillInformation skillInfor;
        [SerializeField] List<Stat> stats;
        [SerializeField] List<SerializedModifierPair> modifierFactors;
        [System.NonSerialized] private bool _initialized;
        private List<IStat> _stats;
        private Dictionary<StatType, IStat> _statDict;
        public ZSkillInformation SkillInfor => skillInfor;
        public IReadOnlyList<IStat> Stats => _stats;
        protected override void DoInitialize()
        {
            base.DoInitialize();
            _initialized = false;
            TryInit();
        }
        private void TryInit()
        {
            if (_initialized) return;
            _initialized = true;
            _statDict = new();
            _stats = new();
            for (int i = 0; i < stats.Count; i++)
            {
                var stat = stats[i];
                _statDict[stat.Definition.StatType] = stat;
            }
        }
        public double GetStatCalculator(StatType statType)
        {
            TryInit();  // lazy-init if Domain.Initialize never ran (e.g. when SO is loaded directly via guid ref)
            if (_statDict.TryGetValue(statType, out var stat))
            {
                return stat.Value.Value;
            }
            return 0;
        }
        public INumber GetStat(StatType statType)
        {
            TryInit();
            if (_statDict.TryGetValue(statType, out var stat))
            {
                return stat.Value;
            }
            return new Number(1);
        }
        public void InitUpgrade()
        {
            for (int i = 0; i < modifierFactors.Count; i++)
            {
                var pair = modifierFactors[i];
                var factor = pair.Factor;
                var modifier = pair.Modifier;
                modifier?.AddFactor(factor);
            }
        }
        public void TerminalUpgrade()
        {
            for (int i = 0; i < modifierFactors.Count; i++)
            {
                var pair = modifierFactors[i];
                var factor = pair.Factor;
                var modifier = pair.Modifier;
                modifier?.RemoveFactor(factor);
            }
        }
        public string GetUpgradeDetail()
        {
            StringBuilder sb = new StringBuilder();
            // Stats
            for (int i = 0; i < stats.Count; i++)
            {
                var stat = stats[i];
                string label = stat.Definition.DisplayName.Value;
                double value = stat.Value.Value;
                sb.AppendLine(FormatSafe(label, value));
            }
            sb.AppendLine();
            sb.AppendLine("Modifiers:");
            // Modifiers
            for (int i = 0; i < modifierFactors.Count; i++)
            {
                var modifier = modifierFactors[i];
                string label = modifier.Factor.Definition.DisplayName.Value;
                double value = modifier.Factor.Value.Value;
                sb.AppendLine(FormatSafe(label, value));
            }
            return sb.ToString();
        }
        private string FormatSafe(string template, object value)
        {
            if (string.IsNullOrEmpty(template))
                return value?.ToString() ?? "";
            if (template.Contains("{0}"))
                return string.Format(template, value);
            return $"{template} : {value}";
        }
#if UNITY_EDITOR
        protected override void Editor_DoOnValidateScriptableObject()
        {
            base.Editor_DoOnValidateScriptableObject();
            AutoRenameSkillInfor();
            void AutoRenameSkillInfor()
            {
                string detail = name;
                string path = UnityEditor.AssetDatabase.GetAssetPath(this);
                var main = UnityEditor.AssetDatabase.LoadMainAssetAtPath(path);
                string nameSkill = main.name;
                string str = nameSkill.Replace("ZSkill_", "");
                str = str.Replace("ZSkillStatAdd", "Tối ưu ");
                str = str.Replace("ZSkill", "");
                str = str.Replace("Config", "");
                str = str.Replace("ATK", "sát thương");
                str = str.Replace("Cooldown", "hồi chiêu");
                str = str.Replace("FireSpeed", "tốc độ bắn");
                str = str.Replace("Heal", "tốc độ hồi máu");
                str = str.Replace("Speed", "tốc độ di chuyển");
                str = str.Replace("Runtime_Gold", "vàng trong game");
                str = str.Replace("Gold", "vàng trong game");
                str = str.Replace("HPMax", "HP tổng");
                str = str.Replace("Runtime_HP", "HP");
                str = str.Replace("Runtime_XP", "XP");
                str = str.Replace("Stat", "Tăng ");
                skillInfor.Editor_SkillName = str;
                //skillInfor.Editor_SkillDetail = detail.Replace("|ZSkillUpgradeConfig_", "");
            }
        }
#endif
    }
    public interface IZSkillStat : IStat
    {
        ICalculator Calculator { get; }
    }
}
//private void AutoFindStatDefinition()
//{
//    if (stats == null) return;
//    string path = UnityEditor.AssetDatabase.GetAssetPath(this);
//    var main = UnityEditor.AssetDatabase.LoadMainAssetAtPath(path);
//    string nameSkill = main.name;
//    nameSkill = nameSkill.Replace("ZSkill", "");
//    nameSkill = nameSkill.Replace("Config", "");
//    for (int i = 0; i < stats.Count; i++)
//    {
//        var stat = stats[i];
//        if (stat == null) continue;
//        if (stat.Definition != null)
//        {
//            char c = name[^1];
//            int x = c - '0';
//            string definition = stat.Editor_Definition.name;
//            definition = definition.Replace("StatDefinition_", "");
//            var assetNumber = FindItemEditor.GetAssetNumberForStatEditor(nameSkill, definition, x);
//            if (assetNumber != null)
//            {
//                stat.Editor_Value = new NumberPicker(NumberMode.AssetNumber, 0, assetNumber);
//            }
//        }
//    }
//}