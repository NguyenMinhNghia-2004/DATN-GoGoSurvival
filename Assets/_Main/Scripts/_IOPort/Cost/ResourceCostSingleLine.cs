using TMPro;
using UnityEngine;

namespace Luzart
{
    public class ResourceCostSingleLine : ViewT<IResourceCost>
    {
        [SerializeField] private TMP_Text txt;
        [SerializeField] private ObjectView objectView;

        protected override void OnSetup()
        {
            base.OnSetup();
            ChangeData(null);
            Data.ResourcePool.Value.Changed += ChangeData;
            Data.Amount.Changed += ChangeData;
        }

        protected override void OnTeardown()
        {
            base.OnTeardown();
            Data.ResourcePool.Value.Changed -= ChangeData;
            Data.Amount.Changed -= ChangeData;
        }

        private void ChangeData(INumber data)
        {
            if (txt != null)
                txt.text = $"{Data.ResourcePool.Value.Value}/{Data.Amount.Value}";
            if (objectView != null)
                objectView.Setup(new ObjectViewData(Data.ResourcePool));
        }
    }
}
