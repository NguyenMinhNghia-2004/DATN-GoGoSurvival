using UnityEngine;
using System;
namespace Luzart
{
    public interface IAnimationExecutorProvider
    {
        IAnimationExecutor CreateExecutor();
    }
    /// <summary>
    /// Interface cho animations cần timing và curve
    /// </summary>
    public interface ITweenAnimationConfig
    {
        float Duration { get; }
        AnimationCurve Curve { get; }
    }
    /// <summary>
    /// Base class cho state animation config
    /// </summary>
    [System.Serializable]
    public abstract class StateAnimConfig : ScriptableObject
    {
        [Header("Base Settings")]
        public EAnimation animationState;
        public abstract ETypeAnimation AnimationType { get; }
        public bool loop = true;
        [Header("Timing")]
        public float delay = 0f;
        /// <summary>
        /// Tạo executor tương ứng với loại animation
        /// </summary>
        public virtual IAnimationExecutor CreateExecutor()
        {
            return null;
        }
    }
}