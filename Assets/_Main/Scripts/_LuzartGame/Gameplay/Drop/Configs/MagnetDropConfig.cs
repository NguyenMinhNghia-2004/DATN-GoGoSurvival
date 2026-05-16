using UnityEngine;
namespace Luzart
{
    [CreateAssetMenu(fileName = "MagnetDropConfig", menuName = "Luzart/Drop/MagnetDropConfig", order = 3)]
    public class MagnetDropConfig : DropConfig
    {
        [Header("Magnet")]
        [Tooltip("Absorb all Coin/XP drops within this radius.")]
        [SerializeField] private float absorbRadius = 30f;
        public float AbsorbRadius => absorbRadius;

        public override DropEntity CreateDrop(IEntity owner) => new XPDropEntity(null, owner);
    }
}
