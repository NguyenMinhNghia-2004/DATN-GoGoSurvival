using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Bridges a Unity GameObject (a projectile visual prefab) to a plain-class
    /// <see cref="ProjectileEntity"/>. Each LateUpdate, syncs this GameObject's
    /// transform from the entity's <see cref="ITransform"/> so the prefab visually
    /// follows wherever <c>MoveProjectileBehavior</c> moved the entity.
    ///
    /// <para>Lifecycle:</para>
    /// <list type="bullet">
    /// <item>Attached on the Pf_*.prefab so it's visible in the Inspector.</item>
    /// <item><see cref="Bind"/> is called by <see cref="ProjectileEntity.Initialize"/>
    ///   right after Instantiate to wire up the entity.</item>
    /// <item>Self-destroys (GameObject.Destroy) when its bound entity reports
    ///   <see cref="IEntity.IsDead"/> (polled in LateUpdate — IEntity doesn't expose
    ///   the OnDead event publicly, so we poll instead of subscribing).</item>
    /// </list>
    ///
    /// <para>Why this exists: prefab-based visuals are simpler than the framework's
    /// RenderingManager batch-rendering — the prefab already has SpriteRenderer /
    /// Animator / particles authored. This binder just keeps the prefab transform
    /// in sync with the entity.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProjectileVisualBinder : MonoBehaviour
    {
        private IEntity _entity;
        private Transform _t;

        public void Bind(IEntity entity)
        {
            _entity = entity;
            _t = transform;
            // Initial sync — avoid a one-frame lag where the prefab renders at the
            // Instantiate position before LateUpdate runs.
            if (_entity != null && _entity.Transform != null) ApplyTransform();
        }

        private void LateUpdate()
        {
            if (_entity == null || _entity.Transform == null) return;
            // Polled teardown — IEntity exposes IsDead but not OnDead, so we check
            // it here every frame. Cheap (just a bool read) and keeps the binder
            // independent of EntityBase's concrete event field.
            if (_entity.IsDead)
            {
                _entity = null;
                Destroy(gameObject);
                return;
            }
            ApplyTransform();
        }

        private void ApplyTransform()
        {
            Vector2 p = _entity.Transform.Position.Value;
            _t.position = new Vector3(p.x, p.y, _t.position.z);
            _t.rotation = _entity.Transform.Rotation.Value;
            _t.localScale = _entity.Transform.Scale.Value;
        }

        private void OnDestroy()
        {
            _entity = null;
        }
    }
}
