using System;
using System.Collections.Generic;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Authored catalog of all 12 weapons (Survivor.io auto-attack arsenal).
    /// Holds presentational data only (icon sprite + display name + description).
    ///
    /// GameObject activation refs (which scene GO to SetActive) intentionally
    /// remain on the legacy <c>SpriteWeapons</c> MonoBehaviour for now — Phase F
    /// replaces both with a per-weapon <c>ZSkillRuntime</c> child of Player.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponCatalog", menuName = "GoGo/Weapon Catalog")]
    public class WeaponCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string id;
            public Sprite sprite;
            public string displayName;
            [TextArea] public string description;
        }

        [SerializeField] private Entry[] _entries;

        public IReadOnlyList<Entry> Entries => _entries;

        public bool TryGet(string id, out Entry entry)
        {
            if (_entries != null)
            {
                foreach (var e in _entries)
                {
                    if (e.id == id) { entry = e; return true; }
                }
            }
            entry = default;
            return false;
        }
    }
}
