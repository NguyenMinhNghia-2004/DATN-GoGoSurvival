using System;
namespace Luzart
{
    public class RuntimeNumber_SimpleBoosted : RuntimeNumber
    {
        INumber _baseNumber;
        INumber _addNumber;
        INumber _multiplyNumber;
        INumber _powNumber;
        double _value;
        public RuntimeNumber_SimpleBoosted(INumber baseNumber, INumber addNumber = null, INumber multiplyNumber = null, INumber powNumber = null)
        {
            _baseNumber = baseNumber;
            _addNumber = (addNumber == null ? new Number(0) : addNumber);
            _multiplyNumber = (multiplyNumber == null ? new Number(1) : multiplyNumber);
            _powNumber = (powNumber == null ? new Number(1) : powNumber);
            _baseNumber.Changed += OnAnyNumberChanged;
            _addNumber.Changed += OnAnyNumberChanged;
            _multiplyNumber.Changed += OnAnyNumberChanged;
            _powNumber.Changed += OnAnyNumberChanged;
        }
        private void OnAnyNumberChanged(INumber obj)
        {
            Recalculate();
        }
        void Recalculate()
        {
            double newValue = Math.Pow((_baseNumber.Value + _addNumber.Value) * _multiplyNumber.Value, _powNumber.Value);
            if (newValue == _value) return;
            _value = newValue;
            InvokeValueChanged();
        }
        protected override double DoGetValue()
        {
            Recalculate();
            return _value;
        }
        protected override void DoDispose()
        {
            base.DoDispose();
            _baseNumber.Changed -= OnAnyNumberChanged;
            _addNumber.Changed -= OnAnyNumberChanged;
            _multiplyNumber.Changed -= OnAnyNumberChanged;
            _powNumber.Changed -= OnAnyNumberChanged;
        }
    }
}
