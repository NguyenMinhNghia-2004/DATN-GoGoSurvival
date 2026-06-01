using Luzart;
using UnityEngine;

/// <summary>
/// Post-nuke (W5/W6) restoration of a SECOND legacy diamond MonoBehaviour, distinct from
/// <see cref="Diamond"/>. Leve1.prefab embeds 455 pre-placed diamond GameObjects on the map
/// (rải sẵn trên map khi vào game), all referencing the deleted class via Missing Script
/// guid <c>681ed9a302e746e488b01c05eb9639c9</c> with serialized fields
/// <c>Player/Manager/Boolean/Flasher</c>. BoxCollider2D on those GOs is already
/// <c>isTrigger=true</c> + tag "Diamond" is set.
///
/// <para>This class restores behaviour for the SAME guid (see .cs.meta) so Unity rebinds
/// the Missing Script across all 455 instances without a single prefab edit. Field names
/// match the YAML so serialized values (mostly null after nuke) rebind cleanly.</para>
///
/// <para>Behaviour mirrors <see cref="Diamond"/>: on trigger overlap with the Player (body
/// or DetecteurRoad magnet child), award XP via <c>Domain.Get&lt;PlayerCharacter&gt;().Stats
/// .GetRuntime(Runtime_XP)</c>, then destroy. Optional <see cref="Flasher"/> spawn skipped
/// (visual juice deferred — restore later if needed).</para>
/// </summary>
[DisallowMultipleComponent]
public class LevelDiamond : MonoBehaviour
{
    // ---- Serialized fields preserved from legacy YAML for rebind ---------
    [Tooltip("Legacy ref kept for serialization compat. Runtime resolves player from Domain.")]
    public GameObject Player;
    [Tooltip("Legacy ref kept for serialization compat.")]
    public GameObject Manager;
    [Tooltip("Legacy boolean-flag carrier (deleted BooleanManager). Kept for serialization compat.")]
    public GameObject Boolean;
    [Tooltip("Legacy pickup-flash VFX prefab. Currently unused; restore the visual juice later.")]
    public GameObject Flasher;

    // ---- Pickup tuning ---------------------------------------------------
    [Tooltip("XP awarded to the player on pickup. Tune per gem placement via Inspector.")]
    [SerializeField] private float xpReward = 1f;

    private bool _consumed;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_consumed || other == null) return;
        // Player body collider (tag "Player") OR DetecteurRoad child magnet (tag "RoadDetections").
        bool isPlayerSide =
            other.CompareTag("Player") ||
            other.CompareTag("RoadDetections") ||
            (other.transform.root != null && other.transform.root.CompareTag("Player"));
        if (!isPlayerSide) return;
        if (!TryAwardXP()) return;
        _consumed = true;
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private bool TryAwardXP()
    {
        var srm = SceneRootManager.Instance;
        var pc = srm != null ? srm.Domain?.Get<PlayerCharacter>() : null;
        if (pc == null || pc.Stats == null) return false;
        var xp = pc.Stats.GetRuntime(StatType.Runtime_XP);
        if (xp == null) return false;
        xp.Set(xp.Value + xpReward);
        return true;
    }
}
