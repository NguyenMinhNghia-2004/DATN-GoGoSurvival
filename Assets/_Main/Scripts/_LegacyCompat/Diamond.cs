using Luzart;
using UnityEngine;

/// <summary>
/// Post-nuke (W5/W6) restoration of the legacy <c>Diamond.cs</c> MonoBehaviour.
/// The deleted class is referenced by Missing Script on Diamond/DiamondGreen/DiamondBlue/
/// DiamondRed/DiamondVIP prefabs (guid <c>34cd61dbc7186ac4583e66814713b55b</c>) with
/// serialized fields <c>Player</c>, <c>Manager</c>, <c>UIManager</c>, <c>Green</c>,
/// <c>WorkOn</c>. BoxCollider2D on those prefabs is already <c>isTrigger=true</c>.
///
/// <para>This file restores the class behind the SAME guid (see .cs.meta) so Unity auto-rebinds
/// the Missing Script reference across all 4 colored Diamond prefabs without prefab edits.
/// Field names + types match the YAML so serialized values rebind cleanly.</para>
///
/// <para>Behaviour: on trigger overlap with the Player, add <see cref="xpReward"/> to
/// <c>StatType.Runtime_XP</c> (GameController already watches via <c>OnXPChange</c>) and
/// destroy self. Legacy Player/Manager/UIManager fields kept for serialization compatibility;
/// runtime resolves PlayerCharacter from Domain instead (so the gem works even if Inspector
/// refs are null post-nuke).</para>
/// </summary>
[DisallowMultipleComponent]
public class Diamond : MonoBehaviour
{
    // ---- Serialized fields preserved from legacy YAML for rebind ---------
    [Tooltip("Legacy ref kept for serialization compat. Runtime resolves player from Domain.")]
    public GameObject Player;
    [Tooltip("Legacy ref kept for serialization compat.")]
    public GameObject Manager;
    [Tooltip("Legacy ref kept for serialization compat.")]
    public GameObject UIManager;
    [Tooltip("Legacy gem-colour flag (true = green/large reward).")]
    public bool Green;
    [Tooltip("Legacy run-gate flag.")]
    public bool WorkOn;

    // ---- Pickup tuning ---------------------------------------------------
    [Tooltip("XP awarded to the player on pickup. Tune per gem prefab via Inspector.")]
    [SerializeField] private float xpReward = 1f;

    [Tooltip("If true, destroy the gem on pickup. Off only for debugging.")]
    [SerializeField] private bool destroyOnPickup = true;

    private bool _consumed; // Guard against multiple trigger frames in a single overlap pass.

    private bool _dbgLoggedAwake;
    private void Awake()
    {
        if (!_dbgLoggedAwake) { _dbgLoggedAwake = true; Debug.Log($"[DBG-CHAIN] DIA: Diamond.Awake on '{name}' tag='{tag}' layer={gameObject.layer} hasCol2D={GetComponent<Collider2D>()!=null}"); }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_consumed || other == null) return;
        // Player has two colliders relevant here:
        //   1) Body collider on the Player GO itself (tag "Player") — damage hitbox.
        //   2) "DetecteurRoad" child collider (tag "RoadDetections") — pickup magnet zone,
        //      scale ~2.3× larger than the body so gems are vacuumed before touching the player.
        // Either qualifies as a pickup trigger. Climbing transform.root.tag also covers any
        // future child colliders authored under Player.
        bool isPlayerSide =
            other.CompareTag("Player") ||
            other.CompareTag("RoadDetections") ||
            (other.transform.root != null && other.transform.root.CompareTag("Player"));
        if (!isPlayerSide) return;
        if (!TryAwardXP()) { Debug.LogWarning($"[DBG-CHAIN] DIA: '{name}' TryAwardXP returned false"); return; }
        _consumed = true;
        if (destroyOnPickup) Destroy(gameObject);
    }

    private bool TryAwardXP()
    {
        var srm = SceneRootManager.Instance;
        var pc = srm != null ? srm.Domain?.Get<PlayerCharacter>() : null;
        if (pc == null) { Debug.LogWarning($"[DBG-CHAIN] DIA: PlayerCharacter null (srm={(srm==null?"NULL":"OK")})"); return false; }
        if (pc.Stats == null) { Debug.LogWarning("[DBG-CHAIN] DIA: pc.Stats null"); return false; }
        var xp = pc.Stats.GetRuntime(StatType.Runtime_XP);
        if (xp == null) { Debug.LogWarning("[DBG-CHAIN] DIA: Runtime_XP stat null"); return false; }
        // Legacy "Green" flag historically doubled the reward; preserve that bias so existing
        // gem placements that toggled this in scene still feel weighted.
        float reward = xpReward * (Green ? 2f : 1f);
        float before = (float)xp.Value;
        xp.Set(xp.Value + reward);
        Debug.Log($"[DBG-CHAIN] DIA: awarded {reward} XP (before={before} after={xp.Value})");
        return true;
    }
}
