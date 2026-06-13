using System;
using System.Collections.Generic;
using Luzart;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Binds the Shop onto existing prefab visuals, IO_Training style: one "Daily Shop" card is used
/// as a TEMPLATE and cloned once per UN-OWNED catalog item (an "ItemUnlock" card). Each card shows
/// the item's name + unlock price + rarity tint; tapping it opens SV_ItemDetailPopup in shop mode
/// (item info + Unlock button). Once unlocked the card disappears (refreshes on inventory change).
/// Pre-placed mockup cards are hidden.
/// </summary>
public class SV_ShopView
{
    private const string TemplateCardName = "Weapon Design"; // has Name + Coins price + Icon

    private Transform _root;
    private Transform _daily;
    private GameObject _template;
    private SV_ItemCatalog _catalog;
    private SV_PlayerInventory _inv;
    private readonly List<GameObject> _spawned = new();
    private bool _subscribed;

    public void Build(Transform root, Action onClose)
    {
        _root = root;
        _catalog = SV_ItemCatalog.Load();
        _inv = SV_PlayerInventory.Instance;

        _daily = FindDeep(root, "Daily Shop");
        if (_daily != null)
        {
            var tmpl = _daily.Find(TemplateCardName);
            if (tmpl == null) // fallback: first child that is a Button
                for (int i = 0; i < _daily.childCount; i++)
                    if (_daily.GetChild(i).GetComponent<Button>() != null) { tmpl = _daily.GetChild(i); break; }
            if (tmpl != null) _template = tmpl.gameObject;

            // Hide all pre-placed mockup cards (keep the Header).
            for (int i = 0; i < _daily.childCount; i++)
            {
                var c = _daily.GetChild(i);
                if (c.GetComponent<Button>() != null) c.gameObject.SetActive(false);
            }

            SetupGrid();
        }

        Refresh();
        CurrencyManager.Instance.OnCoinChanged += OnCoin;
        _inv.OnChanged += Refresh;
        _subscribed = true;
    }

    /// <summary>Daily Shop has no layout group (cards were hand-placed). Add a GridLayoutGroup so
    /// spawned unlock cards arrange in a grid; keep the Header banner fixed via ignoreLayout; let
    /// the outer ScrollRect handle overflow via a ContentSizeFitter.</summary>
    private void SetupGrid()
    {
        var header = _daily.Find("Header");
        if (header != null)
        {
            var le = header.GetComponent<LayoutElement>() ?? header.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;
        }
        var grid = _daily.GetComponent<GridLayoutGroup>() ?? _daily.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(320, 420);
        grid.spacing = new Vector2(15, 15);
        grid.padding = new RectOffset(45, 45, 140, 24);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.childAlignment = TextAnchor.UpperCenter;
        var fit = _daily.GetComponent<ContentSizeFitter>() ?? _daily.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    public void OnShow() => Refresh();

    public void Dispose()
    {
        if (!_subscribed) return;
        if (CurrencyManager.Instance != null) CurrencyManager.Instance.OnCoinChanged -= OnCoin;
        if (_inv != null) _inv.OnChanged -= Refresh;
        SV_ItemDetailPopup.Close();
        _subscribed = false;
    }

    private void OnCoin(long _) => Refresh();

    private void Refresh()
    {
        for (int i = _spawned.Count - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(_spawned[i]);
        _spawned.Clear();

        if (_template == null || _catalog == null) return;

        long coins = CurrencyManager.Instance.Coins;
        foreach (var e in _catalog.Entries)
        {
            if (e == null || _inv.IsOwned(e.id)) continue; // only un-owned = unlockable
            SpawnCard(e, coins >= e.priceCoins);
        }
    }

    private void SpawnCard(SV_ItemEntry entry, bool affordable)
    {
        var go = UnityEngine.Object.Instantiate(_template, _daily);
        go.SetActive(true);
        _spawned.Add(go);
        var card = go.transform;

        SetText(card.Find("Name"), entry.displayName);
        SetText(FindPriceText(card), entry.priceCoins.ToString());

        // Hide the "20% Off" / ad decorations on the cloned template if present.
        var topleft = card.Find("Topleft"); if (topleft != null) topleft.gameObject.SetActive(false);
        for (int i = 0; i < card.childCount; i++)
            if (card.GetChild(i).name.StartsWith("AdsIcon")) card.GetChild(i).gameObject.SetActive(false);

        // Rarity tint on the card frame.
        var img = card.GetComponent<Image>();
        if (img != null) img.color = affordable ? _catalog.GetRarityColor(entry.rarity)
                                                 : new Color(0.5f, 0.5f, 0.5f, 1f);

        if (entry.icon != null)
        {
            var iconImg = FindIconImage(card);
            if (iconImg != null) iconImg.sprite = entry.icon;
        }

        var btn = go.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            var e = entry;
            btn.onClick.AddListener(() => SV_ItemDetailPopup.Show(_root, e, true));
        }
    }

    private static Text FindPriceText(Transform card)
    {
        foreach (var t in card.GetComponentsInChildren<Transform>(true))
        {
            if (t == card) continue;
            if (t.name.StartsWith("Coins"))
            {
                var tx = t.GetComponent<Text>();
                if (tx != null) return tx;
            }
        }
        var icon = card.Find("Icon");
        return icon != null ? icon.GetComponentInChildren<Text>() : null;
    }

    private static Image FindIconImage(Transform card)
    {
        var icon = card.Find("Icon");
        if (icon != null)
        {
            var img = icon.GetComponent<Image>();
            if (img != null) return img;
            return icon.GetComponentInChildren<Image>();
        }
        return null;
    }

    private static void SetText(Transform t, string value)
    {
        if (t == null) return;
        var tx = t.GetComponent<Text>() ?? t.GetComponentInChildren<Text>();
        if (tx != null) tx.text = value;
    }

    private static void SetText(Text tx, string value) { if (tx != null) tx.text = value; }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }
}
