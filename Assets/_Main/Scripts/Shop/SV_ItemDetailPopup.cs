using System;
using Luzart;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Item detail popup (the "UIDetailItem" the user asked for). There is no prefab for this in the
/// project, so it is built at runtime over the calling screen. Modeled on IO_Training's
/// PopupItemEquipView: shows icon + rarity frame, name, stat description, an Equip/UnEquip toggle,
/// and an Upgrade button with its cost (coins + rarity shards). Opened from the Equipment slots /
/// inventory grid and from the Shop unlock cards.
/// </summary>
public class SV_ItemDetailPopup
{
    private static SV_ItemDetailPopup _open;

    private GameObject _root;
    private SV_ItemEntry _entry;
    private SV_PlayerInventory _inv;
    private SV_ItemCatalog _catalog;
    private Font _font;
    private bool _shopMode; // true = opened from shop (offer Unlock instead of Equip)

    private Image _frame, _icon;
    private Text _name, _info, _desc, _shardLabel, _costLabel;
    private Button _primaryBtn; private Text _primaryLabel;     // Equip/Unequip OR Unlock
    private Button _upgradeBtn; private Text _upgradeLabel;

    public static void Show(Transform parent, SV_ItemEntry entry, bool shopMode = false)
    {
        Close();
        if (parent == null || entry == null) return;
        _open = new SV_ItemDetailPopup();
        _open.Build(parent, entry, shopMode);
    }

    public static void Close()
    {
        if (_open != null) { _open.Dispose(); _open = null; }
    }

    private void Build(Transform parent, SV_ItemEntry entry, bool shopMode)
    {
        _entry = entry; _shopMode = shopMode;
        _inv = SV_PlayerInventory.Instance;
        _catalog = SV_ItemCatalog.Load();
        _font = SV_UIKit.DefaultFont();

        _root = new GameObject("SV_ItemDetailPopup", typeof(RectTransform));
        _root.layer = LayerMask.NameToLayer("UI");
        var rrt = _root.GetComponent<RectTransform>();
        rrt.SetParent(parent, false);
        rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
        rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
        rrt.SetAsLastSibling();

        SV_UIKit.Backdrop(rrt, new Color(0, 0, 0, 0.72f), Close);

        var rarityCol = _catalog != null ? _catalog.GetRarityColor(entry.rarity) : Color.gray;
        var panel = SV_UIKit.Panel(rrt, "Panel", new Vector2(660, 860), new Color(0.13f, 0.14f, 0.18f, 0.99f));

        // Close X
        var x = SV_UIKit.TextButton(panel, "X", new Vector2(60, 60), new Color(0.8f, 0.2f, 0.2f), Color.white, _font, Close);
        var xrt = (RectTransform)x.transform;
        xrt.anchorMin = xrt.anchorMax = new Vector2(1, 1); xrt.pivot = new Vector2(1, 1);
        xrt.anchoredPosition = new Vector2(-12, -12);

        // Icon + rarity frame
        _frame = SV_UIKit.Picture(panel, "Frame", new Vector2(220, 220), _catalog?.GetRarityFrame(entry.rarity), rarityCol);
        var frt = (RectTransform)_frame.transform;
        frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 1f); frt.pivot = new Vector2(0.5f, 1f);
        frt.anchoredPosition = new Vector2(0, -28);
        _icon = SV_UIKit.Picture(_frame.transform, "Icon", new Vector2(170, 170), entry.icon, Color.white);
        ((RectTransform)_icon.transform).anchoredPosition = Vector2.zero;

        _name = SV_UIKit.Label(panel, entry.displayName, 40, TextAnchor.UpperCenter, Color.white, _font);
        SV_UIKit.SetAnchoredTop(_name.rectTransform, 50, -262);

        _info = SV_UIKit.Label(panel, "", 24, TextAnchor.UpperCenter, new Color(0.8f, 0.85f, 0.9f), _font);
        SV_UIKit.SetAnchoredTop(_info.rectTransform, 34, -318);

        _desc = SV_UIKit.Label(panel, "", 28, TextAnchor.UpperCenter, new Color(0.85f, 0.95f, 0.85f), _font);
        SV_UIKit.SetAnchoredTop(_desc.rectTransform, 120, -362);

        _shardLabel = SV_UIKit.Label(panel, "", 22, TextAnchor.UpperCenter, new Color(1f, 0.85f, 0.4f), _font);
        SV_UIKit.SetAnchoredTop(_shardLabel.rectTransform, 30, -496);

        _costLabel = SV_UIKit.Label(panel, "", 22, TextAnchor.UpperCenter, new Color(0.9f, 0.9f, 0.9f), _font);
        SV_UIKit.SetAnchoredTop(_costLabel.rectTransform, 30, -528);

