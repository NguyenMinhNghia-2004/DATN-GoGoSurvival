using System;
namespace Luzart
{
    public enum ComparisonMode
    {
        Equal = 0,
        NotEqual = 1,
        Less = 2,
        LEqual = 3,
        Greater = 4,
        GEqual = 5,
    }
    public static class ComparisonModeExtentions
    {
        public static bool IsTrue(this ComparisonMode mode, double n1, double n2)
        {
            switch (mode)
            {
                case ComparisonMode.Equal:
                    return n1 == n2;
                case ComparisonMode.NotEqual:
                    return n1 != n2;
                case ComparisonMode.Less:
                    return n1 < n2;
                case ComparisonMode.LEqual:
                    return n1 <= n2;
                case ComparisonMode.Greater:
                    return n1 > n2;
                case ComparisonMode.GEqual:
                    return n1 >= n2;
                default:
                    throw new Exception("Dont have ComparisonMode ");
            }
        }
    }
    public class Runtime_BoolCompareNumber : IBool, IDisposable
    {
        private INumber _number1;
        private INumber _number2;
        private ComparisonMode _comparisonMode;
        bool IBool.Value => _comparisonMode.IsTrue(_number1.Value, _number2.Value);
        private event Action<IBool> _changed;
        event Action<IBool> IBool.Changed
        {
            add => _changed += value;
            remove => _changed -= value;
        }
        public Runtime_BoolCompareNumber(INumber num1, ComparisonMode comparisonMode, INumber num2)
        {
            _number1 = num1;
            _number2 = num2;
            _comparisonMode = comparisonMode;
            _number1.Changed += OnAnyNumberChange;
            _number2.Changed += OnAnyNumberChange;
        }
        void IDisposable.Dispose()
        {
            _number1.Changed -= OnAnyNumberChange;
            _number2.Changed -= OnAnyNumberChange;
        }
        private void OnAnyNumberChange(INumber inumber)
        {
            _changed?.Invoke(this);
        }
    }
}
