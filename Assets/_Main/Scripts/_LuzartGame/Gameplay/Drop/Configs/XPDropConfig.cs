using UnityEngine;
namespace Luzart
{
    [CreateAssetMenu(fileName = "XPDropConfig", menuName = "Luzart/Drop/XPDropConfig", order = 1)]
    public class XPDropConfig : DropConfig
    {
        [Header("XP Drop Config")]
        [SerializeField] private int xpAmount = 10;
        public int XPAmount => xpAmount;
        public override DropEntity CreateDrop(IEntity owner)
        {
            return new XPDropEntity(this, owner);
        }
    }
}