        // Primary button (Equip/Unequip or Unlock) bottom-left, Upgrade bottom-right.
        _primaryBtn = SV_UIKit.TextButton(panel, "", new Vector2(290, 96), new Color(0.2f, 0.5f, 0.75f), Color.white, _font, OnPrimary);
        var prt = (RectTransform)_primaryBtn.transform;
        prt.anchorMin = prt.anchorMax = new Vector2(0, 0); prt.pivot = new Vector2(0, 0);
        prt.anchoredPosition = new Vector2(24, 28);
        _primaryLabel = _primaryBtn.GetComponentInChildren<Text>();

        _upgradeBtn = SV_UIKit.TextButton(panel, "", new Vector2(290, 96), new Color(0.2f, 0.6f, 0.3f), Color.white, _font, OnUpgrade);
        var urt = (RectTransform)_upgradeBtn.transform;
        urt.anchorMin = urt.anchorMax = new Vector2(1, 0); urt.pivot = new Vector2(1, 0);
        urt.anchoredPosition = new Vector2(-24, 28);
        _upgradeLabel = _upgradeBtn.GetComponentInChildren<Text>();

        _inv.OnChanged += Refresh;
        Refresh();
    }

    private void Dispose()
    {
        if (_inv != null) _inv.OnChanged -= Refresh;
        if (_root != null) UnityEngine.Object.Destroy(_root);
        _root = null;
    }

    private void Refresh()
    {
        if (_root == null) return;
        int level = _inv.GetLevel(_entry.id);
        bool owned = _inv.IsOwned(_entry.id);
        bool equipped = _inv.IsEquipped(_entry.id);

        _info.text = $"{_entry.slot}  ·  {_entry.rarity}  ·  Lv.{level}";
        _desc.text = StatLine(_entry, level) + (owned && !_inv.IsMaxLevel(_entry)
            ? $"\n<size=18><color=#9fd>Next: {StatLine(_entry, level + 1)}</color></size>" : "");
        _shardLabel.text = $"{_entry.rarity} shards: {_inv.GetShards(_entry.rarity)}    Coins: {CurrencyManager.FormatNumber(CurrencyManager.Instance.Coins)}";

        // Primary button
        if (_shopMode && !owned)
        {
            _primaryLabel.text = $"UNLOCK ({_entry.priceCoins})";
            _primaryBtn.interactable = CurrencyManager.Instance.Coins >= _entry.priceCoins;
            _primaryBtn.image.color = _primaryBtn.interactable ? new Color(0.2f, 0.6f, 0.3f) : new Color(0.4f, 0.4f, 0.4f);
        }
        else
        {
            _primaryLabel.text = equipped ? "UNEQUIP" : "EQUIP";
            _primaryBtn.interactable = owned;
            _primaryBtn.image.color = equipped ? new Color(0.6f, 0.35f, 0.2f) : new Color(0.2f, 0.5f, 0.75f);
        }

        // Upgrade button (only meaningful for owned items)
        bool maxed = _inv.IsMaxLevel(_entry);
        _upgradeBtn.gameObject.SetActive(owned && !_shopMode);
        if (owned && !_shopMode)
        {
            if (maxed)
            {
                _upgradeLabel.text = "MAX";
                _upgradeBtn.interactable = false;
                _upgradeBtn.image.color = new Color(0.4f, 0.4f, 0.4f);
                _costLabel.text = "Max level reached";
            }
            else
            {
                _upgradeLabel.text = "UPGRADE";
                bool can = _inv.CanUpgrade(_entry);
                _upgradeBtn.interactable = can;
                _upgradeBtn.image.color = can ? new Color(0.2f, 0.6f, 0.3f) : new Color(0.4f, 0.4f, 0.4f);
                _costLabel.text = $"Cost: {SV_PlayerInventory.UpgradeCoinCost(level)} coin + {SV_PlayerInventory.UpgradeShardCost(level)} {_entry.rarity} shard";
            }
        }
        else _costLabel.text = "";
    }

    private void OnPrimary()
    {
        if (_shopMode && !_inv.IsOwned(_entry.id))
        {
            if (_inv.TryBuy(_entry)) Refresh();
            return;
        }
        if (_inv.IsEquipped(_entry.id)) _inv.Unequip(_entry.slot);
        else _inv.Equip(_entry);
        Refresh();
    }

    private void OnUpgrade()
    {
        if (_inv.TryUpgrade(_entry)) Refresh();
    }

    private static string StatLine(SV_ItemEntry e, int level)
    {
        double amt = SV_PlayerInventory.EffectiveStatAmount(e, level);
        return e.mode == StatsBehavior.StatBonusMode.Additive
            ? $"{e.statType}  +{amt:0.#}"
            : $"{e.statType}  +{amt * 100:0.#}%";
    }
}
