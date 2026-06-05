using System;
using Luzart;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds and drives the functional Shop overlay (runtime grid + Close button) on top of the
/// legacy SV_Shop mockup. Plain class so the heavy UI code stays out of the NinjaUI wrapper.
/// Buy flow mirrors IO_Training (show only un-owned items, affordability gating, refresh on
/// purchase) but spends coins via CurrencyManager and tracks ownership via SV_PlayerInventory.
/// </summary>
public class SV_ShopView
{
    private SV_ItemCatalog _catalog;
    private SV_PlayerInventory _inv;
    private Font _font;
    private Text _coinLabel;
    private RectTransform _content;
    private bool _subscribed;

    public void Build(Transform root, Action onClose)
    {
        _catalog = SV_ItemCatalog.Load();
        _inv = SV_PlayerInventory.Instance;
        _font = SV_UIKit.DefaultFont();

        var backdrop = SV_UIKit.Backdrop(root, new Color(0, 0, 0, 0.6f));
        var panel = SV_UIKit.Panel(backdrop.transform, "SV_ShopPanel",
            new Vector2(760, 1040), new Color(0.12f, 0.12f, 0.16f, 0.98f));

        var title = SV_UIKit.Label(panel, "SHOP", 44, TextAnchor.UpperCenter, Color.white, _font);
        AnchorTop(title.rectTransform, 64, -10);

        _coinLabel = SV_UIKit.Label(panel, "", 28, TextAnchor.UpperCenter, new Color(1f, 0.85f, 0.2f), _font);
        AnchorTop(_coinLabel.rectTransform, 36, -70);

        SV_UIKit.CloseButton(panel, _font, onClose);

        // Debug grant so the shop is testable from the menu (coins otherwise only grow in-game).
        var dbg = SV_UIKit.Button(panel, "+500", new Vector2(96, 48),
            new Color(0.25f, 0.4f, 0.7f, 1f), Color.white, _font,
            () => CurrencyManager.Instance.AddCoin(500));
        var drt = (RectTransform)dbg.transform;
        drt.anchorMin = drt.anchorMax = new Vector2(0f, 1f);
        drt.pivot = new Vector2(0f, 1f);
        drt.anchoredPosition = new Vector2(12, -12);

        _content = SV_UIKit.VerticalScroll(panel, new RectOffset(12, 12, 12, 12), 10);

        UpdateCoinLabel(CurrencyManager.Instance.Coins);
        Refresh();

        CurrencyManager.Instance.OnCoinChanged += OnCoinChanged;
        _inv.OnChanged += Refresh;
        _subscribed = true;
    }

    /// <summary>Re-pull coin balance + grid each time the popup is shown (handles cached reopen).</summary>
    public void OnShow()
    {
        if (_content == null) return;
        UpdateCoinLabel(CurrencyManager.Instance.Coins);
        Refresh();
    }

    public void Dispose()
    {
        if (!_subscribed) return;
        if (CurrencyManager.Instance != null) CurrencyManager.Instance.OnCoinChanged -= OnCoinChanged;
        if (_inv != null) _inv.OnChanged -= Refresh;
        _subscribed = false;
    }

    private void OnCoinChanged(long value)
    {
        UpdateCoinLabel(value);
        Refresh(); // affordability may have changed
    }

    private void UpdateCoinLabel(long value) =>
        _coinLabel.text = $"Coins: {CurrencyManager.FormatNumber(value)}";

    private void Refresh()
    {
        if (_content == null) return;
        for (int i = _content.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(_content.GetChild(i).gameObject);

        if (_catalog == null)
        {
            SV_UIKit.Label(_content, "No catalog found (Resources/Shop/SV_ItemCatalog).",
                22, TextAnchor.MiddleCenter, Color.red, _font);
            return;
        }

        long coins = CurrencyManager.Instance.Coins;
        int shown = 0;
        foreach (var e in _catalog.Entries)
        {
            if (e == null || _inv.IsOwned(e.id)) continue;
            shown++;
            BuildCard(e, coins >= e.priceCoins);
        }
        if (shown == 0)
            SV_UIKit.Label(_content, "All items owned!", 24, TextAnchor.MiddleCenter, Color.white, _font);
    }

    private void BuildCard(SV_ItemEntry e, bool affordable)
    {
        var row = SV_UIKit.Row(_content, 100, RarityColor(e.rarity));

        var info = SV_UIKit.Label(row, $"{e.displayName}\n<size=18><color=#cfcfcf>{StatText(e)}</color></size>",
            26, TextAnchor.MiddleLeft, Color.white, _font);
        var infoLE = info.gameObject.AddComponent<LayoutElement>();
        infoLE.flexibleWidth = 1; infoLE.minWidth = 220;

        var price = SV_UIKit.Label(row, $"{e.priceCoins}", 26, TextAnchor.MiddleRight,
            new Color(1f, 0.85f, 0.2f), _font);
        var priceLE = price.gameObject.AddComponent<LayoutElement>();
        priceLE.minWidth = 110; priceLE.preferredWidth = 110;

        var buy = SV_UIKit.Button(row, affordable ? "BUY" : "NEED $",
            new Vector2(150, 70),
            affordable ? new Color(0.2f, 0.65f, 0.25f) : new Color(0.4f, 0.4f, 0.4f),
            Color.white, _font, affordable ? (Action)(() => _inv.TryBuy(e)) : null);
        buy.interactable = affordable;
        var buyLE = buy.gameObject.AddComponent<LayoutElement>();
        buyLE.minWidth = 150; buyLE.preferredWidth = 150;
    }

    private static string StatText(SV_ItemEntry e)
    {
        string slot = e.slot.ToString();
        string stat = e.statType.ToString();
        string amount = e.mode == StatsBehavior.StatBonusMode.Additive
            ? $"+{e.statAmount:0.##}"
            : $"+{e.statAmount * 100:0.#}%";
        return $"{slot} · {stat} {amount}";
    }

    private static Color RarityColor(ERarity r) => r switch
    {
        ERarity.Epic => new Color(0.22f, 0.18f, 0.32f, 1f),
        ERarity.Legend => new Color(0.34f, 0.26f, 0.12f, 1f),
        _ => new Color(0.18f, 0.18f, 0.22f, 1f),
    };

    private static void AnchorTop(RectTransform rt, float height, float y)
    {
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0, height);
        rt.anchoredPosition = new Vector2(0, y);
    }
}
