using UnityEngine;

namespace Luzart.Pickups
{
    /// <summary>
    /// Hút toàn bộ pickup khác trong scene về player ngay lập tức (Magnet powerup classic).
    /// Tìm mọi <see cref="PickupBase"/> đang active rồi gọi <see cref="PickupBase.TriggerMagnet"/>
    /// để chúng chạy tween InBack vào player. Nếu <see cref="_radius"/> &gt; 0 chỉ hút trong
    /// vòng radius đó (-1 / 0 = vô hạn = toàn map).
    /// </summary>
    [CreateAssetMenu(fileName = "PickupFX_MagnetAll", menuName = "Luzart/Pickup/Effects/Magnet All")]
    public class PickupEffect_MagnetAll : PickupEffect
    {
        [Tooltip("Bán kính hút (world unit). -1 hoặc 0 = vô hạn (toàn map).")]
        [SerializeField] private float _radius = -1f;

        public override void Apply(in PickupContext ctx)
        {
            if (ctx.Player == null || ctx.Player.Transform == null) return;
            Vector2 playerPos = ctx.Player.Transform.Position.Value;
            float sqrR = _radius > 0 ? _radius * _radius : float.PositiveInfinity;

            // FindObjectsByType bỏ qua inactive — phù hợp vì pickup _consumed sẽ Destroy mà không SetActive(false) trước.
            // SortMode.None = không sort, rẻ hơn ~30% so với InstanceID.
            var all = Object.FindObjectsByType<PickupBase>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                var p = all[i];
                if (p == null || p.gameObject == ctx.PickupObject) continue;
                Vector2 diff = (Vector2)p.transform.position - playerPos;
                if (diff.sqrMagnitude > sqrR) continue;
                p.TriggerMagnet(ctx.Player);
            }
        }
    }
}
