using UnityEngine;
using System.Collections.Generic;
using System.Linq;
namespace Luzart
{
    /// <summary>
    /// Đơn giản hơn, modular và dễ mở rộng
    /// </summary>
    public class AnimationBehavior : BehaviorBase
    {
        // Config (được set từ bên ngoài)
        private AnimationConfig animationConfig;
        // Runtime state
        private EAnimation currentState = EAnimation.Idle;
        private Dictionary<EAnimation, List<IAnimationExecutor>> stateExecutors = new();
        private List<IAnimationExecutor> currentExecutors = new();
        // Dependencies
        private MoveBehavior moveBehavior;
        private StatsBehavior statsBehavior;
        // Movement tracking
        private Vector3 lastPosition;
        private bool isMoving = false;
        private float movementThreshold = 0.01f;
        private bool animateOnMovement = true;
        // Settings
        private bool autoStart = true;
        private float timeScale = 1f;
        public AnimationBehavior(IEntity owner) : base(owner)
        {
        }
        protected override void DoStart()
        {
            // Get dependencies
            moveBehavior = Owner.GetBehavior<MoveBehavior>();
            statsBehavior = Owner.GetBehavior<StatsBehavior>();
            if (Owner.Transform != null)
            {
                lastPosition = Owner.Transform.Position.Value;
            }
            // Setup executors từ config
            SetupExecutors();
            // Auto start nếu enabled
            if (autoStart && animationConfig != null)
            {
                PlayAnimation(animationConfig.defaultAnimation);
            }
        }
        protected override void DoUpdate(float dt)
        {
            // Apply time scale
            float scaledDt = dt * timeScale;
            // Update movement detection
            UpdateMovementDetection();
            // Update animation state
            UpdateAnimationState();
            // Update all active executors
            foreach (var executor in currentExecutors)
            {
                executor.Update(scaledDt);
            }
        }
        private void SetupExecutors()
        {
            if (animationConfig == null) return;
            stateExecutors.Clear();
            // Create executors cho tất cả animation states
            var availableStates = animationConfig.GetAvailableStates();
            foreach (var state in availableStates)
            {
                var configs = animationConfig.GetAnimations(state);
                var executors = new List<IAnimationExecutor>();
                foreach (var config in configs)
                {
                    var executor = config.CreateExecutor();
                    if (executor != null)
                    {
                        executors.Add(executor);
                    }
                }
                stateExecutors[state] = executors;
            }
        }
        private void UpdateMovementDetection()
        {
            if (!animateOnMovement || Owner.Transform == null) return;
            Vector3 currentPosition = Owner.Transform.Position.Value;
            float movementDistance = Vector3.Distance(currentPosition, lastPosition);
            isMoving = movementDistance > movementThreshold;
            lastPosition = currentPosition;
        }
        private void UpdateAnimationState()
        {
            EAnimation targetState = DetermineTargetState();
            if (targetState != currentState)
            {
                PlayAnimation(targetState);
            }
        }
        private EAnimation DetermineTargetState()
        {
            if (statsBehavior != null && statsBehavior.IsDead)
            {
                return EAnimation.Death;
            }
            if (animateOnMovement && isMoving)
            {
                return EAnimation.Walk;
            }
            return EAnimation.Idle;
        }
        #region Public API
        /// <summary>
        /// Set animation config từ bên ngoài
        /// </summary>
        public void Configure(AnimationConfig config)
        {
            animationConfig = config;
            if (config != null)
            {
                timeScale = config.globalTimeScale;
                autoStart = config.autoPlayOnStart;
            }
            SetupExecutors();
        }
        /// <summary>
        /// Play animation by state
        /// </summary>
        public void PlayAnimation(EAnimation state)
        {
            if (!stateExecutors.TryGetValue(state, out var executors) || executors.Count == 0)
            {
                return;
            }
            // Stop current animations
            StopAllAnimations();
            // Start new animations
            currentState = state;
            currentExecutors = executors;
            foreach (var executor in executors)
            {
                executor.PlayOneShot(Owner);
            }
        }
        /// <summary>
        /// Play one-shot animation
        /// </summary>
        public void PlayOneShotAnimation(EAnimation state, System.Action onComplete = null)
        {
            if (!stateExecutors.TryGetValue(state, out var executors) || executors.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }
            // Stop current animations
            StopAllAnimations();
            // Play one-shot
            currentState = state;
            currentExecutors = executors;
            // Track completion (chỉ complete khi tất cả executors complete)
            int completedCount = 0;
            int totalCount = executors.Count;
            foreach (var executor in executors)
            {
                executor.PlayOneShot(Owner, () =>
                {
                    completedCount++;
                    if (completedCount >= totalCount)
                    {
                        onComplete?.Invoke();
                    }
                });
            }
        }
        /// <summary>
        /// Stop all animations
        /// </summary>
        public void StopAllAnimations()
        {
            foreach (var executor in currentExecutors)
            {
                executor.Stop(Owner);
            }
            currentExecutors.Clear();
        }
        /// <summary>
        /// Pause all animations
        /// </summary>
        public void PauseAnimations()
        {
            foreach (var executor in currentExecutors)
            {
                executor.Pause(Owner);
            }
        }
        /// <summary>
        /// Resume all animations
        /// </summary>
        public void ResumeAnimations()
        {
            foreach (var executor in currentExecutors)
            {
                executor.Resume(Owner);
            }
        }
        /// <summary>
        /// Check if có animation nào đang chạy
        /// </summary>
        public bool IsPlaying()
        {
            return currentExecutors.Any(e => e.IsPlaying);
        }
        /// <summary>
        /// Check if animation state có sẵn
        /// </summary>
        public bool HasAnimation(EAnimation state)
        {
            return animationConfig?.HasAnimation(state) ?? false;
        }
        /// <summary>
        /// Set time scale cho animations
        /// </summary>
        public void SetTimeScale(float scale)
        {
            timeScale = scale;
        }
        /// <summary>
        /// Configure movement animation settings
        /// </summary>
        public void ConfigureMovementAnimation(bool enable, float threshold = 0.01f)
        {
            animateOnMovement = enable;
            movementThreshold = threshold;
        }
        /// <summary>
        /// Get current animation state
        /// </summary>
        public EAnimation GetCurrentState()
        {
            return currentState;
        }
        #endregion
        protected override void DoDestroy()
        {
            StopAllAnimations();
            stateExecutors.Clear();
        }
    }
}