using System;
namespace Luzart
{
    public interface INumberWithSet : INumber
    {
        void Set(double d);
    }
    public class Number : INumberWithSet
    {
        public Number()
        {
        }
        public Number(double d)
        {
            _value = d;
        }
        private double _value = 0;
        double INumber.Value => _value;
        private event Action<INumber> _onValueChanged;
        event Action<INumber> INumber.Changed
        {
            add
            {
                _onValueChanged += value;
            }
            remove
            {
                _onValueChanged -= value;
            }
        }
        void INumberWithSet.Set(double d)
        {
            if (_value == d) return;
            _value = d;
            _onValueChanged?.Invoke(this);
        }
    }
}
