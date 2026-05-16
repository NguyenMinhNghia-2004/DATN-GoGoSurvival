using UnityEngine;
namespace Luzart
{
    /// <summary>
    /// Executor cho Frame-by-Frame animation
    /// </summary>
    public class FrameByFrameExecutor : AnimationExecutorBase
    {
        private FrameByFrameAnimationConfig config;
        private RenderBehavior renderBehavior;
        private Material originalMaterial;
        private MaterialPropertyBlock propertyBlock; // Thêm MaterialPropertyBlock
        private int currentFrameIndex = 0;
        private float frameTimer = 0f;
        private bool isReversing = false; // For ping-pong
        public override ETypeAnimation AnimationType => ETypeAnimation.FrameByFrame;
        public FrameByFrameExecutor(FrameByFrameAnimationConfig config)
        {
            this.config = config;
            this.propertyBlock = new MaterialPropertyBlock(); // Khởi tạo PropertyBlock
        }
        public override void PlayOneShot(IEntity target, System.Action onComplete = null)
        {
            if (config.frames == null || config.frames.Length == 0) return;
            currentTarget = target;
            onCompleteCallback = onComplete;
            renderBehavior = target.GetBehavior<RenderBehavior>();
            if (renderBehavior?.Material != null)
            {
                originalMaterial = renderBehavior.Material;
            }
            currentFrameIndex = 0;
            frameTimer = 0f;
            isReversing = false;
            isPlaying = true;
            isPaused = false;
            UpdateFrame();
        }
        public override void Play(IEntity target)
        {
            PlayOneShot(target, null);
        }
        public override void Stop(IEntity target)
        {
            isPlaying = false;
            isPaused = false;
            // Restore original material if needed
            if (renderBehavior != null && originalMaterial != null)
            {
                renderBehavior.SetMaterial(originalMaterial);
            }
        }
        protected override void DoUpdate(float deltaTime)
        {
            if (config.frames == null || config.frames.Length == 0) return;
            frameTimer += deltaTime;
            float frameDuration = 1f / config.frameRate;
            if (frameTimer >= frameDuration)
            {
                frameTimer = 0f;
                AdvanceFrame();
            }
        }
        private void AdvanceFrame()
        {
            if (config.pingPong)
            {
                if (!isReversing)
                {
                    currentFrameIndex++;
                    if (currentFrameIndex >= config.frames.Length)
                    {
                        currentFrameIndex = config.frames.Length - 2;
                        isReversing = true;
                        // Don't complete immediately for ping-pong, let it reverse first
                        // Only complete when back to start if not looping
                    }
                }
                else
                {
                    currentFrameIndex--;
                    if (currentFrameIndex < 0)
                    {
                        if (config.loop)
                        {
                            currentFrameIndex = 1;
                            isReversing = false;
                        }
                        else
                        {
                            // For non-looping ping-pong, complete when back to start
                            currentFrameIndex = 0;
                            OnAnimationComplete();
                            return;
                        }
                    }
                }
            }
            else
            {
                currentFrameIndex++;
                if (currentFrameIndex >= config.frames.Length)
                {
                    if (config.loop)
                    {
                        currentFrameIndex = 0;
                    }
                    else
                    {
                        currentFrameIndex = config.frames.Length - 1;
                        OnAnimationComplete();
                        return;
                    }
                }
            }
            UpdateFrame();
        }
        private void UpdateFrame()
        {
            if (renderBehavior?.Material != null && 
                currentFrameIndex < config.frames.Length && 
                config.frames[currentFrameIndex] != null)
            {
                var sprite = config.frames[currentFrameIndex];
                var mat = renderBehavior.Material;
                mat.mainTexture = sprite.texture;
                Rect r = sprite.textureRect;
                Vector2 offset = new Vector2(r.x / sprite.texture.width, r.y / sprite.texture.height);
                Vector2 scale = new Vector2(r.width / sprite.texture.width, r.height / sprite.texture.height);
                if (scale == Vector2.zero)
                {
                    offset = Vector2.zero;
                    scale = Vector2.one;
                }
                renderBehavior.SetProperty("_MainTexOffset", offset);
                renderBehavior.SetProperty("_MainTexScale", scale);
            }
        }
    }
}