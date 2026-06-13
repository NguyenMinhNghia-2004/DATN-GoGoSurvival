using System.Threading;
using Cysharp.Threading.Tasks;
using Luzart;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// NinjaUI wrapper cho Main Menu screen của DATN.
/// Lane: Screen. Cache: KeepLoaded (mở thường xuyên).
///
/// Replaces: <c>MainMenu.cs</c> + <c>MainMenuManager.cs</c>.
/// Setup wiring:
///   - Drag MainMenu prefab vào UIRegistrySO entry `SV_MainMenu`.
///   - Trên prefab root, replace MainMenu/MainMenuManager components với class này.
///   - Kéo button references vào field tương ứng.
/// </summary>
public class SV_MainMenuUI : UIBase
{
    [Header("Top-level buttons")]
    [SerializeField] private Button btnPlay;
    [SerializeField] private Button btnShop;
    [SerializeField] private Button btnEquipment;
    [SerializeField] private Button btnSettings;
    [SerializeField] private Button btnMessages;

    [Header("Sub-panels (legacy compat, nếu cần)")]
    [SerializeField] private GameObject panelSettings;
    [SerializeField] private GameObject panelMessages;

    public override UniTask OnCreateAsync(UIContext ctx, CancellationToken ct)
    {
        // Defensive: disable every Inspector-wired onClick listener whose target is null.
        // Many legacy buttons in this prefab were authored against the old DATN.Legacy
        // hierarchy; after migration their targets resolve to null and clicking them
        // throws NullReferenceException. SanitizeButtons disables (state=Off) those
        // broken listeners across the whole MainMenu hierarchy so the only listeners
        // that fire are the runtime ones we add below.
        UIButtonSanitizer.SanitizeChildButtons(transform);

        // Auto-find buttons by name. Tolerant to spaces and case so prefab renames
        // don't silently break wiring.
        if (btnPlay == null) btnPlay = FindChildButton("BtnPlay") ?? FindChildButton("Battle");
        if (btnShop == null) btnShop = FindChildButton("Shop");
        if (btnEquipment == null) btnEquipment = FindChildButton("Equipement") ?? FindChildButton("Equipment");
        if (btnSettings == null) btnSettings = FindChildButton("Setting") ?? FindChildButton("Settings");
        if (btnMessages == null) btnMessages = FindChildButton("Messaging") ?? FindChildButton("Messages") ?? FindChildButton("Mails");

        if (btnPlay != null) btnPlay.onClick.AddListener(OnPlay);
        if (btnShop != null) btnShop.onClick.AddListener(OnShop);
        if (btnEquipment != null) btnEquipment.onClick.AddListener(OnEquipment);
        if (btnSettings != null) btnSettings.onClick.AddListener(OnSettings);
        if (btnMessages != null) btnMessages.onClick.AddListener(OnMessages);

        // The middle DownContainer button is "Battle" (a separate GO from the big "BtnPlay"
        // Start button, which btnPlay resolved to). Wire it as the HOME/close button: tapping it
        // closes whatever Shop/Equipment screen is open and returns to the menu.
        var navHome = FindChildButton("Battle");
        if (navHome != null && navHome != btnPlay) navHome.onClick.AddListener(OnNavHome);

        // Wire remaining DownContainer nav buttons (Death/Evolve) → corresponding SV_*UI screens.
        WireExtraNav("Death", UIId.SV_Process);
        WireExtraNav("Evolve", UIId.SV_Evolve);

        // The menu prefab embeds "Shop" and "Equipement" SECTIONS as children that ship ACTIVE
        // (they stack on top of the "MainMenu" home → boot would show the equipment view). Hide
        // them so boot lands on the home; the nav opens the standalone SV_Shop/SV_Equipement
        // screens instead.
        HideEmbeddedScreenSections();

        // Keep the bottom nav (DownContainer) rendering ON TOP of the Shop/Equipment screens so
        // it stays tappable while a screen is open (those screens otherwise cover the menu).
        RaiseNavOnTop();

        // Show + live-update the shop gold balance (the menu's authored currency art is static).
        if (GetComponent<SV_GoldDisplay>() == null) gameObject.AddComponent<SV_GoldDisplay>();
        return UniTask.CompletedTask;
    }

    /// <summary>Hide the menu's embedded Shop/Equipement sections so the home (MainMenu) shows on
    /// boot. These are redundant with the standalone SV_Shop/SV_Equipement screens the nav opens.</summary>
    private void HideEmbeddedScreenSections()
    {
        foreach (Transform c in transform)
            if (c.name == "Shop" || c.name == "Equipement")
                c.gameObject.SetActive(false);
    }

