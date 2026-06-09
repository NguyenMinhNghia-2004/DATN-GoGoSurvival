using UnityEngine;

namespace Luzart
{
    public class AssetCostVisualResolver_ResourcePool : AbstractScriptableContent, ICostVisualResolver
    {
        [SerializeField] ViewT<IResourceCost> resourceCostView_singleLine;

        IView ICostVisualResolver.GetCostView(ICost data, object displayContext)
        {
            if (data is IResourceCost resourceCost)
            {
                if (displayContext is EResourceCostView.SingleLine)
                {
                    return resourceCostView_singleLine;
                }
            }
            return null;
        }
    }
}
