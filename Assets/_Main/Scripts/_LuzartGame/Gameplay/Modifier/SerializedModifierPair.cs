using System.Numerics;
using UnityEngine;
namespace Luzart
{
    [System.Serializable]
    public class SerializedModifierPair : IModifierPair
    {
        [SerializeField] SerializedModifierFactor factor;
        [SerializeField] AssetModifier modifier;
        public IModifierFactor Factor => factor;
        public IModifier Modifier => modifier;
        public SerializedModifierFactor FactorEditor
        {
            get => factor;
            set => factor = value;
        }
        public AssetModifier AssetModifierEditor
        {
            get => modifier;
            set => modifier = value;
        }
    }
}
