namespace Luzart
{
    public class RuntimeNumber_Variable : RuntimeNumber, INumberWithSet
    {
        double _value;
        public RuntimeNumber_Variable(double value)
        {
            _value = value;
        }
        protected override double DoGetValue()
        {
            return _value;
        }
        void INumberWithSet.Set(double d)
        {
            if (_value == d) return;
            _value = d;
            InvokeValueChanged();
        }
    }
}
