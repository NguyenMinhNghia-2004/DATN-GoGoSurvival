using System.Collections.Generic;
namespace Luzart
{
    public class RuntimeStat : IStat
    {
        IStatDefinition _definition;
        INumber _value;
        IStatDefinition IStat.Definition => _definition;
        INumber IStat.Value => _value;
        private double _valueCalculated = 0;
        public RuntimeStat(IStatDefinition definition, INumber value)
        {
            _definition = definition;
            _value = value;
        }
        public void InitStat(List<ICalculator> calculators)
        {
            _valueCalculated = 0;
            foreach (var calculator in calculators)
            {
                _valueCalculated = calculator.Calculate(_value.Value);
            }
        }
        public double GetValue()
        {
            return _valueCalculated;
        }
    }
}
