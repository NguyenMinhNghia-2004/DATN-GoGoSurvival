using UnityEngine;
namespace Luzart
{
    /// <summary>
    /// Interface cho t?t c? animation executors
    /// </summary>
    public interface IAnimationExecutor
    {
        void PlayOneShot(IEntity target, System.Action onComplete = null);
        void Play(IEntity target);
        void Stop(IEntity target);
        void Update(float deltaTime);
        void Pause(IEntity target);
        void Resume(IEntity target);
        bool IsPlaying { get; }
        bool IsPaused { get; }
        ETypeAnimation AnimationType { get; }
    }
    /// <summary>
    /// Base class cho animation executors
    /// </summary>
    public abstract class AnimationExecutorBase : IAnimationExecutor
    {
        protected bool isPlaying = false;
        protected bool isPaused = false;
        protected System.Action onCompleteCallback;
        protected IEntity currentTarget;
        public bool IsPlaying => isPlaying;
        public bool IsPaused => isPaused;
        public abstract ETypeAnimation AnimationType { get; }
        public abstract void PlayOneShot(IEntity target, System.Action onComplete = null);
        public abstract void Play(IEntity target);
        public abstract void Stop(IEntity target);
        public virtual void Pause(IEntity target)
        {
            isPaused = true;
        }
        public virtual void Resume(IEntity target)
        {
            isPaused = false;
        }
        protected virtual void OnAnimationComplete()
        {
            isPlaying = false;
            onCompleteCallback?.Invoke();
            onCompleteCallback = null;
        }
        public virtual void Update(float deltaTime)
        {
            if (!isPlaying || isPaused) return;
            DoUpdate(deltaTime);
        }
        protected abstract void DoUpdate(float deltaTime);
    }
}