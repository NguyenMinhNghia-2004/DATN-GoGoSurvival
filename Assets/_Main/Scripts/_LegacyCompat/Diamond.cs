using System.Collections;
using Luzart;
using UnityEngine;

/// <summary>
/// Post-nuke (W5/W6) restoration of the legacy <c>Diamond.cs</c> MonoBehaviour.
/// The deleted class is referenced by Missing Script on Diamond/DiamondGreen/DiamondBlue/
/// DiamondRed/DiamondVIP prefabs (guid <c>34cd61dbc7186ac4583e66814713b55b</c>) with
/// serialized fields <c>Player</c>, <c>Manager</c>, <c>UIManager</c>, <c>Green</c>,
/// <c>WorkOn</c>. BoxCollider2D on those prefabs is already <c>isTrigger=true</c>.
///
/// <para>Pickup behaviour: trigger overlap with the Player (body collider or the
/// "RoadDetections" magnet zone) triggers a Survivor.io-style <b>InBack</b> tween —
/// the gem first eases AWAY from the player (overshoot), then accelerates back into
/// them. Award XP + destroy when the tween completes. Without this animation, gems
/// dropped inside the magnet zone vanished in the same frame they spawned, leaving
/// the user with no visual feedback.</para>
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

    [Tooltip("Duration of the InBack fly-to-player animation (seconds).")]
    [SerializeField] private float flyDuration = 0.45f;

    private bool _consumed;
    private bool _flying;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_consumed || _flying || other == null) return;
        // Accept Player body collider OR the "RoadDetections" magnet zone OR anything
        // under a Player-tagged root (covers future child colliders).
        bool isPlayerSide =
            other.CompareTag("Player") ||
            other.CompareTag("RoadDetections") ||
            (other.transform.root != null && other.transform.root.CompareTag("Player"));
        if (!isPlayerSide) return;

        _flying = true;

        // Disable our own collider so the magnet doesn't re-trigger us mid-fly.
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Resolve a stable player Transform to track (the magnet's collider was on a
        // child, so walk up to the tagged root).
        Transform playerTr = other.transform.root != null && other.transform.root.CompareTag("Player")
            ? other.transform.root
            : other.transform;
        StartCoroutine(FlyToPlayerInBack(playerTr));
    }

    /// <summary>
    /// Tween position from current pos → player pos over <c>flyDuration</c> using the
    /// Penner InBack easing curve. EaseInBack goes NEGATIVE briefly at the start, so
    /// LerpUnclamped drives the gem past the spawn point in the opposite direction of
    /// the player (the "back" overshoot) before rushing inward.
    /// </summary>
    private IEnumerator FlyToPlayerInBack(Transform player)
    {
        Vector3 start = transform.position;
        float t = 0f;
        while (t < flyDuration && player != null && this != null)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / flyDuration);
            float eased = EaseInBack(p);
            // LerpUnclamped is important — clamped Lerp would clip the negative overshoot
            // back to 0, killing the "fly out" half of the animation.
            transform.position = Vector3.LerpUnclamped(start, player.position, eased);
            yield return null;
        }
        TryAwardXP();
        _consumed = true;
        Destroy(gameObject);
    }

    /// <summary>Penner's InBack easing — t² × ((s+1)t − s) with s = 1.70158.</summary>
    private static float EaseInBack(float t)
    {
        const float s = 1.70158f;
        return t * t * ((s + 1f) * t - s);
    }

    private bool TryAwardXP()
    {
        var srm = SceneRootManager.Instance;
        var pc = srm != null ? srm.Domain?.Get<PlayerCharacter>() : null;
        if (pc == null || pc.Stats == null) return false;
        var xp = pc.Stats.GetRuntime(StatType.Runtime_XP);
        if (xp == null) return false;
        float reward = xpReward * (Green ? 2f : 1f);
        xp.Set(xp.Value + reward);
        return true;
    }
}
