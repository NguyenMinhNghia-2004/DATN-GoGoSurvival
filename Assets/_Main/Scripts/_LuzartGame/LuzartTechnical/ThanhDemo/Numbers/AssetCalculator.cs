using Luzart;
using System;
namespace Luzart
{
    public abstract class AssetCalculator : AbstractScriptableContent, ICalculator
    {
        event Action<ICalculator> _OnCalculatorChanged;
        event Action<ICalculator> ICalculator.OnCalculatorChanged
        {
            add => _OnCalculatorChanged += value;
            remove => _OnCalculatorChanged -= value;
        }
        double ICalculator.Calculate(double x)
        {
            return DoCalculate(x);
        }
        protected abstract double DoCalculate(double x);
    }
}
