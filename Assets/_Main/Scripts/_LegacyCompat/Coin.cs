using UnityEngine;

/// <summary>
/// Post-nuke restoration of the legacy <c>Coin</c> MonoBehaviour. Across 6 level prefabs
/// (Leve1-6) there are 476 pre-placed coin GameObjects with tag "Coins" + CircleCollider2D
/// (isTrigger=true) referencing this deleted class via Missing Script guid
/// <c>ee0fceb031db4134fafaa69a2a97b60b</c> + serialized fields <c>Player/Manager/UIManager/WorkOn</c>.
///
/// <para>This file restores the class behind the SAME guid (see .cs.meta) so Unity auto-rebinds
/// all 476 instances without prefab edits. Field names match YAML for serialized value rebind.</para>
///
/// <para>Behavior: on trigger overlap with the Player (body or DetecteurRoad magnet),
/// award <see cref="coinReward"/> to <see cref="CurrencyManager"/> (upgraded stub →
/// self-instantiating singleton — fires OnCoinChanged so SV_GameplayHudUI updates the HUD
/// label) and destroy self.</para>
/// </summary>
[DisallowMultipleComponent]
public class Coin : MonoBehaviour
{
    // ---- Serialized fields preserved from legacy YAML for rebind ---------
    [Tooltip("Legacy ref kept for serialization compat. Pickup uses tag-based detection.")]
    public GameObject Player;
    [Tooltip("Legacy ref kept for serialization compat.")]
    public GameObject Manager;
    [Tooltip("Legacy ref kept for serialization compat.")]
    public GameObject UIManager;
    [Tooltip("Legacy run-gate flag (true = pickup enabled). Default true in original YAML.")]
    public bool WorkOn = true;

    // ---- Pickup tuning ---------------------------------------------------
    [Tooltip("Coins awarded to the player on pickup. Tune per coin placement via Inspector.")]
    [SerializeField] private long coinReward = 1;

    private bool _consumed;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_consumed || other == null) return;
        // Same pickup contract as Diamond/LevelDiamond: body collider OR DetecteurRoad magnet.
        bool isPlayerSide =
            other.CompareTag("Player") ||
            other.CompareTag("RoadDetections") ||
            (other.transform.root != null && other.transform.root.CompareTag("Player"));
        if (!isPlayerSide) return;
        CurrencyManager.Instance.AddCoin(coinReward);
        _consumed = true;
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