    /// <summary>Put the DownContainer on its own sorting canvas above the screen popups so the
    /// nav stays tappable while Shop/Equipment are open. Wrapped defensively — must never break
    /// the menu show.</summary>
    private void RaiseNavOnTop()
    {
        try
        {
            Transform down = null;
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == "DownContainer") { down = t; break; }
            if (down == null) return; // only raise a real nav container; never the whole menu
            var go = down.gameObject;
            var canvas = go.GetComponent<Canvas>();
            if (canvas == null) canvas = go.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 500;
            if (go.GetComponent<GraphicRaycaster>() == null) go.AddComponent<GraphicRaycaster>();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SV_MainMenuUI] RaiseNavOnTop skipped: {e.Message}");
        }
    }

    private Button FindChildButton(string name)
    {
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (t.name == name)
            {
                var b = t.GetComponent<Button>();
                if (b != null) return b;
            }
        return null;
    }

    private void WireExtraNav(string buttonName, UIId target)
    {
        var b = FindChildButton(buttonName);
        if (b != null) b.onClick.AddListener(async () =>
        {
            await UIManager.Instance.ShowAsync(target, ct: this.GetCancellationTokenOnDestroy());
        });
    }

    private async void OnPlay()
    {
        // Middle (Battle) button doubles as "close the open screen": if a Shop/Equipment screen is
        // currently visible, tap it to return to the menu instead of starting a run.
        if (await TryCloseOpenScreens()) return;

        // 1) Kick legacy DATN gameplay flow (sets Bool.GameStart, activates weapons, etc.)
        var legacyUIManager = FindObjectOfType<DATN.Legacy.UIManager>();
        if (legacyUIManager != null) legacyUIManager.PlayBtn();

        // 2) Start the framework gameplay loop via the ClassicMode state machine (single entry
        //    door). ClassicMode.StartGame sets state=Playing and calls GameController.StartGameplay.
        if (Luzart.SceneRootManager.Instance != null)
        {
            var domain = Luzart.SceneRootManager.Instance.Domain;
            var classicMode = domain?.Get<Luzart.ClassicModeController>();
            if (classicMode != null) classicMode.StartGame();
            else domain?.Get<Luzart.GameController>()?.StartGameplay(); // fallback if not wired
        }

        // 3) Hide MainMenu, show GameplayHud.
        await Luzart.UIManager.Instance.ShowAsync(Luzart.UIId.SV_GameplayHud,
            ct: this.GetCancellationTokenOnDestroy());
        OnCloseButtonClicked(); // close MainMenu
    }

    /// <summary>Middle "Battle" nav button → close any open Shop/Equipment screen (return home).</summary>
    private async void OnNavHome() => await TryCloseOpenScreens();

    // Faithful IO_Training port: nav opens the PopupService-driven screens via IOPortBootstrap
    // instead of the old SV_Shop/SV_Equipement NinjaUI screens.
    private void OnShop() => GetIOPort()?.OpenShop();
    private void OnEquipment() => GetIOPort()?.OpenEquipment();

    private static Luzart.IOPortBootstrap GetIOPort()
    {
        var srm = Luzart.SceneRootManager.Instance;
        return srm != null && srm.Domain != null ? srm.Domain.GetFirst<Luzart.IOPortBootstrap>() : null;
    }
    private async void OnSettings() => await SafeShowAsync(UIId.SV_SettingsPopup);

    /// <summary>Tap a nav button to open its screen; tap again (or the middle Battle button) to
    /// close. Toggle is driven by ACTUAL screen visibility (robust against async timing / multiple
    /// menu instances) rather than a tracked flag.</summary>
    private async UniTask ToggleScreen(UIId id)
    {
        bool isShop = id == UIId.SV_Shop;
        bool alreadyVisible = isShop ? IsScreenVisible<SV_ShopUI>() : IsScreenVisible<SV_EquipementUI>();
        if (alreadyVisible) { await SafeHide(id); return; }

        await TryCloseOpenScreens(); // only one of Shop/Equipment open at a time
        try
        {
            await UIManager.Instance.ShowAsync(id, ct: this.GetCancellationTokenOnDestroy());
        }
        catch (System.Collections.Generic.KeyNotFoundException)
        {
            Debug.LogWarning($"[SV_MainMenuUI] {id} has no UIRegistry entry — button click ignored.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SV_MainMenuUI] {id} show failed: {e.Message}");
        }
        RaiseNavOnTop(); // keep nav tappable above the newly shown screen
    }

    /// <summary>Hide whichever Shop/Equipment screen is currently visible. Returns true if any
    /// were closed (used by the Battle button to return to the menu).</summary>
    private async UniTask<bool> TryCloseOpenScreens()
    {
        bool closed = false;
        if (IsScreenVisible<SV_ShopUI>()) { await SafeHide(UIId.SV_Shop); closed = true; }
        if (IsScreenVisible<SV_EquipementUI>()) { await SafeHide(UIId.SV_ItemEquipment); closed = true; }
        // Also close any open IO_Training-port popup.
        GetIOPort()?.CloseAllPopups();
        return closed;
    }

    private static bool IsScreenVisible<T>() where T : UIBase
    {
        foreach (var u in Resources.FindObjectsOfTypeAll<T>())
            if (u != null && u.gameObject.scene.IsValid() && u.IsVisible) return true;
        return false;
    }

    private async UniTask SafeHide(UIId id)
    {
        try { await UIManager.Instance.HideAsync(id); } catch { }
    }

    private void OnMessages()
    {
        // Messages panel — keep legacy SetActive pattern for now.
        if (panelMessages != null) panelMessages.SetActive(true);
    }

    /// <summary>Show a UIId, swallowing the "no config" exception when the UIRegistry doesn't
    /// have a matching entry yet. Lets MainMenu buttons stay clickable without throwing while
    /// new screens are being authored.</summary>
    private async UniTask SafeShowAsync(UIId id)
    {
        try { await UIManager.Instance.ShowAsync(id, ct: this.GetCancellationTokenOnDestroy()); }
        catch (System.Collections.Generic.KeyNotFoundException) {
            Debug.LogWarning($"[SV_MainMenuUI] {id} has no UIRegistry entry — screen not yet authored. Button click ignored.");
        }
        catch (System.Exception e) {
            Debug.LogError($"[SV_MainMenuUI] {id} show failed: {e.Message}");
        }
    }

    public override bool HandleEscape()
    {
        // Don't dismiss MainMenu via ESC — instead show settings/quit confirm.
        return false;
    }
}
