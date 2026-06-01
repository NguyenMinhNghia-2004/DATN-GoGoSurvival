using System.Collections;
using Luzart;
using Luzart.Pickups;
using UnityEngine;

/// <summary>
/// Base MonoBehaviour cho mọi pickup đặt trên sàn. <b>Strict data-driven</b>: mọi reward
/// đều đến từ <see cref="DropConfig.Effects"/>. Không còn legacy hardcoded path — pickup
/// thiếu <see cref="_data"/> sẽ log warning và destroy không cộng gì.
///
/// <para><b>Flow</b>: OnTriggerEnter2D → trigger gating → tween InBack về player →
/// loop <c>_data.Effects[*].Apply(ctx)</c> → Destroy.</para>
///
/// <para>Subclass (Diamond/Coin/LevelDiamond) chỉ tồn tại để giữ GUID legacy cho hàng
/// nghìn instance trong Leve*.prefab; nội dung là rỗng. Tất cả config nằm ở DropConfig SO.</para>
/// </summary>
[DisallowMultipleComponent]
public class PickupBase : MonoBehaviour
{
    [Tooltip("DropConfig SO quy định pickup này làm gì khi nhặt (effects list + flyDuration).")]
    [SerializeField] private DropConfig _data;

    private bool _consumed;
    private bool _flying;

    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (_consumed || _flying || other == null) return;
        bool isPlayerSide =
            other.CompareTag("Player") ||
            other.CompareTag("RoadDetections") ||
            (other.transform.root != null && other.transform.root.CompareTag("Player"));
        if (!isPlayerSide) return;

        Transform playerTr = other.transform.root != null && other.transform.root.CompareTag("Player")
            ? other.transform.root
            : other.transform;
        BeginFly(playerTr, ResolvePlayerCharacter());
    }

    /// <summary>
    /// Public API cho <see cref="PickupEffect_MagnetAll"/> hoặc effect hút khác gọi:
    /// ép pickup này fly vào player ngay, kể cả khi chưa va chạm. Idempotent.
    /// </summary>
    public void TriggerMagnet(PlayerCharacter player)
    {
        if (_consumed || _flying || player == null || player.Transform == null) return;
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo == null) return;
        BeginFly(playerGo.transform, player);
    }

    private void BeginFly(Transform playerTr, PlayerCharacter playerChar)
    {
        _flying = true;
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        // _data có thể null (prefab chưa migrate) — dùng 0.45 default an toàn.
        float dur = _data != null ? _data.FlyDuration : 0.45f;
        StartCoroutine(FlyToPlayerInBack(playerTr, playerChar, dur));
    }

    private IEnumerator FlyToPlayerInBack(Transform player, PlayerCharacter playerChar, float duration)
    {
        Vector3 start = transform.position;
        float t = 0f;
        if (duration <= 0f)
        {
            if (player != null) transform.position = player.position;
        }
        else
        {
            while (t < duration && player != null && this != null)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                float eased = EaseInBack(p);
                transform.position = Vector3.LerpUnclamped(start, player.position, eased);
                yield return null;
            }
        }
        ApplyEffects(playerChar);
        _consumed = true;
        Destroy(gameObject);
    }

    private void ApplyEffects(PlayerCharacter player)
    {
        if (_data == null)
        {
            Debug.LogWarning($"[Pickup] '{gameObject.name}' không có DropConfig assigned — destroy không reward. " +
                             "Gán PickupBase._data trong prefab để fix.");
            return;
        }
        var effects = _data.Effects;
        if (effects == null || effects.Count == 0) return;
        var ctx = new PickupContext(transform.position, player, gameObject);
        for (int i = 0; i < effects.Count; i++)
        {
            var fx = effects[i];
            if (fx != null) fx.Apply(in ctx);
        }
    }

    /// <summary>Resolve PlayerCharacter từ Domain bootstrap. Có thể null nếu boot chưa xong.</summary>
    protected static PlayerCharacter ResolvePlayerCharacter()
    {
        var srm = SceneRootManager.Instance;
        return srm != null ? srm.Domain?.Get<PlayerCharacter>() : null;
    }

    /// <summary>Penner's InBack — t² × ((s+1)t − s) với s = 1.70158.</summary>
    private static float EaseInBack(float t)
    {
        const float s = 1.70158f;
        return t * t * ((s + 1f) * t - s);
    }
}
