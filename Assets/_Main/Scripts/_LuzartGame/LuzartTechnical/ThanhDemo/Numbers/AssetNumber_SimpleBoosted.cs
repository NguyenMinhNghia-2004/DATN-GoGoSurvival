using System;
using UnityEngine;
namespace Luzart
{
    public sealed class AssetNumber_SimpleBoosted : AssetNumber
    {
        [SerializeField] NumberPicker baseNumber = new NumberPicker(NumberMode.Constant,0);
        [SerializeField] NumberPicker addNumber = new NumberPicker(NumberMode.Constant,0);
        [SerializeField] NumberPicker multiplyNumber = new NumberPicker(NumberMode.Constant, 1);
        [SerializeField] NumberPicker powNumber = new NumberPicker(NumberMode.Constant, 1);
        INumber _baseNumber;
        INumber _addNumber;
        INumber _multiplyNumber;
        INumber _powNumber;
        double _value;
        protected override void DoInitialize()
        {
            base.DoInitialize();
            _isInit = false;
            _baseNumber = baseNumber.PickNumber();
            _addNumber = addNumber.PickNumber();
            _multiplyNumber = multiplyNumber.PickNumber();
            _powNumber = powNumber.PickNumber();
            _isInit = true;
        }
        private bool _isInit = false;
        protected override void DoStartContent()
        {
            base.DoStartContent();
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
            if(_baseNumber == null)
            {
                Debug.LogError($"{this} + isInit {_isInit} with {baseNumber}");
            }
            double _baseValue = _baseNumber.Value;
            double _addValue = _addNumber.Value;
            double _multiplyValue = _multiplyNumber.Value;
            double _powValue = _powNumber.Value;
            double newValue = Math.Pow((_baseValue + _addValue) * _multiplyValue, _powValue);
            if (newValue == _value) return;
            _value = newValue;
            InvokeValueChanged();
        }
        protected override double DoGetValue()
        {
            Recalculate();
            return _value;
        }
    }
}
