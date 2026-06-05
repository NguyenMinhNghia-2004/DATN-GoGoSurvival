using System;
using System.Collections.Generic;
using Luzart;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Binds the Equipment screen onto existing prefab visuals, IO_Training style:
///  - the 6 character slots (Left|Right/Firs|Second|Third → ETypeItem) show the equipped item
///    (icon + level + rarity tint) or an empty placeholder (LogoNot). Tapping an equipped slot
///    opens SV_ItemDetailPopup.
///  - the inventory grid (WeaponIcons/Cnte) is driven by a spawner: one Cnte is the TEMPLATE,
///    cloned once per owned item (icon/level/rarity injected); pre-placed mockup cells are hidden.
///    Tapping an owned cell opens SV_ItemDetailPopup.
/// </summary>
public class SV_EquipmentView
{
    private static readonly KeyValuePair<string, ETypeItem>[] SlotMap =
    {
        new KeyValuePair<string, ETypeItem>("Left/Firs",    ETypeItem.Weapon),
        new KeyValuePair<string, ETypeItem>("Left/Second",  ETypeItem.Armor),
        new KeyValuePair<string, ETypeItem>("Left/Third",   ETypeItem.Necklace),
        new KeyValuePair<string, ETypeItem>("Right/Firs",   ETypeItem.Belt),
        new KeyValuePair<string, ETypeItem>("Right/Second", ETypeItem.Gloves),
        new KeyValuePair<string, ETypeItem>("Right/Third",  ETypeItem.Shoes),
    };

    private Transform _root;
    private SV_ItemCatalog _catalog;
    private SV_PlayerInventory _inv;
    private readonly List<KeyValuePair<Transform, ETypeItem>> _slots = new();
    private Transform _gridParent;
    private GameObject _gridTemplate;
    private readonly List<GameObject> _spawned = new();
    private bool _subscribed;

    public void Build(Transform root, Action onClose)
    {
        _root = root;
        _catalog = SV_ItemCatalog.Load();
        _inv = SV_PlayerInventory.Instance;

        var half = FindDeep(root, "HalfPlayerShow");
        if (half != null)
        {
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
                    btn.onClick.AddListener(() => OnSlotClicked(type));
                }
            }

            var grid = FindDeep(half, "WeaponIcons");
            if (grid != null)
            {
                _gridParent = grid;
                for (int i = 0; i < grid.childCount; i++)
                {
                    var c = grid.GetChild(i);
                    if (_gridTemplate == null && c.GetComponent<Button>() != null)
                        _gridTemplate = c.gameObject;
                    c.gameObject.SetActive(false);
                }
            }
        }

        Refresh();
        _inv.OnChanged += Refresh;
        _subscribed = true;
    }

    public void OnShow() => Refresh();

    public void Dispose()
    {
        if (!_subscribed) return;
        if (_inv != null) _inv.OnChanged -= Refresh;
        SV_ItemDetailPopup.Close();
        _subscribed = false;
    }

    private void Refresh()
    {
        RefreshSlots();
        RefreshGrid();
    }

    private void RefreshSlots()
    {
        foreach (var kv in _slots)
        {
            var id = _inv.GetEquipped(kv.Value);
            var entry = id != null && _catalog != null ? _catalog.GetById(id) : null;
            FillCell(kv.Key, entry, kv.Value.ToString());
        }
    }

    private void RefreshGrid()
    {
        for (int i = _spawned.Count - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(_spawned[i]);
        _spawned.Clear();

        if (_gridTemplate == null || _catalog == null) return;

        foreach (var e in _catalog.Entries)
        {
            if (e == null || !_inv.IsOwned(e.id)) continue;
            var cell = UnityEngine.Object.Instantiate(_gridTemplate, _gridParent);
            cell.SetActive(true);
            _spawned.Add(cell);
            FillCell(cell.transform, e, null);
            var entry = e;
            var btn = cell.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => SV_ItemDetailPopup.Show(_root, entry));
            }
        }
    }

    private void OnSlotClicked(ETypeItem type)
    {
        var id = _inv.GetEquipped(type);
        var entry = id != null && _catalog != null ? _catalog.GetById(id) : null;
        if (entry != null) SV_ItemDetailPopup.Show(_root, entry);
    }

    /// <summary>Fill a slot/cell with item data, or show its empty placeholder when entry is null.</summary>
    private void FillCell(Transform node, SV_ItemEntry entry, string emptyLabel)
    {
        bool has = entry != null;
        var icon = node.Find("Icon");
        var logoNot = node.Find("LogoNot");
        var lvl = node.Find("Text (Legacy)");

        if (icon != null)
        {
            icon.gameObject.SetActive(has);
            if (has)
            {
                var img = icon.GetComponent<Image>();
                if (img != null && entry.icon != null) img.sprite = entry.icon;
            }
        }
        if (logoNot != null) logoNot.gameObject.SetActive(!has);

        var frame = node.GetComponent<Image>();
        if (frame != null)
            frame.color = has ? _catalog.GetRarityColor(entry.rarity) : Color.white;

        if (lvl != null)
        {
            var t = lvl.GetComponent<Text>();
            if (has)
            {
                lvl.gameObject.SetActive(true);
                if (t != null) t.text = "Lv." + _inv.GetLevel(entry.id);
            }
            else if (!string.IsNullOrEmpty(emptyLabel))
            {
                lvl.gameObject.SetActive(true);
                if (t != null) t.text = emptyLabel;
            }
            else lvl.gameObject.SetActive(false);
        }
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }
}
