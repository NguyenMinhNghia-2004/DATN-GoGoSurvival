using UnityEngine;
namespace Luzart
{
    [CreateAssetMenu(fileName = "FoodDropConfig", menuName = "Luzart/Drop/FoodDropConfig", order = 4)]
    public class FoodDropConfig : DropConfig
    {
        [Header("Food / Heal")]
        [Tooltip("Heal as percentage of MaxHP (0.20 = +20%).")]
        [SerializeField] private float healPercent = 0.20f;
        public float HealPercent => healPercent;

        public override DropEntity CreateDrop(IEntity owner) => new XPDropEntity(null, owner);
    }
}
