using Luzart;
using UnityEngine;
namespace Luzart
{
    [CreateAssetMenu(fileName = "New Stat Definition", menuName = "ThanhDemo/Stats/Stat Definition")]
    public class AssetStatDefinition : AbstractScriptableContent, IStatDefinition
    {
        [SerializeField] StatType statType;
        [SerializeField] string displayName;
        [SerializeField] Sprite statIcon;
        StatType IStatDefinition.StatType => statType;
        IString IStatDefinition.DisplayName => TryGetString();
        Sprite IStatDefinition.Icon => statIcon;
        protected override void Editor_DoOnValidateScriptableObject()
        {
            base.Editor_DoOnValidateScriptableObject();
            //displayName = name.Replace("StatDefinition_","");
        }
        private IString _displayName;
        private IString TryGetString()
        {
            if(_displayName == null)
            {
                _displayName = new StringValue(displayName);
            }
            return _displayName;
        }
    }
}
