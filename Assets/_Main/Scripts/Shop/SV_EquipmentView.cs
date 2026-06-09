using System;
using System.Collections.Generic;
using Luzart;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the Equipment screen by binding existing prefab visuals, mirroring IO_Training's
/// SlotItemEquipmentView + ItemViewWithLevelInventory structure:
///  - the 6 character slots (Left|Right/Firs|Second|Third → ETypeItem) each own an SV_ItemView;
///    on refresh a slot is Setup(item, level) when equipped or SetEmpty() when not. Tapping an
///    equipped slot opens SV_ItemDetailPopup.
///  - the inventory grid (WeaponIcons) is spawned from one Cnte TEMPLATE, one cell per owned item,
///    each bound through its own SV_ItemView. Tapping a cell opens SV_ItemDetailPopup.
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

    private sealed class Slot
    {
        public ETypeItem Type;
        public SV_ItemView View;
    }

    private Transform _root;
    private SV_ItemCatalog _catalog;
    private SV_PlayerInventory _inv;
    private readonly List<Slot> _slots = new();
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
            BindSlots(half);
            BindGridTemplate(FindDeep(half, "WeaponIcons"));
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

    // ── setup ───────────────────────────────────────────────────────
    private void BindSlots(Transform half)
    {
        foreach (var map in SlotMap)
        {
            var node = half.Find(map.Key);
            if (node == null) continue;
            _slots.Add(new Slot { Type = map.Value, View = new SV_ItemView(node, _catalog) });

            var type = map.Value;
            var btn = node.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OpenDetail(_inv.GetEquipped(type)));
            }
        }
    }

    private void BindGridTemplate(Transform grid)
    {
        if (grid == null) return;
        _gridParent = grid;
        for (int i = 0; i < grid.childCount; i++)
        {
            var c = grid.GetChild(i);
            if (_gridTemplate == null && c.GetComponent<Button>() != null) _gridTemplate = c.gameObject;
            c.gameObject.SetActive(false); // hide pre-placed mockup cells
        }
    }

    // ── refresh ──────────────────────────────────────────────────────
    private void Refresh()
    {
        RefreshSlots();
        RefreshGrid();
    }

    private void RefreshSlots()
    {
        foreach (var slot in _slots)
        {
            var entry = Lookup(_inv.GetEquipped(slot.Type));
            if (entry != null) slot.View.Setup(entry, _inv.GetLevel(entry.id));
            else slot.View.SetEmpty();
        }
    }

    private void RefreshGrid()
    {
        for (int i = _spawned.Count - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(_spawned[i]);
        _spawned.Clear();

        if (_gridTemplate == null || _catalog == null) return;

        foreach (var entry in _catalog.Entries)
        {
            if (entry == null || !_inv.IsOwned(entry.id)) continue;

            var cell = UnityEngine.Object.Instantiate(_gridTemplate, _gridParent);
            cell.SetActive(true);
            _spawned.Add(cell);

            new SV_ItemView(cell.transform, _catalog).Setup(entry, _inv.GetLevel(entry.id));

            var captured = entry;
            var btn = cell.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => SV_ItemDetailPopup.Show(_root, captured));
            }
        }
    }

    private void OpenDetail(string itemId)
    {
        var entry = Lookup(itemId);
        if (entry != null) SV_ItemDetailPopup.Show(_root, entry);
    }

    private SV_ItemEntry Lookup(string id) =>
        id != null && _catalog != null ? _catalog.GetById(id) : null;

    private static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }
}
