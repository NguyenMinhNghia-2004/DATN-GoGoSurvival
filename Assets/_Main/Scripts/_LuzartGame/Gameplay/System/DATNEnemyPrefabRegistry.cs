using System.Collections.Generic;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Maps a framework <see cref="EnemyDefinition"/> SO → an actual DATN GameObject prefab
    /// (the one with `EnemyManager`, Animator, SpriteRenderer, Collider2D — i.e. the legacy
    /// visual + HP pipeline). Used by <see cref="EnemySpawnerManager"/> to resolve which
    /// prefab to Instantiate for a given EnemyWaveConfig.
    ///
    /// Create asset: Right-click → Create → Luzart/Enemy/DATN Enemy Prefab Registry.
    /// Fill <see cref="entries"/> in Inspector: one row per EnemyDefinition with the
    /// matching DATN prefab dropped in.
    /// </summary>
    [CreateAssetMenu(fileName = "DATNEnemyPrefabRegistry",
                     menuName = "Luzart/Enemy/DATN Enemy Prefab Registry")]
    public class DATNEnemyPrefabRegistry : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            [Tooltip("Framework EnemyDefinition (what data drives this enemy).")]
            public EnemyDefinition definition;

            [Tooltip("DATN scene prefab to Instantiate when this definition is requested.")]
            public GameObject datnPrefab;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        [Tooltip("Fallback prefab if a definition has no registered prefab.")]
        [SerializeField] private GameObject defaultPrefab;

        private Dictionary<EnemyDefinition, GameObject> _lookup;

        public GameObject ResolvePrefab(EnemyDefinition def)
        {
            if (_lookup == null) BuildLookup();
            if (def != null && _lookup.TryGetValue(def, out var prefab) && prefab != null) return prefab;
            return defaultPrefab;
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<EnemyDefinition, GameObject>();
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null || e.definition == null || e.datnPrefab == null) continue;
                _lookup[e.definition] = e.datnPrefab;
            }
        }

#if UNITY_EDITOR
        private void OnValidate() { _lookup = null; }
#endif
    }
}
