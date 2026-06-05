using System;
using Luzart;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds and drives the functional Equipment overlay (6 slot widgets + owned-item list +
/// Close button) on top of the legacy SV_Equipement mockup. Equip/unequip mutate
/// SV_PlayerInventory (persisted); the actual stat bonus is applied at gameplay start by
/// SV_EquipmentStatApplier. Mirrors IO_Training's slot/equip concept, simplified.
/// </summary>
public class SV_EquipmentView
{
    private static readonly ETypeItem[] Slots =
    {
        ETypeItem.Weapon, ETypeItem.Armor, ETypeItem.Necklace,
        ETypeItem.Belt, ETypeItem.Gloves, ETypeItem.Shoes,
    };

    private SV_ItemCatalog _catalog;
    private SV_PlayerInventory _inv;
    private Font _font;
    private RectTransform _slotRow;
    private RectTransform _content;
    private bool _subscribed;

    public void Build(Transform root, Action onClose)
    {
        _catalog = SV_ItemCatalog.Load();
        _inv = SV_PlayerInventory.Instance;
        _font = SV_UIKit.DefaultFont();

        var backdrop = SV_UIKit.Backdrop(root, new Color(0, 0, 0, 0.6f));
        var panel = SV_UIKit.Panel(backdrop.transform, "SV_EquipPanel",
            new Vector2(760, 1040), new Color(0.10f, 0.13f, 0.12f, 0.98f));

        var title = SV_UIKit.Label(panel, "EQUIPMENT", 44, TextAnchor.UpperCenter, Color.white, _font);
        AnchorTop(title.rectTransform, 64, -10);

        SV_UIKit.CloseButton(panel, _font, onClose);

        // Slot row (6 equipment slots) below the title.
        _slotRow = NewSlotRow(panel);

        // Owned items list fills the rest.
        _content = SV_UIKit.VerticalScroll(panel, new RectOffset(12, 12, 12, 12), 10);
        // push the scroll root down so the list sits below the slot row (content→Viewport→SV_Scroll)
        var scrollRoot = _content.parent.parent as RectTransform;
        if (scrollRoot != null) scrollRoot.offsetMax = new Vector2(0, -250);

        Refresh();
        _inv.OnChanged += Refresh;
        _subscribed = true;
    }

    public void OnShow()
    {
        if (_content != null) Refresh();
    }

    public void Dispose()
    {
        if (!_subscribed) return;
        if (_inv != null) _inv.OnChanged -= Refresh;
        _subscribed = false;
    }

    private RectTransform NewSlotRow(Transform parent)
    {
        var rt = new GameObject("SV_SlotRow", typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(-24, 150);
        rt.anchoredPosition = new Vector2(0, -84);
        var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(6, 6, 6, 6);
        h.spacing = 8;
        h.childAlignment = TextAnchor.MiddleCenter;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = true;
        h.childForceExpandHeight = true;
        return rt;
    }

    private void Refresh()
    {
        BuildSlots();
        BuildOwnedList();
    }

    private void BuildSlots()
    {
        for (int i = _slotRow.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(_slotRow.GetChild(i).gameObject);

        foreach (var slot in Slots)
        {
            string id = _inv.GetEquipped(slot);
            var entry = id != null ? _catalog?.GetById(id) : null;
            bool filled = entry != null;

            var box = new GameObject($"Slot_{slot}", typeof(RectTransform)).GetComponent<RectTransform>();
            box.SetParent(_slotRow, false);
            var img = box.gameObject.AddComponent<Image>();
            img.color = filled ? new Color(0.2f, 0.45f, 0.25f, 1f) : new Color(0.2f, 0.2f, 0.24f, 1f);

            var v = box.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(4, 4, 6, 6);
            v.childAlignment = TextAnchor.MiddleCenter;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;

            SV_UIKit.Label(box, slot.ToString(), 16, TextAnchor.MiddleCenter, new Color(0.8f, 0.8f, 0.8f), _font);
            SV_UIKit.Label(box, filled ? entry.displayName : "—", 18, TextAnchor.MiddleCenter, Color.white, _font);

            if (filled)
            {
                // Tap a filled slot to unequip.
                var btn = box.gameObject.AddComponent<Button>();
                btn.targetGraphic = img;
                var captured = slot;
                btn.onClick.AddListener(() => _inv.Unequip(captured));
                SV_UIKit.Label(box, "(tap=off)", 12, TextAnchor.MiddleCenter, new Color(1f, 0.7f, 0.7f), _font);
            }
        }
    }

    private void BuildOwnedList()
    {
        for (int i = _content.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(_content.GetChild(i).gameObject);

        if (_catalog == null)
        {
            SV_UIKit.Label(_content, "No catalog found.", 22, TextAnchor.MiddleCenter, Color.red, _font);
            return;
        }

        int shown = 0;
        foreach (var e in _catalog.Entries)
        {
            if (e == null || !_inv.IsOwned(e.id)) continue;
            shown++;
            BuildOwnedCard(e);
        }
        if (shown == 0)
            SV_UIKit.Label(_content, "No items owned yet. Buy some in the Shop!", 22,
                TextAnchor.MiddleCenter, Color.white, _font);
    }

    private void BuildOwnedCard(SV_ItemEntry e)
    {
        var row = SV_UIKit.Row(_content, 96, new Color(0.16f, 0.2f, 0.18f, 1f));

        var info = SV_UIKit.Label(row, $"{e.displayName}\n<size=18><color=#cfcfcf>{StatText(e)}</color></size>",
            26, TextAnchor.MiddleLeft, Color.white, _font);
        var infoLE = info.gameObject.AddComponent<LayoutElement>();
        infoLE.flexibleWidth = 1; infoLE.minWidth = 240;

        bool equipped = _inv.IsEquipped(e.id);
        var btn = SV_UIKit.Button(row, equipped ? "UNEQUIP" : "EQUIP",
            new Vector2(180, 70),
            equipped ? new Color(0.6f, 0.3f, 0.2f) : new Color(0.2f, 0.5f, 0.7f),
            Color.white, _font,
            equipped ? (Action)(() => _inv.Unequip(e.slot)) : () => _inv.Equip(e));
        var btnLE = btn.gameObject.AddComponent<LayoutElement>();
        btnLE.minWidth = 180; btnLE.preferredWidth = 180;
    }

    private static string StatText(SV_ItemEntry e)
    {
        string amount = e.mode == StatsBehavior.StatBonusMode.Additive
            ? $"+{e.statAmount:0.##}"
            : $"+{e.statAmount * 100:0.#}%";
        return $"{e.slot} · {e.statType} {amount}";
    }

    private static void AnchorTop(RectTransform rt, float height, float y)
    {
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0, height);
        rt.anchoredPosition = new Vector2(0, y);
    }
}
