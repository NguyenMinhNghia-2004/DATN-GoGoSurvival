using UnityEngine;
namespace Luzart
{
    /// <summary>
    /// Config cho Frame-by-Frame animation
    /// </summary>
    [CreateAssetMenu(fileName = "FBFConfig", menuName = "Luzart/Animation/Frame By Frame Animation Config")]
    public class FrameByFrameAnimationConfig : StateAnimConfig
    {
        [Header("Frame Animation")]
        public Sprite[] frames;
        public float frameRate = 12f;
        public bool pingPong = false;
        public override ETypeAnimation AnimationType => ETypeAnimation.FrameByFrame;
        public override IAnimationExecutor CreateExecutor()
        {
            return new FrameByFrameExecutor(this);
        }
    }
}