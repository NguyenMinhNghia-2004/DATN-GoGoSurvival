using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Holds Inspector-wired Level prefab references. Originally drove the legacy SelectMap UI
/// (deleted in Phase A migration); now exists only as a data holder so UIManager.PlayBtn
/// can resolve which Level prefab to Instantiate. Eventually replaced by an IContent
/// LevelConfig service in Phase B7.
/// </summary>
public class LevelsManager : MonoBehaviour
{
    [Header("Active level prefab (Inspector-wired)")]
    public GameObject Level1;

    [Header("All level prefabs")]
    public GameObject[] Levels;

    // Legacy SelectMap UI was deleted; the rest of the original LevelsManager (swipe nav,
    // Map sprite swapping, etc.) is no longer used. Keep this class minimal so the
    // serialized GameObject references survive scene load.
}
