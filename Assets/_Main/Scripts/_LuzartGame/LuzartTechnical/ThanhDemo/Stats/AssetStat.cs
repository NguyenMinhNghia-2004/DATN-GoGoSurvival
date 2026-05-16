using Luzart;
using UnityEngine;
namespace Luzart
{
    [CreateAssetMenu(fileName = "New Stat", menuName = "ThanhDemo/Stats/Stat")]
    public class AssetStat : AbstractScriptableContent, IStat
    {
        [SerializeField] AssetStatDefinition definition;
        [SerializeField] NumberPicker value;
        INumber _value;
        IStatDefinition IStat.Definition => definition;
        INumber IStat.Value => _value;
        protected override void DoInitialize()
        {
            base.DoInitialize();
            _value = value.PickNumber();
        }
        public double Value => _value.Value;
    }
}
