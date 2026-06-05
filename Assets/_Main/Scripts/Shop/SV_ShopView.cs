using System;
using System.Collections.Generic;
using Luzart;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Binds buy logic onto the EXISTING "Daily Shop" cards in the SV_Shop mockup (no generated
/// panels). Each of the 6 cards is repurposed to sell one catalog item: its Name + price text
/// are driven from the catalog, and its Button buys the item with coins (CurrencyManager) +
/// marks it owned (SV_PlayerInventory). A small Close (X) button is added because the mockup
/// has none.
/// </summary>
public class SV_ShopView
{
    // Existing card GameObject names under "Daily Shop", in display order.
    private static readonly string[] CardNames =
        { "Gems", "Weapon Design", "Bone Pendant", "BoneM", "Clothing Design", "Shoes Design" };

    private SV_ItemCatalog _catalog;
    private SV_PlayerInventory _inv;
    private readonly List<KeyValuePair<Transform, SV_ItemEntry>> _cards = new();
    private bool _subscribed;

    public void Build(Transform root, Action onClose)
    {
        _catalog = SV_ItemCatalog.Load();
        _inv = SV_PlayerInventory.Instance;

        var daily = FindDeep(root, "Daily Shop");
        if (daily != null && _catalog != null)
        {
            var entries = _catalog.Entries;
            for (int i = 0; i < CardNames.Length && i < entries.Count; i++)
            {
                var card = daily.Find(CardNames[i]);
                if (card == null) continue;
                var entry = entries[i];
                _cards.Add(new KeyValuePair<Transform, SV_ItemEntry>(card, entry));
                WireCard(card, entry);
            }
        }

        // Close button — the mockup has no close affordance.
        SV_UIKit.CloseButton(root, SV_UIKit.DefaultFont(), onClose);

        Refresh();
        CurrencyManager.Instance.OnCoinChanged += OnCoin;
        _inv.OnChanged += Refresh;
        _subscribed = true;
    }

    public void OnShow() => Refresh();

    public void Dispose()
    {
        if (!_subscribed) return;
        if (CurrencyManager.Instance != null) CurrencyManager.Instance.OnCoinChanged -= OnCoin;
        if (_inv != null) _inv.OnChanged -= Refresh;
        _subscribed = false;
    }

    private void OnCoin(long _) => Refresh();

    private void WireCard(Transform card, SV_ItemEntry entry)
    {
        SetText(card.Find("Name"), entry.displayName);
        SetText(FindPriceText(card), entry.priceCoins.ToString());

        var btn = card.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => _inv.TryBuy(entry));
        }
    }

    private void Refresh()
    {
        long coins = CurrencyManager.Instance.Coins;
        foreach (var kv in _cards)
        {
            var card = kv.Key;
            var entry = kv.Value;
            bool owned = _inv.IsOwned(entry.id);

            SetText(card.Find("Name"), owned ? entry.displayName + " (Owned)" : entry.displayName);
            SetText(FindPriceText(card), owned ? "OWNED" : entry.priceCoins.ToString());

            var btn = card.GetComponent<Button>();
            if (btn != null) btn.interactable = !owned && coins >= entry.priceCoins;

            // "Done" overlay child (a check/sold mark in the mockup) reflects owned state.
            var done = card.Find("Done");
            if (done != null) done.gameObject.SetActive(owned);
        }
    }

    // Prefer a child named like "Coins" (the coin-price text); otherwise the gem text under Icon.
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

    private static void SetText(Transform t, string value)
    {
        if (t == null) return;
        var tx = t.GetComponent<Text>() ?? t.GetComponentInChildren<Text>();
        if (tx != null) tx.text = value;
    }

    private static void SetText(Text tx, string value)
    {
        if (tx != null) tx.text = value;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }
}
