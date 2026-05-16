using System;
namespace Luzart
{
    public abstract class RuntimeNumber : INumber, IDisposable
    {
        double INumber.Value => DoGetValue();
        Action<INumber> _onValueChanged;
        event Action<INumber> INumber.Changed
        {
            add => _onValueChanged += value;
            remove => _onValueChanged -= value;
        }
        void IDisposable.Dispose()
        {
            DoDispose();
        }
        protected abstract double DoGetValue();
        protected virtual void DoDispose()
        {
        }
        protected void InvokeValueChanged()
        {
            _onValueChanged?.Invoke(this);
        }
    }
}
