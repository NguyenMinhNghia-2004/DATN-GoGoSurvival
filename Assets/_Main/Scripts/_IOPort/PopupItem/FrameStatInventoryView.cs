using TMPro;
using UnityEngine;
using Luzart.NewBase;

namespace Luzart
{
    public class FrameStatInventoryView : ViewT<FrameStatInventoryVM>
    {
        [SerializeField] private BaseSelect bs;
        [SerializeField] private TMP_Text txt;

        protected override void OnSetup()
        {
            base.OnSetup();
            SetBaseSelect(Data.StatType);
            var numberStat = Data.GetNumberStat();
            OnChangeData(numberStat);
            numberStat.Changed += OnChangeData;
        }

        protected override void OnTeardown()
        {
            base.OnTeardown();
            var numberStat = Data.GetNumberStat();
            numberStat.Changed -= OnChangeData;
        }

        private void OnChangeData(INumber numberStat)
        {
            if (txt != null) txt.text = numberStat.Value.ToString();
        }

        private void SetBaseSelect(StatType statType)
        {
            if (bs == null) return;
            switch (statType)
            {
                case StatType.ATK:
                    {
                        bs.Select(0);
                        break;
                    }
                case StatType.HPMax:
                    {
                        bs.Select(1);
                        break;
                    }
            }
        }
    }
}
