using Luzart;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Reusable item-cell view — the GoGo equivalent of IO_Training's ItemViewWithLevelInventory.
/// Binds an item's icon, rarity frame and level onto one cell's known children. Cache the cell's
/// elements once in the constructor, then call Setup(entry, level) (equipped/owned) or SetEmpty().
/// Used by both the 6 equipment slots and the inventory grid cells.
///
/// Cell layout (shared by slot "Firs/Second/Third" and grid "Cnte"):
///   Icon          → item sprite (imIcon)
///   LogoNot       → empty-state placeholder (shown when no item)
///   Text (Legacy) → "Lv.X" (txtLevel)
///   Reward*       → leftover ad-reward badge (hidden; not part of equipment UX)
/// The cell's own Image is the rarity frame (imBg) — set to the assigned rarity sprite when one
/// exists; otherwise left as the prefab default (no harsh colour tint).
/// </summary>
public class SV_ItemView
{
    private readonly Image _icon;
    private readonly Image _frame;
    private readonly GameObject _emptyMark;
    private readonly Text _level;
    private readonly SV_ItemCatalog _catalog;

    public SV_ItemView(Transform cell, SV_ItemCatalog catalog)
    {
        _catalog = catalog;
        _icon = Find<Image>(cell, "Icon");
        _emptyMark = cell.Find("LogoNot")?.gameObject;
        _level = Find<Text>(cell, "Text (Legacy)");
        _frame = cell.GetComponent<Image>();

        // Hide the leftover ad-reward badge — it isn't part of the equipment UX.
        foreach (Transform c in cell)
            if (c.name.StartsWith("Reward")) c.gameObject.SetActive(false);
    }

    public void Setup(SV_ItemEntry entry, int level)
    {
        if (_emptyMark != null) _emptyMark.SetActive(false);
        if (_icon != null)
        {
            _icon.gameObject.SetActive(true);
            if (entry.icon != null) _icon.sprite = entry.icon;
        }
        var frameSprite = _catalog != null ? _catalog.GetRarityFrame(entry.rarity) : null;
        if (_frame != null && frameSprite != null) { _frame.sprite = frameSprite; _frame.color = Color.white; }
        if (_level != null) { _level.gameObject.SetActive(true); _level.text = "Lv." + level; }
    }

    public void SetEmpty()
    {
        if (_icon != null) _icon.gameObject.SetActive(false);
        if (_emptyMark != null) _emptyMark.SetActive(true);
        if (_level != null) _level.gameObject.SetActive(false);
    }

    private static T Find<T>(Transform parent, string child) where T : Component
    {
        var t = parent.Find(child);
        return t != null ? t.GetComponent<T>() : null;
    }
}
