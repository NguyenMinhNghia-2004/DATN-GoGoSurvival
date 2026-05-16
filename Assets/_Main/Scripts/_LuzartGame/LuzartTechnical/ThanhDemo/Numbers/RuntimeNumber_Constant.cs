namespace Luzart
{
    public class RuntimeNumber_Constant : RuntimeNumber
    {
        double _value;
        public RuntimeNumber_Constant(double value)
        {
            _value = value;
        }
        protected override double DoGetValue()
        {
            return _value;
        }
    }
}
