using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Bridges a Unity GameObject (a projectile visual prefab) to a plain-class
    /// <see cref="ProjectileEntity"/>. Two responsibilities:
    /// <list type="bullet">
    /// <item><b>Transform sync</b> — every LateUpdate, copy entity Transform → GameObject
    ///   transform so the prefab visually follows wherever MoveProjectileBehavior moved it.</item>
    /// <item><b>Collision bridge</b> — on <c>OnTriggerEnter2D</c>, look up the hit
    ///   Collider2D's <see cref="EntityRef"/>, decide if it's a valid target by owner-faction
    ///   rule, then forward to <see cref="ProjectileEntity.OnCollision"/> which applies ATK
    ///   damage and kills the projectile.</item>
    /// </list>
    ///
    /// <para>This replaces the framework's `ProjectileCollisionHandlerBehavior` for the
    /// Pf_*.prefab visual path. The legacy Bullet1.prefab carried a MonoBehaviour with the
    /// same job (script guid 7f3b0495...) but that script was deleted in the W-nuke — this
    /// is the Luzart-native replacement.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProjectileVisualBinder : MonoBehaviour
    {
        private ProjectileEntity _entity;
        private Transform _t;

        public void Bind(IEntity entity)
        {
            _entity = entity as ProjectileEntity;
            _t = transform;
            if (entity != null && entity.Transform != null) ApplyTransform();
        }

        private void LateUpdate()
        {
            if (_entity == null || _entity.Transform == null) return;
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

        // ───── Unity Physics2D collision bridge ─────
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_entity == null || _entity.IsDead || other == null) return;

            // Get the entity behind the hit collider.
            var er = other.GetComponent<EntityRef>();
            if (er == null || er.Entity == null) return;

            // Faction rule: projectile owned by a PlayerCharacter only damages EnemyCharacter
            // (and vice-versa). Skip same-faction hits + self.
            var owner = _entity.Owner;
            bool ownerIsPlayer = owner is PlayerCharacter;
            bool targetIsEnemy = er.Entity is EnemyCharacter;
            bool targetIsPlayer = er.Entity is PlayerCharacter;
            bool valid = (ownerIsPlayer && targetIsEnemy) || (!ownerIsPlayer && targetIsPlayer);
            if (!valid) return;

            // ProjectileEntity.OnCollision applies StatType.ATK damage + sets IsDead.
            // The IsDead check above will then destroy this GameObject on next LateUpdate.
            _entity.OnCollision(er.Entity);

            // Also call the legacy LuzartEnemyEntityRoot.TakeDamage path so the visual
            // Zombie GameObject's HP bar / death sequence runs (since LuzartEnemyCharacter
            // skips StatsBehavior setup → CharacterBase.TakeDamage path is inert here).
            if (targetIsEnemy)
            {
                double atk = _entity.GetStat(StatType.ATK);
                var enemyRoot = other.GetComponentInParent<LuzartEnemyEntityRoot>();
                if (enemyRoot != null) enemyRoot.TakeDamage((float)atk);
            }
        }

        private void OnDestroy()
        {
            _entity = null;
        }
    }
}
