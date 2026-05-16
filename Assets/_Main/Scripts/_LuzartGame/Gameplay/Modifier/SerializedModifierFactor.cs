using UnityEngine;
namespace Luzart
{
    [System.Serializable]
    public class SerializedModifierFactor : IModifierFactor
    {
        [SerializeField] private AssetModifierDefinition definition;
        [SerializeField] private NumberPicker value;
        IModifierDefinition IModifierFactor.Definition => definition;
        INumber IModifierFactor.Value => TryGetNumber();
        private INumber _value;
        private INumber TryGetNumber()
        {
            if (_value == null)
            {
                _value = value.PickNumber();
            }
            return _value;
        }
        public AssetModifierDefinition DefinitionEditor
        {
            get => definition;
            set => definition = value;
        }
        public NumberPicker ValueEditor
        {
            get => value;
            set => this.value = value;
        }
    }
}
