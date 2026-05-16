using UnityEngine;
namespace Luzart
{
    [System.Serializable]
    public class NumberPicker : INumberPicker
    {
        [SerializeField] NumberMode mode;
        [SerializeField] double constant;
        [SerializeField] AssetNumber asset;
        public NumberPicker(NumberMode modes, double constant = 0, AssetNumber asset = null)
        {
            this.mode = modes;
            this.constant = constant;
            this.asset = asset;
        }
        public double Value => DoGetValue();
        public INumber PickNumber()
        {
            return DoGetPickNumber();
        }
        private double DoGetValue()
        {
            return PickNumber().Value;
        }
        private INumber DoGetPickNumber()
        {
            return mode switch
            {
                NumberMode.Constant => new RuntimeNumber_Constant(constant),
                NumberMode.AssetNumber => asset,
                _ => throw new System.Exception($"Unsupported mode {mode}"),
            };
        }
    }
    public interface INumberPicker
    {
        double Value { get; }
        INumber PickNumber();
    }
    public enum NumberMode
    {
        Constant,
        AssetNumber,
    }
    public interface INumberPickerPair
    {
        INumberPicker NumberMin { get; }
        INumberPicker NumberMax { get; }
    }
    [System.Serializable]
    public class PickerPair : INumberPickerPair
    {
        [SerializeField] private NumberPicker numberPickerMin;
        [SerializeField] private NumberPicker numberPickerMax;
        public PickerPair(int min, int max)
        {
            numberPickerMin = new NumberPicker(NumberMode.Constant, min);
            numberPickerMax = new NumberPicker(NumberMode.Constant, max);
        }
        INumberPicker INumberPickerPair.NumberMin => numberPickerMin;
        INumberPicker INumberPickerPair.NumberMax => numberPickerMax;
    }
}
