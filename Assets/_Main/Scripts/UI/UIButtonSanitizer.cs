using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Disables Inspector-wired Button.onClick listeners whose target is null.
///
/// The original DATN prefabs were authored against the legacy `DATN.Legacy.UIManager`
/// hierarchy and other scene singletons. After the NinjaUI migration, many of those
/// targets resolve to null at runtime (the GameObjects/components they pointed at no
/// longer exist or got short-circuited). Clicking those buttons throws
/// NullReferenceException which the user perceives as "buttons not working".
///
/// This helper walks the given root (or whole scene), inspects every Button.onClick
/// persistent listener, and switches the call state to <see cref="UnityEventCallState.Off"/>
/// for any whose persistent target is null. Runtime AddListener(...) calls added by
/// our SV_*UI scripts are unaffected — they execute as normal.
/// </summary>
public static class UIButtonSanitizer
{
    /// <summary>Sanitize every Button in the given subtree (including inactive).</summary>
    public static int SanitizeChildButtons(Transform root)
    {
        if (root == null) return 0;
        int killed = 0;
        var btns = root.GetComponentsInChildren<Button>(true);
        foreach (var b in btns) killed += SanitizeButton(b);
        return killed;
    }

    /// <summary>Sanitize all Buttons in all loaded scenes. Heavy — call sparingly.</summary>
    public static int SanitizeAllInScene()
    {
        int killed = 0;
        var allBtns = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var b in allBtns) killed += SanitizeButton(b);
        if (killed > 0) Debug.Log($"[UIButtonSanitizer] Disabled {killed} broken persistent listeners across scene.");
        return killed;
    }

    private static int SanitizeButton(Button b)
    {
        if (b == null || b.onClick == null) return 0;
        int n = b.onClick.GetPersistentEventCount();
        int killed = 0;
        for (int i = 0; i < n; i++)
        {
            var target = b.onClick.GetPersistentTarget(i);
            if (target == null)
            {
                // Persistent listener points to a destroyed/missing target — disable it
                // so clicking the button doesn't throw.
                b.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
                killed++;
            }
        }
        return killed;
    }
}
