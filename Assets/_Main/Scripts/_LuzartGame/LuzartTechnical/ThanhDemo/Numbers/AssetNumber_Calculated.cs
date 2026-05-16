using UnityEngine;
namespace Luzart
{
    public sealed class AssetNumber_Calculated : AssetNumber
    {
        [SerializeField] NumberPicker baseNumber;
        [SerializeField] AssetCalculator[] calculators;
        protected override void DoInitialize()
        {
            base.DoInitialize();
            foreach (ICalculator c in calculators)
            {
                c.OnCalculatorChanged += OnCalculatorChanged;
            }
        }
        private void OnCalculatorChanged(ICalculator calculator)
        {
            InvokeValueChanged();
        }
        protected override double DoGetValue()
        {
            var baseValue = baseNumber.PickNumber().Value;
            double result = 0;
            foreach (ICalculator c in calculators)
            {
                var val = c.Calculate(baseValue);
                result += val;
            }
            return result;
        }
    }
}
