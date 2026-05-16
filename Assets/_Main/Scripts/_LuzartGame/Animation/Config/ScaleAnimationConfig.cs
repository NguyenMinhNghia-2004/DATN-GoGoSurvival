using UnityEngine;
namespace Luzart
{
    /// <summary>
    /// Config cho Scale animation
    /// </summary>
    [CreateAssetMenu(fileName = "ScaleAnimationConfig", menuName = "Luzart/Animation/Scale Animation Config")]
    public class ScaleAnimationConfig : StateAnimConfig
    {
        [Header("Scale Animation")]
        public Vector3 scaleFrom = Vector3.one;
        public Vector3 scaleTo = Vector3.one * 1.2f;
        public float timeIn = 0.3f;
        public float timeOut = 0.3f;
        public bool useBaseScale = true; // S? d?ng scale hi?n t?i làm base
        [Header("Timing")]
        public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public override ETypeAnimation AnimationType => ETypeAnimation.Scale;
        public override IAnimationExecutor CreateExecutor()
        {
            return new ScaleExecutor(this);
        }
    }
}