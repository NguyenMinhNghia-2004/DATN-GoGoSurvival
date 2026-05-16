using UnityEngine;
namespace Luzart
{
    /// <summary>
    /// Config cho Position animation
    /// </summary>
    [CreateAssetMenu(fileName = "PositionAnimationConfig", menuName = "Luzart/Animation/Position Animation Config")]
    public class PositionAnimationConfig : StateAnimConfig
    {
        [Header("Position Animation")]
        public Vector3 positionOffset = Vector3.up;
        public bool relative = true; // Di chuyển relative từ vị trí hiện tại
        public Vector3 absolutePosition = Vector3.zero; // Nếu không relative
        [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public AnimationCurve Curve => curve;
        [SerializeField] private float duration = 1f;
        public float Duration => duration;
        public override ETypeAnimation AnimationType => ETypeAnimation.Position;
        public override IAnimationExecutor CreateExecutor()
        {
            return new PositionExecutor(this);
        }
    }
}