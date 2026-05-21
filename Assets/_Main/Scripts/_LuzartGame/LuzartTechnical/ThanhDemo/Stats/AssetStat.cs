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
        // Lazy-init: AssetStat SOs are passed directly to StatsBehavior.Add() by
        // EnemySpawnerManager without IContent.Initialize() being called — relying on
        // DoInitialize() alone leaves _value null and silently breaks enemy stats.
        INumber IStat.Value => EnsureValue();
        protected override void DoInitialize()
        {
            base.DoInitialize();
            EnsureValue();
        }
        public double Value => EnsureValue().Value;
        private INumber EnsureValue()
        {
            if (_value == null) _value = value.PickNumber();
            return _value;
        }
    }
}
