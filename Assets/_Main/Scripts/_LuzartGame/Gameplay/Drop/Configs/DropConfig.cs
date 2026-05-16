using System.Collections.Generic;
using UnityEngine;
namespace Luzart
{
    /// <summary>
    /// Drop SO — **prefab-first**. <see cref="Prefab"/> carries the visual + pickup behavior.
    /// This SO only adds data the prefab cannot know on its own (XP amount, special effects).
    ///
    /// Concrete subclasses (XPDropConfig / CoinDropConfig / FoodDropConfig / etc.) add their
    /// own typed data fields. The base class hosts the shared prefab + identity.
    /// </summary>
    public abstract class DropConfig : EntityConfigScriptableObject
    {
        public override string Id => string.IsNullOrEmpty(_id) ? name : _id;

        [Header("Prefab (REQUIRED — drag drop pickup prefab here)")]
        [Tooltip("Drop GameObject prefab. Visual / collider / pickup trigger are baked into the prefab; " +
                 "this SO only adds the data values (xpAmount, coinAmount, etc.).")]
        [SerializeField] private GameObject prefab;

        [Header("Identity")]
        [SerializeField] private string displayName;
        [TextArea(2, 4)]
        [SerializeField] private string description;

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
        public float ColliderRadius => colliderRadius;
        public float PickupRange => pickupRange;
        public AnimationConfig AnimationConfig => animationConfig;
        public List<Stat> DropValues => dropValues;

        public abstract DropEntity CreateDrop(IEntity owner);
    }
    public interface IDropHandler
    {
        void HandlePickup(IEntity picker, DropConfig dropConfig);
        bool CanPickup(IEntity picker, DropConfig dropConfig);
    }
}