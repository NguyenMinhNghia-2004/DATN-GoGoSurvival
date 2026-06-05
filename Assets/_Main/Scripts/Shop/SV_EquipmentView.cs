using System;
using System.Collections.Generic;
using Luzart;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Binds equip logic onto the EXISTING SV_Equipement mockup (no generated panels):
///  - the 6 slots around the character (HalfPlayerShow/Left|Right/Firs|Second|Third) become the
///    6 equipment slots (Weapon/Armor/Necklace/Belt/Gloves/Shoes); tapping a filled slot unequips.
///  - the item grid (HalfPlayerShow/.../WeaponIcons/Cnte*) lists owned items; tapping one equips
///    it into its slot (or unequips if already equipped).
/// A small Close (X) button is added because the mockup has none.
/// </summary>
public class SV_EquipmentView
{
    private static readonly KeyValuePair<string, ETypeItem>[] SlotMap =
    {
        new KeyValuePair<string, ETypeItem>("Left/Firs",   ETypeItem.Weapon),
        new KeyValuePair<string, ETypeItem>("Left/Second", ETypeItem.Armor),
        new KeyValuePair<string, ETypeItem>("Left/Third",  ETypeItem.Necklace),
        new KeyValuePair<string, ETypeItem>("Right/Firs",  ETypeItem.Belt),
        new KeyValuePair<string, ETypeItem>("Right/Second",ETypeItem.Gloves),
        new KeyValuePair<string, ETypeItem>("Right/Third", ETypeItem.Shoes),
    };

    private static readonly Color EquippedTint = new Color(0.4f, 0.9f, 0.45f, 1f);
    private static readonly Color EmptyTint = new Color(1f, 1f, 1f, 1f);

    private SV_ItemCatalog _catalog;
    private SV_PlayerInventory _inv;
    private readonly List<KeyValuePair<Transform, ETypeItem>> _slots = new();
    private readonly List<Transform> _cells = new();
    private bool _subscribed;

    public void Build(Transform root, Action onClose)
    {
        _catalog = SV_ItemCatalog.Load();
        _inv = SV_PlayerInventory.Instance;

        var half = FindDeep(root, "HalfPlayerShow");
        if (half != null)
        {
            // 6 equip slots
            foreach (var map in SlotMap)
            {
                var slot = half.Find(map.Key);
                if (slot == null) continue;
                _slots.Add(new KeyValuePair<Transform, ETypeItem>(slot, map.Value));
                var type = map.Value;
                var btn = slot.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => _inv.Unequip(type));
                }
            }

            // item grid cells
            var grid = FindDeep(half, "WeaponIcons");
            if (grid != null)
                for (int i = 0; i < grid.childCount; i++)
                {
                    var cell = grid.GetChild(i);
                    if (cell.GetComponent<Button>() != null) _cells.Add(cell);
                }
        }

        SV_UIKit.CloseButton(root, SV_UIKit.DefaultFont(), onClose);

        Refresh();
        _inv.OnChanged += Refresh;
        _subscribed = true;
    }

    public void OnShow() => Refresh();

    public void Dispose()
    {
        if (!_subscribed) return;
        if (_inv != null) _inv.OnChanged -= Refresh;
        _subscribed = false;
    }

    private void Refresh()
    {
        // Slots
        foreach (var kv in _slots)
        {
            var slot = kv.Key;
            var id = _inv.GetEquipped(kv.Value);
            var entry = id != null && _catalog != null ? _catalog.GetById(id) : null;
            SetLabel(slot, entry != null ? entry.displayName : kv.Value.ToString());
            Tint(slot, entry != null ? EquippedTint : EmptyTint);
        }

        // Grid = owned items
        var owned = new List<SV_ItemEntry>();
        if (_catalog != null)
            foreach (var e in _catalog.Entries)
                if (e != null && _inv.IsOwned(e.id)) owned.Add(e);

        for (int i = 0; i < _cells.Count; i++)
        {
            var cell = _cells[i];
            var btn = cell.GetComponent<Button>();
            if (i < owned.Count)
            {
                var entry = owned[i];
                bool equipped = _inv.IsEquipped(entry.id);
                cell.gameObject.SetActive(true);
                SetLabel(cell, entry.displayName);
                Tint(cell, equipped ? EquippedTint : EmptyTint);
                if (btn != null)
                {
                    btn.interactable = true;
                    btn.onClick.RemoveAllListeners();
                    var e = entry;
                    btn.onClick.AddListener(() =>
                    {
                        if (_inv.IsEquipped(e.id)) _inv.Unequip(e.slot);
                        else _inv.Equip(e);
                    });
                }
            }
            else
            {
                // Empty grid cell — blank + non-interactive.
                SetLabel(cell, "");
                if (btn != null) { btn.onClick.RemoveAllListeners(); btn.interactable = false; }
            }
        }
    }

    private static void SetLabel(Transform node, string value)
    {
        var t = node.Find("Text (Legacy)");
        var tx = t != null ? t.GetComponent<Text>() : node.GetComponentInChildren<Text>();
        if (tx != null) tx.text = value;
    }

    private static void Tint(Transform node, Color c)
    {
        var icon = node.Find("Icon");
        var img = icon != null ? icon.GetComponent<Image>() : node.GetComponent<Image>();
        if (img != null) img.color = c;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }
}
