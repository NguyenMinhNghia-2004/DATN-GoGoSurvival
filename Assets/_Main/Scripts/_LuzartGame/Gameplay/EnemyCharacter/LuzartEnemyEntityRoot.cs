using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Phase F foundation scaffold (Phase F.6). Replaces the legacy
    /// <c>EnemyManager</c> MonoBehaviour + <c>DATNEnemyEntityAdapter</c>
    /// bridge on the Zombie prefab.
    ///
    /// <para>When activated, on Awake:
    /// <list type="number">
    /// <item>Create <see cref="EnemyCharacter"/> from <see cref="EnemyDefinition"/>.</item>
    /// <item>Register with framework <c>EntityManager</c>.</item>
    /// <item>Attach framework behaviors (AI, animation, collision, drop).</item>
    /// </list>
    /// On <c>Stats.Runtime_HP &lt;= 0</c>: trigger drop spawn + <c>Destroy(gameObject)</c>.</para>
    ///
    /// <para>Dormant until <c>MigrationFlags.UseLuzartEnemyEntityRoot</c> is true.</para>
    /// </summary>
    public class LuzartEnemyEntityRoot : MonoBehaviour
    {
        [SerializeField] private EnemyDefinition _enemyDefinition;

        public EnemyDefinition Definition => _enemyDefinition;

        // Phase F.x will add Awake/OnDestroy/Update wiring here.
    }
}
