using UnityEngine;

namespace Luzart
{
    [System.Serializable]
    public class FrameStatInventoryVM
    {
        [SerializeField] private StatType statType;
        [SerializeField] private NumberPicker numberPicker;
        public StatType StatType => statType;

        public INumber GetNumberStat()
        {
            return numberPicker.PickNumber();
        }
    }
}
