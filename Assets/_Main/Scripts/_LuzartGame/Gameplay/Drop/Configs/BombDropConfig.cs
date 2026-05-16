using UnityEngine;
namespace Luzart
{
    [CreateAssetMenu(fileName = "BombDropConfig", menuName = "Luzart/Drop/BombDropConfig", order = 5)]
    public class BombDropConfig : DropConfig
    {
        [Header("Bomb")]
        [SerializeField] private float damage = 200f;
        [SerializeField] private float radius = 8f;
        public float Damage => damage;
        public float Radius => radius;

        public override DropEntity CreateDrop(IEntity owner) => new XPDropEntity(null, owner);
    }
}
