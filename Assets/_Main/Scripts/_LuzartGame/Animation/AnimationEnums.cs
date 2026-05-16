using UnityEngine;
namespace Luzart
{
    /// <summary>
    /// Animation state enum - ??nh ngh?a các tr?ng thái animation
    /// </summary>
    public enum EAnimation
    {
        Idle,
        Walk, 
        Attack,
        Death,
        Hit,
        Jump,
        Cast,
        Victory
    }
    /// <summary>
    /// Animation type enum - ??nh ngh?a lo?i animation
    /// </summary>
    public enum ETypeAnimation
    {
        FrameByFrame,   // Sprite animation
        Scale,          // Scale tween animation
        Rotation,       // Rotation tween animation
        Color,          // Color tween animation
        Position        // Position tween animation
    }
}