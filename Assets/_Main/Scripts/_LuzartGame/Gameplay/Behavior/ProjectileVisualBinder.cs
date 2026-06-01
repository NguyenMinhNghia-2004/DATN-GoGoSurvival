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
    /// <item>Attach by <see cref="ProjectileEntity"/> right after Instantiate.</item>
    /// <item>Self-destroys (GameObject.Destroy) when its bound entity dies.</item>
    /// </list>
    ///
    /// <para>Why this exists: the Luzart framework's <see cref="RenderBehavior"/> uses
    /// a centralized RenderingManager + batch-rendering with Material/Sprite — which
    /// is invisible when the material isn't wired. Prefab-based visuals are simpler:
    /// the prefab already has a SpriteRenderer / Animator / particles authored, and
    /// this binder just keeps its transform in sync with the entity.</para>
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
            if (_entity?.OnDead != null)
            {
                _entity.OnDead -= OnEntityDead;
            }
            if (_entity != null)
            {
                _entity.OnDead += OnEntityDead;
                // Initial sync — avoid a one-frame lag where the prefab renders at
                // Instantiate position before LateUpdate runs.
                if (_entity.Transform != null) ApplyTransform();
            }
        }

        private void OnEntityDead(IEntity _)
        {
            if (this == null) return;
            if (gameObject != null) Destroy(gameObject);
        }

        private void LateUpdate()
        {
            if (_entity == null || _entity.Transform == null) return;
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
            if (_entity != null) _entity.OnDead -= OnEntityDead;
            _entity = null;
        }
    }
}
