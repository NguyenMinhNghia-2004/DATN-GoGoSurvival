using UnityEngine;

namespace Luzart.Pickups
{
    /// <summary>Cộng coin vào CurrencyManager singleton — HUD coin label tự update qua OnCoinChanged.</summary>
    [CreateAssetMenu(fileName = "PickupFX_GrantCoin", menuName = "Luzart/Pickup/Effects/Grant Coin")]
    public class PickupEffect_GrantCoin : PickupEffect
    {
        [Tooltip("Số coin cộng.")]
        [SerializeField] private long _amount = 1;

        public override void Apply(in PickupContext ctx)
        {
            if (CurrencyManager.Instance == null) return;
            CurrencyManager.Instance.AddCoin(_amount);
        }
    }
}
