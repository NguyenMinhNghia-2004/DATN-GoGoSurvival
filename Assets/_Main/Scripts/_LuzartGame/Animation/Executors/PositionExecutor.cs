using UnityEngine;
namespace Luzart
{
    /// <summary>
    /// Executor cho Position animation
    /// </summary>
    public class PositionExecutor : AnimationExecutorBase
    {
        private PositionAnimationConfig config;
        private ITransform targetTransform;
        private Vector3 originalPosition;
        private Vector3 fromPosition;
        private Vector3 toPosition;
        private float currentTime = 0f;
        public override ETypeAnimation AnimationType => ETypeAnimation.Position;
        public PositionExecutor(PositionAnimationConfig config)
        {
            this.config = config;
        }
        public override void PlayOneShot(IEntity target, System.Action onComplete = null)
        {
            currentTarget = target;
            onCompleteCallback = onComplete;
            targetTransform = target.Transform;
            if (targetTransform == null) return;
            // Setup positions
            originalPosition = targetTransform.Position.Value;
            if (config.relative)
            {
                fromPosition = originalPosition;
                toPosition = originalPosition + config.positionOffset;
            }
            else
            {
                fromPosition = originalPosition;
                toPosition = config.absolutePosition;
            }
            currentTime = 0f;
            isPlaying = true;
            isPaused = false;
        }
        public override void Play(IEntity target)
        {
            PlayOneShot(target, () =>
            {
                if (config.loop && isPlaying)
                {
                    // Restart animation
                    currentTime = 0f;
                }
            });
        }
        public override void Stop(IEntity target)
        {
            isPlaying = false;
            isPaused = false;
            // Restore original position
            if (targetTransform != null)
            {
                targetTransform.SetPosition(originalPosition);
            }
        }
        protected override void DoUpdate(float deltaTime)
        {
            if (targetTransform == null) return;
            currentTime += deltaTime;
            float progress = Mathf.Clamp01(currentTime / config.Duration);
            float curveValue = config.Curve.Evaluate(progress);
            Vector3 currentPosition = Vector3.Lerp(fromPosition, toPosition, curveValue);
            targetTransform.SetPosition(currentPosition);
            if (progress >= 1f)
            {
                if (config.loop)
                {
                    // Restart animation
                    currentTime = 0f;
                }
                else
                {
                    OnAnimationComplete();
                }
            }
        }
    }
}