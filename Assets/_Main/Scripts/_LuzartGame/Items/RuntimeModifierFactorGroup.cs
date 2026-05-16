using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Pool;
namespace Luzart
{
    public class RuntimeModifierFactorGroup : IModifierFactorGroup, IDisposable
    {
        List<IModifierPair> _pairs;
        List<IModifierFactor> _factors;
        IReadOnlyList<IModifierFactor> IModifierFactorGroup.Factors => _factors;
        public RuntimeModifierFactorGroup(IEnumerable<IModifierPair> pairs)
        {
            _pairs = ListPool<IModifierPair>.Get();
            _pairs.Clear();
            _pairs.AddRange(pairs);
            _factors = ListPool<IModifierFactor>.Get();
            _factors.AddRange(_pairs.Select(p => p.Factor));
        }
        void IModifierFactorGroup.Activate()
        {
            foreach (var f in _pairs)
            {
                f.Modifier.AddFactor(f.Factor);
            }
        }
        void IModifierFactorGroup.Deactivate()
        {
            foreach (var f in _pairs)
            {
                f.Modifier.RemoveFactor(f.Factor);
            }
        }
        void IDisposable.Dispose()
        {
            // dipose 2 lists
            ListPool<IModifierPair>.Release(_pairs);
            ListPool<IModifierFactor>.Release(_factors);
        }
    }
}