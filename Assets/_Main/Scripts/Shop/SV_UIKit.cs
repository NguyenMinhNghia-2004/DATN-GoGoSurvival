using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minimal helper for the one runtime control the bound shop/equipment screens still need:
/// a Close (X) button (the legacy mockups have no close affordance). Everything else binds to
/// existing prefab visuals. Uses legacy UnityEngine.UI.Text to match the prefabs' fonts.
/// </summary>
public static class SV_UIKit
{
    public static Font DefaultFont()
    {
        Font f = null;
        try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
        if (f == null) { try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
        return f;
    }

    private static RectTransform StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
    }

    /// <summary>A small square Close (X) button anchored to the top-right of <paramref name="parent"/>.</summary>
    public static Button CloseButton(Transform parent, Font font, Action onClick)
    {
        var rt = NewRect("SV_CloseButton", parent);
        rt.sizeDelta = new Vector2(64, 64);
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-12, -12);

        var img = rt.gameObject.AddComponent<Image>();
        img.color = new Color(0.8f, 0.2f, 0.2f, 1f);
        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        if (onClick != null) btn.onClick.AddListener(() => onClick());

        var label = StretchFull(NewRect("Text", rt));
        var t = label.gameObject.AddComponent<Text>();
        t.text = "X";
        t.font = font;
        t.fontSize = 32;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        return btn;
    }
}
