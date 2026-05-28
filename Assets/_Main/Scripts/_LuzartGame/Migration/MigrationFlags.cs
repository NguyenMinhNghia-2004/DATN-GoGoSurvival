using UnityEngine;

namespace Luzart.Migration
{
    /// <summary>
    /// Runtime feature flags for the strangler-fig migration from DATN legacy
    /// gameplay to the Luzart framework.
    ///
    /// Authored as a ScriptableObject so the Inspector can toggle flags live
    /// during Play mode without recompiling. Each flag tracks one slice in
    /// docs/superpowers/specs/. Remove a flag once its slice is complete and
    /// the legacy code path is deleted.
    /// </summary>
    [CreateAssetMenu(fileName = "MigrationFlags", menuName = "GoGo/Migration Flags")]
    public class MigrationFlags : ScriptableObject
    {
        // Phase C: no flags needed (no behaviour change).
        // Phase D: data extraction only, no flags needed.

        [Header("Phase F — Gameplay rewrite")]

        [Tooltip("Phase F.x — Switch player movement + input to LuzartPlayerController. " +
                 "When false, legacy PlayerManager + JoystickManager drive movement.")]
        public bool UseLuzartPlayerController = false;

        [Tooltip("Phase F.x — Activate LuzartPlayerEntityRoot (replaces DATNPlayerEntityAdapter " +
                 "with a real PlayerCharacter created from StatsConfig).")]
        public bool UseLuzartPlayerEntityRoot = false;

        [Tooltip("Phase F.x — Switch Zombie prefab to LuzartEnemyEntityRoot (replaces legacy " +
                 "EnemyManager + DATNEnemyEntityAdapter).")]
        public bool UseLuzartEnemyEntityRoot = false;

        [Tooltip("Phase F.x — Framework StatsBehavior owns HP (reverse DATNGameplayBridge). " +
                 "Cutover flag: when true, GameManager.Health becomes a read-only mirror.")]
        public bool FrameworkOwnsPlayerHP = false;
    }
}
