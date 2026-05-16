using System.Collections.Generic;
using UnityEngine;
namespace Luzart
{
    public abstract class AssetModifier : AbstractScriptableContent, IModifier
    {
        [SerializeField] private AssetModifierDefinition definition;
        [SerializeField] private List<SerializedModifierFactor> factors;
        List<IModifierFactor> _factors;
        IModifierDefinition IModifier.Definition => definition;
        IReadOnlyList<IModifierFactor> IModifier.Factors => _factors;
        protected override void DoInitialize()
        {
            base.DoInitialize();
            _factors = new();
        }
        protected override void DoStartContent()
        {
            base.DoStartContent();
            for (int i = 0; i < _factors.Count; i++)
            {
                IModifierFactor f = _factors[i];
                DoAddFactor(f);
            }
        }
        protected override void DoStopContent()
        {
            base.DoStopContent();
            for (int i = _factors.Count - 1; i >= 0; i--)
            {
                IModifierFactor f = _factors[i];
                DoRemoveFactor(f);
            }
        }
        void IModifier.AddFactor(IModifierFactor factor)
        {
            DoAddFactor(factor);
        }
        void IModifier.RemoveFactor(IModifierFactor factor)
        {
            DoRemoveFactor(factor);
        }
        void DoAddFactor(IModifierFactor factor)
        {
            if (_factors.Contains(factor)) return;
            IModifierDefinition iModifierDefinition = definition;
            if (factor.Definition != iModifierDefinition)
            {
                Debug.LogError($"[AssetModifier] Can't Add Factor. Must be {definition.name} in {this.name} ");
                Debug.LogError($"[AssetModifier] Factor Is {factor.Definition.DisplayName.Value} it must {iModifierDefinition.DisplayName.Value} ");
                return;
            }
            _factors.Add(factor);
            OnFactorIncluded(factor);
            Debug.Log($"[AssetModifier] Add Factor {factor.Definition.DisplayName.Value} in {this.name} with array has {_factors.Count} elements");
        }
        void DoRemoveFactor(IModifierFactor factor)
        {
            if (_factors.Remove(factor))
            {
                OnFactorExcluded(factor);
            }
        }
        protected virtual void OnFactorIncluded(IModifierFactor factor)
        {
        }
        protected virtual void OnFactorExcluded(IModifierFactor factor)
        {
        }
    }
}
