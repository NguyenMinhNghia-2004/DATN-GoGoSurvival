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
    private UnityEngine.UI.Text _legacyLabel;
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
        if (_number == null) return;
        string valStr = CurrencyManager.FormatNumber((long)_number.Value);
        if (_label != null) _label.text = valStr;
        if (_legacyLabel != null) _legacyLabel.text = valStr;
    }

    private void EnsureLabel()
    {
        if (_label != null || _legacyLabel != null) return;

        // Try finding existing UI elements in known prefabs
        string[] paths = {
            "TOP/Coins/Value",          // SV_MainMenu
            "Currency/Value"            // SV_PausePopup
        };

        foreach (var path in paths)
        {
            var tr = transform.Find(path);
            if (tr != null)
            {
                _label = tr.GetComponent<TextMeshProUGUI>();
                _legacyLabel = tr.GetComponent<UnityEngine.UI.Text>();
                if (_label != null || _legacyLabel != null) return;
            }
        }

        // Search the whole hierarchy for a Text component under a "Coins" or "Currency" object as a fallback
        foreach (var tr in GetComponentsInChildren<Transform>(true))
        {
            if (tr.name == "Coins" || tr.name == "Currency")
            {
                var valTr = tr.Find("Value");
                if (valTr != null)
                {
                    _label = valTr.GetComponent<TextMeshProUGUI>();
                    _legacyLabel = valTr.GetComponent<UnityEngine.UI.Text>();
                    if (_label != null || _legacyLabel != null) return;
                }
            }
        }

    }
}
