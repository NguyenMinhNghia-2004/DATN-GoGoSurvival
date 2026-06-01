using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Bridge between a Unity GameObject (with Collider2D) and the Luzart plain-class
    /// <see cref="IEntity"/> it represents. Attached automatically when an entity is
    /// spawned (player root, enemy root, projectile). Lets <see cref="TargetProvider"/>
    /// query the scene via Unity Physics2D and translate the hit Collider2D back to
    /// the framework entity that owns it.
    ///
    /// <para>Usage:</para>
    /// <list type="bullet">
    /// <item>On spawn: <c>go.AddComponent&lt;EntityRef&gt;().Entity = myIEntity;</c></item>
    /// <item>On query: <c>collider2D.GetComponent&lt;EntityRef&gt;()?.Entity</c></item>
    /// </list>
    ///
    /// <para>Replaces the older CollisionManager / SpatialHash / Mapping registration
    /// pipeline — Unity's broadphase is already running, this just lets us pull the
    /// framework entity off the colliders we hit.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EntityRef : MonoBehaviour
    {
        public IEntity Entity;
    }
}
