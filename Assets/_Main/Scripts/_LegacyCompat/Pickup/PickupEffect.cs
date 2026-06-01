using UnityEngine;

namespace Luzart.Pickups
{
    /// <summary>
    /// Strategy base class cho mọi effect mà pickup có thể trigger khi được nhặt.
    /// Tách thành ScriptableObject để mỗi effect là 1 asset độc lập, share được giữa
    /// nhiều <see cref="PickupData"/> (vd: 1 effect "Heal20HP" dùng cho cả heart pickup
    /// và food pickup).
    ///
    /// <para>Subclass tự khai báo CreateAssetMenu để designer create từ Project window.</para>
    /// </summary>
    public abstract class PickupEffect : ScriptableObject
    {
        /// <summary>Thực thi effect 1 lần khi pickup được nhặt.</summary>
        public abstract void Apply(in PickupContext ctx);
    }
}
