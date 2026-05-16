using System.Collections.Generic;
using UnityEngine;
namespace Luzart
{
    [CreateAssetMenu(fileName = "New Aggregation Number", menuName = "ThanhDemo/Numbers/Aggregation Number")]
    public sealed class AssetNumber_Aggregation : AssetNumber, INumberWithContribution
    {
        [SerializeField] AggregationModes aggregationMode;
        [SerializeField] NumberPicker[] numbers;
        List<INumber> _numbers;
        bool _calculated;
        double _value;
        System.Action<INumber> _delegate_Number_OnChanged;
        System.Action<INumber> delegate_Number_OnChanged => _delegate_Number_OnChanged ??= Number_OnChanged;
        protected override void DoInitialize()
        {
            base.DoInitialize();
            _numbers = new();
            for (int i = 0; i < numbers.Length; i++)
            {
                _numbers.Add(numbers[i].PickNumber());
            }
        }
        protected override void DoStartContent()
        {
            base.DoStartContent();
            for (int i = 0; i < _numbers.Count; i++)
            {
                INumber n = _numbers[i];
                n.Changed -= delegate_Number_OnChanged;
                n.Changed += delegate_Number_OnChanged;
            }
            Refresh();
        }
        protected override void DoStopContent()
        {
            base.DoStopContent();
            for (int i = 0; i < _numbers.Count; i++)
            {
                INumber n = _numbers[i];
                n.Changed -= delegate_Number_OnChanged;
            }
            _numbers.Clear();
        }
        private void Number_OnChanged(INumber obj)
        {
            Refresh();
        }
        protected override double DoGetValue()
        {
            if (!_calculated)
            {
                Refresh();
            }
            return _value;
        }
        void Refresh()
        {
            var d = Calculate();
            if (d == _value) return;
            _calculated = true;
            _value = d;
            InvokeValueChanged();
        }
        double Calculate()
        {
            double d = 0;
            switch (aggregationMode)
            {
                case AggregationModes.Sum:
                    d = CalculateSum();
                    break;
                case AggregationModes.Multiply:
                    d = CalculateMultiply();
                    break;
                case AggregationModes.Average:
                    d = CalculateAverage();
                    break;
                case AggregationModes.Max:
                    d = CalculateMax();
                    break;
                case AggregationModes.Min:
                    d = CalculateMin();
                    break;
                default:
                    break;
            }
            return d;
        }
        private double CalculateSum()
        {
            double sum = 0;
            int length = _numbers.Count;
            for (int i = 0; i < length; i++)
            {
                if (_numbers[i] != null)
                {
                    sum += _numbers[i].Value;
                }
                else
                {
                    Debug.LogError($"[AssetNumber_Aggregation] _numbers[{i}] is null in {name}");
                }
            }
            return sum;
        }
        private double CalculateMultiply()
        {
            try
            {
                double product = 1;
                int length = _numbers.Count;
                for (int i = 0; i < length; i++)
                {
                    product *= _numbers[i].Value;
                }
                return product;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AssetNumber_Aggregation] CalculateMultiply error in {name} : {e}");
                return 0;
            }
        }
        private double CalculateAverage()
        {
            if (_numbers.Count <= 0) return 0;
            double sum = CalculateSum();
            return sum / _numbers.Count;
        }
        private double CalculateMax()
        {
            double max = double.MinValue;
            int length = _numbers.Count;
            for (int i = 0; i < length; i++)
            {
                var val = _numbers[i].Value;
                if (val > max)
                {
                    max = val;
                }
            }
            return max;
        }
        private double CalculateMin()
        {
            double min = double.MaxValue;
            int length = _numbers.Count;
            for (int i = 0; i < length; i++)
            {
                var val = _numbers[i].Value;
                if (val < min)
                {
                    min = val;
                }
            }
            return min;
        }
        void INumberWithContribution.Contribute(INumber subNumber)
        {
            _numbers.Add(subNumber);
            subNumber.Changed -= delegate_Number_OnChanged;
            subNumber.Changed += delegate_Number_OnChanged;
            Refresh();
        }
        void INumberWithContribution.Uncontribute(INumber subNumber)
        {
            if (_numbers.Remove(subNumber))
            {
                if (!_numbers.Contains(subNumber))
                {
                    subNumber.Changed -= delegate_Number_OnChanged;
                }
                Refresh();
            }
        }
        enum AggregationModes
        {
            Sum = 0,
            Multiply = 1,
            Average = 2,
            Max = 10,
            Min = 11,
        }
    }
}
