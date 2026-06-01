using System.Collections.Generic;
using Luzart.Pickups;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Drop SO — **prefab-first + data-driven Strategy**. <see cref="Prefab"/> mang visual +
    /// PickupBase MonoBehaviour. SO này chứa data prefab không tự biết (xp, coin, fx...) qua
    /// list <see cref="Effects"/> — mỗi effect là 1 <see cref="PickupEffect"/> SO áp dụng tuần
    /// tự khi pickup vào người player.
    ///
    /// <para>Cách dùng:
    /// <list type="number">
    /// <item>Tạo asset DropConfig (Right-click → Luzart/Drop/Drop Config).</item>
    /// <item>Drag prefab pickup vào <see cref="Prefab"/>.</item>
    /// <item>Tạo các PickupEffect_* (GrantXP/GrantCoin/MagnetAll/HealHP) → drag vào Effects.</item>
    /// <item>Trong prefab, assign DropConfig này vào PickupBase._data.</item>
    /// </list>
    /// </para>
    ///
    /// <para>Legacy subclass (XPDropConfig/CoinDropConfig/MagnetDropConfig/BombDropConfig/
    /// FoodDropConfig) extend từ class này — 10 .asset Drop_* hiện ref các GUID subclass đó vẫn
    /// hợp lệ + inherit field Effects mới. Có thể migrate dần sang plain DropConfig + effects rồi
    /// xoá subclass sau.</para>
    /// </summary>
    [CreateAssetMenu(fileName = "DropConfig", menuName = "Luzart/Drop/Drop Config", order = 0)]
    public class DropConfig : EntityConfigScriptableObject
    {
        public override string Id => string.IsNullOrEmpty(_id) ? name : _id;

        [Header("Prefab (REQUIRED — drag drop pickup prefab here)")]
        [Tooltip("Drop GameObject prefab. Visual / collider / pickup trigger baked vào prefab; " +
                 "SO này chỉ thêm data values qua Effects list.")]
        [SerializeField] private GameObject prefab;

        [Header("Identity")]
        [SerializeField] private string displayName;
        [TextArea(2, 4)]
        [SerializeField] private string description;

        [Header("Pickup Behavior")]
        [Tooltip("Thời lượng tween InBack bay vào player (giây). 0 = pickup instant không tween.")]
        [SerializeField] private float flyDuration = 0.45f;

        [Tooltip("Các effect chạy tuần tự khi pickup vào player. Drag PickupEffect_* asset vào đây.")]
        [SerializeField] private List<PickupEffect> effects = new List<PickupEffect>();

        // Procedural-path fields kept for legacy `DropEntity` POCO. Not used by the prefab-first
        // path; ignored unless you spawn DropEntity directly (we don't — we Instantiate Prefab).
        [Header("Legacy procedural-path data (only used if you spawn DropEntity instead of Prefab)")]
        [SerializeField] private float colliderRadius = 0.3f;
        [SerializeField] private float pickupRange = 1f;
        [SerializeField] private AnimationConfig animationConfig;
        [SerializeField] private List<Stat> dropValues;

        public GameObject Prefab => prefab;
        public string DisplayName => displayName;
        public string Description => description;
        public float FlyDuration => flyDuration;
        public IReadOnlyList<PickupEffect> Effects => effects;
        public float ColliderRadius => colliderRadius;
        public float PickupRange => pickupRange;
        public AnimationConfig AnimationConfig => animationConfig;
        public List<Stat> DropValues => dropValues;

        /// <summary>
        /// Legacy POCO entity factory. Override trong subclass nếu cần spawn POCO DropEntity
        /// (XPDropEntity/...). Path mặc định trả null — modern path dùng <see cref="Prefab"/>
        /// + PickupBase + Effects.
        /// </summary>
        public virtual DropEntity CreateDrop(IEntity owner) => null;
    }

    public interface IDropHandler
    {
        void HandlePickup(IEntity picker, DropConfig dropConfig);
        bool CanPickup(IEntity picker, DropConfig dropConfig);
    }
}
