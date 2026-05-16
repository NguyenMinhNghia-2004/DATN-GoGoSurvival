using UnityEngine;
namespace Luzart
{
    /// <summary>
    /// Config cho Color animation
    /// </summary>
    [CreateAssetMenu(fileName = "ColorAnimationConfig", menuName = "Luzart/Animation/Color Animation Config")]
    public class ColorAnimationConfig : StateAnimConfig
    {
        public override ETypeAnimation AnimationType => ETypeAnimation.Color;
        [Header("Color Animation")]
        public Color colorFrom = Color.white;
        public Color colorTo = Color.red;
        public bool useCurrentColor = true;
        [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public AnimationCurve Curve => curve;
        [SerializeField] private float duration = 1f;
        public float Duration => duration;
        public override IAnimationExecutor CreateExecutor()
        {
            return new ColorExecutor(this);
        }
    }
}