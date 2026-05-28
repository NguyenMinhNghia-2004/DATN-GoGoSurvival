using System.Collections.Generic;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Catalog of level prefabs that can be instantiated as the playfield.
    /// Replaces the legacy <c>LevelsManager</c> MonoBehaviour which only
    /// existed to hold these refs in the Inspector.
    ///
    /// The legacy <c>DATN.Legacy.UIManager.PlayBtn()</c> currently instantiates
    /// <see cref="DefaultLevelPrefab"/>. Phase F's <c>GameController.SpawnDefaultLevel</c>
    /// will absorb that responsibility.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelCatalog", menuName = "GoGo/Level Catalog")]
    public class LevelCatalog : ScriptableObject
    {
        [SerializeField] private GameObject _defaultLevelPrefab;
        [SerializeField] private GameObject[] _additionalLevels;

        public GameObject DefaultLevelPrefab => _defaultLevelPrefab;
        public IReadOnlyList<GameObject> AdditionalLevels => _additionalLevels;
    }
}
