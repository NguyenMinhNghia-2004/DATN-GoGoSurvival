using System;
using UnityEngine;
namespace Luzart
{
    [CreateAssetMenu(fileName = "New Calculator Stat", menuName = "ThanhDemo/Calculators/Stat Calculator")]
    public class AssetCalculator_Stat : AssetCalculator
    {
        [SerializeField]
        private AssetStatDefinition statDefinition;
        [SerializeField]
        private ModeCalculation modeCalculation;
        [SerializeField]
        private NumberPicker numberPicker;
        [SerializeField]
        private AssetCalculator baseAssetCalculator;
        public AssetStatDefinition StatDefinition => statDefinition;
        protected override double DoCalculate(double x)
        {
            if(modeCalculation == ModeCalculation.Add)
            {
                return numberPicker.Value;
            }
            else if (modeCalculation == ModeCalculation.MultiBase)
            {
                return x* numberPicker.Value;
            }
            return 0;
        }
    }
    [System.Serializable]
    public class StatCalculator : ICalculator
    {
        public StatCalculator(AssetStatDefinition statDefinition, ModeCalculation modeCalculation, NumberPicker numberPicker = null, AssetCalculator baseAssetCalculator = null)
        {
            this.statDefinition = statDefinition;
            this.modeCalculation = modeCalculation;
            this.numberPicker = numberPicker;
            this.baseAssetCalculator = baseAssetCalculator;
        }
        //
        [SerializeField]
        private AssetStatDefinition statDefinition;
        //
        [SerializeField]
        private ModeCalculation modeCalculation;
        //
        private bool IsNotCustom => modeCalculation != ModeCalculation.Custom;
        [SerializeField]
        private NumberPicker numberPicker;
        //
        [SerializeField]
        private AssetCalculator baseAssetCalculator;
        public IStatDefinition Definition => statDefinition;
        public ModeCalculation ModeCalculation => modeCalculation;
        private event Action<ICalculator> _onCalculatorChanged;
        event Action<ICalculator> ICalculator.OnCalculatorChanged
        {
            add
            {
                _onCalculatorChanged += value;
            }
            remove
            {
                _onCalculatorChanged -= value;
            }
        }
        double ICalculator.Calculate(double x)
        {
            if (modeCalculation == ModeCalculation.Add)
            {
                return numberPicker.Value;
            }
            else if (modeCalculation == ModeCalculation.MultiBase)
            {
                return x * numberPicker.Value;
            }
            else if(modeCalculation == ModeCalculation.MultiAll)
            {
                return 1 + numberPicker.Value;
            }
            else
            {
                ICalculator iCalculator = baseAssetCalculator;
                if (iCalculator != null)
                {
                    return iCalculator.Calculate(x);
                }
                else
                    return 0;
            }
        }
    }
    public enum ModeCalculation
    {
        Add = 0,
        MultiBase = 1,
        MultiAll = 2,
        Custom = 100,
    }
}
