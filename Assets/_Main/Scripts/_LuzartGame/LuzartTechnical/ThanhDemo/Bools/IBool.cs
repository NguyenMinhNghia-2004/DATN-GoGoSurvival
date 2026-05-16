using System;
namespace Luzart
{
    public interface IBool
    {
        bool Value { get; }
        event Action<IBool> Changed;
    }
    public class BoolValue : IBool
    {
        private bool _value = false;
        bool IBool.Value => _value;
        private event Action<IBool> _onValueChanged;
        event Action<IBool> IBool.Changed
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
        public BoolValue(bool value = false)
        {
            _value = value;
        }
    }
    public interface IBoolSet : IBool
    {
        void Set(bool b);
    }
    public class BoolSet: IBoolSet
    {
        private bool _value = false;
        bool IBool.Value => _value;
        private event Action<IBool> _onValueChanged;
        event Action<IBool> IBool.Changed
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
        public BoolSet(bool value = false)
        {
            _value = value;
        }
        void IBoolSet.Set(bool b)
        {
            if (_value != b)
            {
                _value = b;
                _onValueChanged?.Invoke(this);
            }
        }
    }
}
