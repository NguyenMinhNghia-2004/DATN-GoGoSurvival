using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Small runtime uGUI builder. Used ONLY by SV_ItemDetailPopup (which has no prefab to bind to)
/// to construct a clean centered detail popup. The Shop/Equipment screens bind to existing
/// prefab visuals and do not use these builders. Legacy UnityEngine.UI.Text matches prefab fonts.
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
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return rt;
    }

    public static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
    }

    /// <summary>Full-screen dim backdrop; clicking it invokes <paramref name="onClick"/> (close).</summary>
    public static Image Backdrop(Transform parent, Color color, Action onClick)
    {
        var rt = StretchFull(NewRect("SV_Backdrop", parent));
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        var btn = rt.gameObject.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        if (onClick != null) btn.onClick.AddListener(() => onClick());
        return img;
    }

    public static RectTransform Panel(Transform parent, string name, Vector2 size, Color color)
    {
        var rt = NewRect(name, parent);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        // Eat clicks so tapping the panel doesn't close via the backdrop button.
        rt.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
        return rt;
    }

    public static Image Picture(Transform parent, string name, Vector2 size, Sprite sprite, Color color)
    {
        var rt = NewRect(name, parent);
        rt.sizeDelta = size;
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.preserveAspect = true;
        return img;
    }

    public static Text Label(Transform parent, string text, int fontSize, TextAnchor align, Color color, Font font)
    {
        var rt = NewRect("SV_Label", parent);
        var t = rt.gameObject.AddComponent<Text>();
        t.text = text; t.font = font; t.fontSize = fontSize; t.alignment = align; t.color = color;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    /// <summary>Text button; the label Text is the button's child (GetComponentInChildren).</summary>
    public static Button TextButton(Transform parent, string label, Vector2 size, Color bg, Color textColor,
        Font font, Action onClick)
    {
        var rt = NewRect("SV_Button", parent);
        rt.sizeDelta = size;
        var img = rt.gameObject.AddComponent<Image>();
        img.color = bg;
        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        if (onClick != null) btn.onClick.AddListener(() => onClick());

        var lrt = StretchFull(NewRect("Text", rt));
        var t = lrt.gameObject.AddComponent<Text>();
        t.text = label; t.font = font; t.fontSize = Mathf.RoundToInt(Mathf.Min(size.y * 0.42f, 30));
        t.alignment = TextAnchor.MiddleCenter; t.color = textColor;
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
        return btn;
    }

    public static void SetAnchoredTop(RectTransform rt, float height, float y)
    {
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(-24, height);
        rt.anchoredPosition = new Vector2(0, y);
    }
}
