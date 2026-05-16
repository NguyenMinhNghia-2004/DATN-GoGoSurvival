using UnityEngine;
namespace Luzart
{
    /// <summary>
    /// Executor cho Color animation
    /// </summary>
    public class ColorExecutor : AnimationExecutorBase
    {
        private ColorAnimationConfig config;
        private RenderBehavior renderBehavior;
        private Color originalColor;
        private Color fromColor;
        private Color toColor;
        private float currentTime = 0f;
        public override ETypeAnimation AnimationType => ETypeAnimation.Color;
        public ColorExecutor(ColorAnimationConfig config)
        {
            this.config = config;
        }
        public override void PlayOneShot(IEntity target, System.Action onComplete = null)
        {
            currentTarget = target;
            onCompleteCallback = onComplete;
            renderBehavior = target.GetBehavior<RenderBehavior>();
            if (renderBehavior?.Material == null) return;
            // Setup colors - l?y t? MaterialPropertyBlock thay vì material
            originalColor = renderBehavior.Material.color;
            if (config.useCurrentColor)
            {
                fromColor = originalColor;
                toColor = config.colorTo;
            }
            else
            {
                fromColor = config.colorFrom;
                toColor = config.colorTo;
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
            // Restore original color - s? d?ng MaterialPropertyBlock
            if (renderBehavior != null)
            {
                renderBehavior.SetProperty("_Color", originalColor);
            }
        }
        protected override void DoUpdate(float deltaTime)
        {
            if (renderBehavior == null) return;
            currentTime += deltaTime;
            float progress = Mathf.Clamp01(currentTime / config.Duration);
            float curveValue = config.Curve.Evaluate(progress);
            Color currentColor = Color.Lerp(fromColor, toColor, curveValue);
            // ? S?A: S? d?ng MaterialPropertyBlock thay vì set tr?c ti?p vào material
            renderBehavior.SetProperty("_Color", currentColor);
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