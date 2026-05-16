using UnityEngine;
namespace Luzart
{
    public sealed class AssetNumber_Formula : AssetNumber
    {
        [SerializeField] string formula;
        [SerializeField] VariableConfig[] variables;
        protected override double DoGetValue()
        {
            return 0;
        }
        [System.Serializable]
        class VariableConfig
        {
            [SerializeField] string name;
            [SerializeField] NumberPicker numberPicker;
        }
    }
}
