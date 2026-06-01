using Luzart;
using UnityEngine;

namespace Luzart.Pickups
{
    /// <summary>
    /// Bundle dữ liệu runtime truyền vào <see cref="PickupEffect.Apply"/>.
    /// Giúp effect không phải tự resolve PlayerCharacter / vị trí pickup mỗi lần.
    /// Readonly struct = không alloc heap khi pass-by-value.
    /// </summary>
    public readonly struct PickupContext
    {
        /// <summary>World position của pickup khi nó vừa bị nhặt (sau khi fly xong).</summary>
        public readonly Vector3 PickupPosition;
        /// <summary>Player nhặt pickup. Có thể null nếu chưa init xong — effect tự xử.</summary>
        public readonly PlayerCharacter Player;
        /// <summary>GameObject của pickup vừa được nhặt — phòng khi effect cần spawn VFX tại đó.</summary>
        public readonly GameObject PickupObject;

        public PickupContext(Vector3 pos, PlayerCharacter player, GameObject pickupGo)
        {
            PickupPosition = pos;
            Player = player;
            PickupObject = pickupGo;
        }
    }
}
