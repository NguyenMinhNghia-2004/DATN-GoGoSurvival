using UnityEngine;
namespace Luzart
{
    /// <summary>
    /// Executor cho Scale animation
    /// </summary>
    public class ScaleExecutor : AnimationExecutorBase
    {
        private ScaleAnimationConfig config;
        private ITransform targetTransform;
        private Vector3 originalScale;
        private Vector3 fromScale;
        private Vector3 toScale;
        private float currentTime = 0f;
        private bool isScalingIn = true;
        public override ETypeAnimation AnimationType => ETypeAnimation.Scale;
        public ScaleExecutor(ScaleAnimationConfig config)
        {
            this.config = config;
        }
        public override void PlayOneShot(IEntity target, System.Action onComplete = null)
        {
            currentTarget = target;
            onCompleteCallback = onComplete;
            targetTransform = target.Transform;
            if (targetTransform == null) return;
            // Setup scales
            originalScale = targetTransform.Scale.Value;
            if (config.useBaseScale)
            {
                fromScale = originalScale;
                toScale = Vector3.Scale(originalScale, config.scaleTo);
            }
            else
            {
                fromScale = config.scaleFrom;
                toScale = config.scaleTo;
            }
            currentTime = 0f;
            isScalingIn = true;
            isPlaying = true;
            isPaused = false;
        }
        public override void Play(IEntity target)
        {
            PlayOneShot(target, null);
        }
        public override void Stop(IEntity target)
        {
            isPlaying = false;
            isPaused = false;
            // Restore original scale
            if (targetTransform != null)
            {
                targetTransform.SetScale(originalScale);
            }
        }
        protected override void DoUpdate(float deltaTime)
        {
            if (targetTransform == null) return;
            currentTime += deltaTime;
            if (isScalingIn)
            {
                // Scale in phase
                float progress = Mathf.Clamp01(currentTime / config.timeIn);
                float curveValue = config.curve.Evaluate(progress);
                Vector3 currentScale = Vector3.Lerp(fromScale, toScale, curveValue);
                targetTransform.SetScale(currentScale);
                if (progress >= 1f)
                {
                    if (config.loop)
                    {
                        // Switch to scale out
                        isScalingIn = false;
                        currentTime = 0f;
                    }
                    else
                    {
                        OnAnimationComplete();
                    }
                }
            }
            else
            {
                // Scale out phase
                float progress = Mathf.Clamp01(currentTime / config.timeOut);
                float curveValue = config.curve.Evaluate(progress);
                Vector3 currentScale = Vector3.Lerp(toScale, fromScale, curveValue);
                targetTransform.SetScale(currentScale);
                if (progress >= 1f)
                {
                    if (config.loop)
                    {
                        // Switch back to scale in
                        isScalingIn = true;
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
}