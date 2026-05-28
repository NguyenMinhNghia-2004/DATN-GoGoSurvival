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
        // Phase D-F: flags appear here as their slices begin.
    }
}
