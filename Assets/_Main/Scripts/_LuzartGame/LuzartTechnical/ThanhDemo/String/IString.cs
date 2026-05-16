using System;
namespace Luzart
{
    public interface IString
    {
        string Value { get; }
        event Action<IString> Changed;
    }
    public class StringValue : IString
    {
        public StringValue(string value)
        {
            _value = value;
        }
        private string _value;
        string IString.Value => _value;
        private event Action<IString> _onChanged;
        event Action<IString> IString.Changed
        {
            add
            {
                _onChanged += value;
            }
            remove
            {
                _onChanged -= value;
            }
        }
    }
}
