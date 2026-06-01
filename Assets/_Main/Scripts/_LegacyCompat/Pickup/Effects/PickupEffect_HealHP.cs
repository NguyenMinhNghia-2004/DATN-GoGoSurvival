using UnityEngine;

namespace Luzart.Pickups
{
    /// <summary>
    /// Hồi HP cho player. <see cref="_amount"/> &gt; 0 = absolute amount. Nếu
    /// <see cref="_isPercent"/> = true thì <see cref="_amount"/> = % HPMax (0.3 = 30%).
    /// </summary>
    [CreateAssetMenu(fileName = "PickupFX_HealHP", menuName = "Luzart/Pickup/Effects/Heal HP")]
    public class PickupEffect_HealHP : PickupEffect
    {
        [Tooltip("Lượng HP hồi (absolute) hoặc tỷ lệ HPMax (nếu Is Percent = true).")]
        [SerializeField] private float _amount = 20f;

        [Tooltip("True: amount tính theo % HPMax. False: amount là số tuyệt đối.")]
        [SerializeField] private bool _isPercent;

        public override void Apply(in PickupContext ctx)
        {
            if (ctx.Player == null || ctx.Player.Stats == null) return;
            var hp = ctx.Player.Stats.GetRuntime(StatType.Runtime_HP);
            if (hp == null) return;
            double heal = _amount;
            double max = ctx.Player.Stats.Get(StatType.HPMax).Value;
            if (_isPercent) heal = max * _amount;
            double next = System.Math.Min(hp.Value + heal, max);
            hp.Set(next);
        }
    }
}
