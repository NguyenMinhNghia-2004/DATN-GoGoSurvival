using UnityEngine;
namespace Luzart
{
    [CreateAssetMenu(fileName = "New Constant Number", menuName = "ThanhDemo/Numbers/Constant Number")]
    public sealed class AssetNumber_Constant : AssetNumber
    {
        [SerializeField] double value;
        protected override double DoGetValue()
        {
            return value;
        }
    }
}
