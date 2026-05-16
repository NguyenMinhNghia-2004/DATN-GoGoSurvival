using UnityEngine;
namespace Luzart
{
    public class FXEntityConfig : AbstractScriptableContent
    {
        [SerializeField] private Material material;
        [SerializeField] private AnimationConfig animationConfig;
        [Range(0.1f, 2f)]
        [SerializeField] private float scale = 0.4f;
        [SerializeField] private float duration = 1f;
        public Material Material => material;
        public AnimationConfig AnimationConfig => animationConfig;
        public float Scale => scale;
        public float Duration => duration;
    }
}