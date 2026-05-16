using System;
using System.Collections.Generic;
using UnityEngine;
namespace Luzart
{
    public abstract class AssetNumber : AbstractScriptableContent, INumber
    {
        // Thread-local stack để track chain
        //[ThreadStatic]
        private static Stack<AssetNumber> _traceStack;
        double INumber.Value => DoGetValue();
        Action<INumber> _onValueChanged;
        event Action<INumber> INumber.Changed
        {
            add => _onValueChanged += value;
            remove => _onValueChanged -= value;
        }
        private double DoGetValueSafe()
        {
            if (_traceStack == null)
                _traceStack = new Stack<AssetNumber>();
            // Nếu đã từng thấy this trong stack -> CYCLE!
            if (_traceStack.Contains(this))
            {
                PrintCycleTrace(this);
                throw new Exception($"Cycle dependency detected in {this.name}");
            }
            _traceStack.Push(this);
            double value = 0;
            try
            {
                value = DoGetValue();
            }
            finally
            {
                _traceStack.Pop();
            }
            return value;
        }
        protected abstract double DoGetValue();
        protected void InvokeValueChanged()
        {
            _onValueChanged?.Invoke(this);
        }
        private void PrintCycleTrace(AssetNumber last)
        {
            Debug.LogError("===== CYCLE DETECTED =====");
            foreach (var item in _traceStack)
            {
                Debug.LogError($" → {item.name}", item);
            }
            Debug.LogError($" → {last.name} (loop here)", last);
            Debug.LogError("==========================");
        }
    }
}
