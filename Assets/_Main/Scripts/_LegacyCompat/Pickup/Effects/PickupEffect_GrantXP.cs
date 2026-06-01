using UnityEngine;

namespace Luzart.Pickups
{
    /// <summary>Cộng XP cho player. Dùng cho Diamond / LevelDiamond / gem các loại.</summary>
    [CreateAssetMenu(fileName = "PickupFX_GrantXP", menuName = "Luzart/Pickup/Effects/Grant XP")]
    public class PickupEffect_GrantXP : PickupEffect
    {
        [Tooltip("Lượng XP cộng.")]
        [SerializeField] private float _amount = 1f;

        public override void Apply(in PickupContext ctx)
        {
            if (ctx.Player == null || ctx.Player.Stats == null) return;
            var xp = ctx.Player.Stats.GetRuntime(StatType.Runtime_XP);
            if (xp == null) return;
            xp.Set(xp.Value + _amount);
        }
    }
}
