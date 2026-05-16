using UnityEngine;
namespace Luzart
{
    [CreateAssetMenu(fileName = "CoinDropConfig", menuName = "Luzart/Drop/CoinDropConfig", order = 2)]
    public class CoinDropConfig : DropConfig
    {
        [Header("Coin Drop")]
        [SerializeField] private int coinAmount = 10;
        public int CoinAmount => coinAmount;

        public override DropEntity CreateDrop(IEntity owner)
        {
            // TODO: implement CoinDropEntity when gameplay wires coin pickup.
            // For now reuse XPDropEntity (same pattern: emit value on pickup).
            return new XPDropEntity(null, owner);
        }
    }
}
