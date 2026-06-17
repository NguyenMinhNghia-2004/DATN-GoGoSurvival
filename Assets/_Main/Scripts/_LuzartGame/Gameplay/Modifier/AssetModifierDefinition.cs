using UnityEngine;
namespace Luzart
{
    public class AssetModifierDefinition : AbstractScriptableContent, IModifierDefinition
    {
        [SerializeField]
        private string displayName;
        IString _iString = null;
        IString IModifierDefinition.DisplayName => TryGetString();
        string IModifierDefinition.FormatValue(double value)
        {
            return DoFormatValue(value);
        }
        private IString TryGetString()
        {
            if (_iString == null)
            {
                _iString = new StringValue(displayName);
            }
            return _iString;
        }
        protected virtual string DoFormatValue(double value)
        {
            return $"{((IModifierDefinition)this).DisplayName}: {value}";
        }
#if UNITY_EDITOR
        protected override void Editor_DoOnValidateScriptableObject()
        {
            base.Editor_DoOnValidateScriptableObject();
            string str = name.Replace("ModDef_Player_", "");
            str = str.Replace("AddNormal_", "Thêm thẳng {0} ");
            str = str.Replace("AddRatio_", "Thêm dựa trên gốc {0}% ");
            str = str.Replace("Multiply_", "Nhân tất cả x{0} ");
            str = str.Replace("ATK", "sát thương");
            str = str.Replace("Cooldown", "s hồi chiêu");
            str = str.Replace("FireSpeed", "m/s tốc độ bắn");
            str = str.Replace("Heal", "hp/s tốc độ hồi máu");
            str = str.Replace("Speed", "m/s tốc độ di chuyển");
            str = str.Replace("Runtime_Gold", "vàng trong game");
            str = str.Replace("Gold", "vàng trong game");
            str = str.Replace("HPMax", "HP tổng");
            str = str.Replace("Runtime_HP", "HP");
            str = str.Replace("Runtime_XP", "XP");
            displayName = str;
        }
#endif
    }
}
