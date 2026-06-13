using Luzart;
using TMPro;
using UnityEngine;

/// <summary>
/// Reusable currency display: shows the shop gold balance (the <c>io_gold</c>
/// <see cref="ResourcePool"/>) and live-updates when it changes. Builds its own TMP label at the
/// top-right of whatever UI it is attached to, so it works on screens that have no authored
/// currency text (main menu, pause, win, lose). Add it once via
/// <c>gameObject.AddComponent&lt;SV_GoldDisplay&gt;()</c> from a UI's OnCreate.
///
/// <para>Why io_gold: the project has 3 disconnected currencies; per the game design the shop gold
/// is THE spendable currency, and in-run coins are banked into it at run end (see
/// <c>SV_EndGameBridge</c>). All meta screens therefore display io_gold.</para>
/// </summary>
public sealed class SV_GoldDisplay : MonoBehaviour
{
    private IResourcePool _pool;
    private INumber _number;
    private TextMeshProUGUI _label;
    private bool _subscribed;

    private void OnEnable()
    {
        EnsureLabel();
        Resolve();
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        if (_subscribed && _number != null) _number.Changed -= OnChanged;
        _subscribed = false;
    }

    private void Update()
    {
        // Domain/io_gold may not be ready the first frame the UI appears; keep trying cheaply
        // until resolved, then this becomes a no-op (and we can stop polling).
        if (_pool == null)
        {
            Resolve();
            Subscribe();
            Refresh();
            if (_pool != null) enabled = true; // resolved; Update still cheap (early-out)
        }
    }

    private void Resolve()
    {
        if (_pool != null) return;
        var domain = SceneRootManager.Instance != null ? SceneRootManager.Instance.Domain : null;
        var rp = domain != null ? domain.Get<ResourcePool>("io_gold") : null;
        if (rp == null) return;
        _pool = rp;
        _number = ((IResourcePool)rp).Value; // also initializes the inner Number
    }

    private void Subscribe()
    {
        if (_subscribed || _number == null) return;
        _number.Changed += OnChanged;
        _subscribed = true;
    }

    private void OnChanged(INumber n) => Refresh();

    private void Refresh()
    {
        if (_label == null || _number == null) return;
        _label.text = CurrencyManager.FormatNumber((long)_number.Value);
    }

    private void EnsureLabel()
    {
        if (_label != null) return;

        var go = new GameObject("GoldDisplay_Runtime", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-40f, -40f);
        rt.sizeDelta = new Vector2(360f, 60f);

        _label = go.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) _label.font = TMP_Settings.defaultFontAsset;
        _label.alignment = TextAlignmentOptions.Right;
        _label.fontSize = 40;
        _label.fontStyle = FontStyles.Bold;
        _label.color = new Color(1f, 0.85f, 0.2f, 1f); // gold
        _label.text = "0";
    }
}
