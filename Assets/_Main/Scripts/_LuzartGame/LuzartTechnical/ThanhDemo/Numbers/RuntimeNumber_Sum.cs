using System.Collections.Generic;
using UnityEngine.Pool;
namespace Luzart
{
    public class RuntimeNumber_Sum : RuntimeNumber
    {
        List<INumber> _elements;
        public RuntimeNumber_Sum(IEnumerable<INumber> elements)
        {
            _elements = ListPool<INumber>.Get();
            _elements.Clear();
            _elements.AddRange(elements);
            foreach (var e in _elements)
            {
                e.Changed += Element_OnValueChanged;
            }
        }
        protected override void DoDispose()
        {
            base.DoDispose();
            foreach (var e in _elements)
            {
                e.Changed -= Element_OnValueChanged;
            }
            _elements.Clear();
            ListPool<INumber>.Release(_elements);
        }
        protected override double DoGetValue()
        {
            double sum = 0;
            foreach (var e in _elements)
            {
                sum += e.Value;
            }
            return sum;
        }
        private void Element_OnValueChanged(INumber obj)
        {
            InvokeValueChanged();
        }
    }
}
