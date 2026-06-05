using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tiny runtime uGUI builder used by SV_ShopView / SV_EquipmentView to lay functional
/// controls (panels, labels, buttons, scroll lists, a Close button) on top of the messy
/// legacy mockup prefabs. Legacy UnityEngine.UI.Text is used to match the prefabs' fonts.
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

    public static RectTransform StretchFull(RectTransform rt)
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

    /// <summary>Full-screen dim backdrop image.</summary>
    public static Image Backdrop(Transform parent, Color color)
    {
        var rt = StretchFull(NewRect("SV_Backdrop", parent));
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        return img;
    }

    /// <summary>Center-anchored panel of a fixed size.</summary>
    public static RectTransform Panel(Transform parent, string name, Vector2 size, Color color)
    {
        var rt = NewRect(name, parent);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        return rt;
    }

    public static Text Label(Transform parent, string text, int fontSize, TextAnchor align, Color color, Font font)
    {
        var rt = NewRect("SV_Label", parent);
        var t = rt.gameObject.AddComponent<Text>();
        t.text = text;
        t.font = font;
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    public static Button Button(Transform parent, string label, Vector2 size, Color bg, Color textColor,
        Font font, Action onClick)
    {
        var rt = NewRect("SV_Button", parent);
        rt.sizeDelta = size;
        var img = rt.gameObject.AddComponent<Image>();
        img.color = bg;
        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        if (onClick != null) btn.onClick.AddListener(() => onClick());

        var label_rt = StretchFull(NewRect("Text", rt));
        var t = label_rt.gameObject.AddComponent<Text>();
        t.text = label;
        t.font = font;
        t.fontSize = Mathf.RoundToInt(Mathf.Min(size.y * 0.5f, 28));
        t.alignment = TextAnchor.MiddleCenter;
        t.color = textColor;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return btn;
    }

    /// <summary>A small square Close (X) button anchored to the top-right of <paramref name="parent"/>.</summary>
    public static Button CloseButton(Transform parent, Font font, Action onClick)
    {
        var btn = Button(parent, "X", new Vector2(64, 64), new Color(0.8f, 0.2f, 0.2f, 1f), Color.white, font, onClick);
        var rt = (RectTransform)btn.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-12, -12);
        return rt.gameObject.GetComponent<Button>();
    }

    /// <summary>Builds a vertical scroll list. Returns the Content transform to parent rows under.</summary>
    public static RectTransform VerticalScroll(Transform parent, RectOffset padding, float spacing)
    {
        var scrollRT = StretchFull(NewRect("SV_Scroll", parent));
        // leave room at top for header
        scrollRT.offsetMax = new Vector2(0, -90);
        scrollRT.offsetMin = new Vector2(0, 12);
        var scroll = scrollRT.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24;

        var viewport = StretchFull(NewRect("Viewport", scrollRT));
        viewport.gameObject.AddComponent<RectMask2D>();
        var vpImg = viewport.gameObject.AddComponent<Image>();
        vpImg.color = new Color(0, 0, 0, 0.001f); // near-invisible, needed so mask has a graphic

        var content = NewRect("Content", viewport);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0, 0);

        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = padding;
        vlg.spacing = spacing;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport;
        scroll.content = content;
        return content;
    }

    /// <summary>A horizontal row container with a preferred height (for layout groups).</summary>
    public static RectTransform Row(Transform parent, float height, Color bg)
    {
        var rt = NewRect("SV_Row", parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = bg;
        var le = rt.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;
        var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(12, 12, 6, 6);
        h.spacing = 10;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = true;
        return rt;
    }
}
