using UnityEngine;
using System.Collections.Generic;
using System.Linq;
namespace Luzart
{
    /// <summary>
    /// ScriptableObject ch?a t?t c? animation configs cho m?t Entity
    /// </summary>
    [CreateAssetMenu(fileName = "AnimationConfig", menuName = "Luzart/Animation/Animation Config")]
    public class AnimationConfig : ScriptableObject
    {
        [Header("Animation Settings")]
        [SerializeField] private List<StateAnimConfig> stateAnimations = new();
        [Header("Global Settings")]
        public bool autoPlayOnStart = true;
        public EAnimation defaultAnimation = EAnimation.Idle;
        public float globalTimeScale = 1f;
        // Runtime cache
        private Dictionary<EAnimation, List<StateAnimConfig>> animationLookup;
        private bool isLookupBuilt = false;
        private void OnEnable()
        {
            BuildLookup();
        }
        private void OnValidate()
        {
            isLookupBuilt = false;
        }
        /// <summary>
        /// Build lookup dictionary for fast access
        /// </summary>
        private void BuildLookup()
        {
            if (isLookupBuilt) return;
            animationLookup = new Dictionary<EAnimation, List<StateAnimConfig>>();
            foreach (var config in stateAnimations)
            {
                if (config == null) continue;
                if (!animationLookup.ContainsKey(config.animationState))
                {
                    animationLookup[config.animationState] = new List<StateAnimConfig>();
                }
                animationLookup[config.animationState].Add(config);
            }
            isLookupBuilt = true;
        }
        /// <summary>
        /// Get all animation configs for a specific state
        /// </summary>
        public List<StateAnimConfig> GetAnimations(EAnimation state)
        {
            BuildLookup();
            if (animationLookup.TryGetValue(state, out var configs))
            {
                return configs;
            }
            return new List<StateAnimConfig>();
        }
        /// <summary>
        /// Get specific animation by state and type
        /// </summary>
        public StateAnimConfig GetAnimation(EAnimation state, ETypeAnimation type)
        {
            var animations = GetAnimations(state);
            return animations.FirstOrDefault(a => a.AnimationType == type);
        }
        /// <summary>
        /// Get all frame-by-frame animations for a state
        /// </summary>
        public List<FrameByFrameAnimationConfig> GetFrameAnimations(EAnimation state)
        {
            var animations = GetAnimations(state);
            return animations.OfType<FrameByFrameAnimationConfig>().ToList();
        }
        /// <summary>
        /// Get all tween animations for a state (Scale, Rotation, Color, Position)
        /// </summary>
        public List<StateAnimConfig> GetTweenAnimations(EAnimation state)
        {
            var animations = GetAnimations(state);
            return animations.Where(a => a.AnimationType != ETypeAnimation.FrameByFrame).ToList();
        }
        /// <summary>
        /// Check if has any animation for state
        /// </summary>
        public bool HasAnimation(EAnimation state)
        {
            return GetAnimations(state).Count > 0;
        }
        /// <summary>
        /// Check if has specific animation type for state
        /// </summary>
        public bool HasAnimation(EAnimation state, ETypeAnimation type)
        {
            return GetAnimation(state, type) != null;
        }
        /// <summary>
        /// Add animation config at runtime
        /// </summary>
        public void AddAnimation(StateAnimConfig config)
        {
            if (config == null) return;
            stateAnimations.Add(config);
            isLookupBuilt = false;
        }
        /// <summary>
        /// Remove animation config
        /// </summary>
        public bool RemoveAnimation(StateAnimConfig config)
        {
            bool removed = stateAnimations.Remove(config);
            if (removed)
            {
                isLookupBuilt = false;
            }
            return removed;
        }
        /// <summary>
        /// Get all available animation states
        /// </summary>
        public List<EAnimation> GetAvailableStates()
        {
            BuildLookup();
            return animationLookup.Keys.ToList();
        }
    }
}