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

        // Wire remaining DownContainer nav buttons (Death/Evolve) → corresponding SV_*UI screens.
        WireExtraNav("Death", UIId.SV_Process);
        WireExtraNav("Evolve", UIId.SV_Evolve);
        return UniTask.CompletedTask;
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
        Debug.Log("[DBG-CHAIN] A: SV_MainMenuUI.OnPlay clicked");
        // 1) Kick legacy DATN gameplay flow (sets Bool.GameStart, activates weapons, etc.)
        var legacyUIManager = FindObjectOfType<DATN.Legacy.UIManager>();
        if (legacyUIManager != null) legacyUIManager.PlayBtn();

        // 2) Start the framework gameplay loop via the ClassicMode state machine (single entry
        //    door). ClassicMode.StartGame sets state=Playing and calls GameController.StartGameplay.
        if (Luzart.SceneRootManager.Instance != null)
        {
            var domain = Luzart.SceneRootManager.Instance.Domain;
            var classicMode = domain?.Get<Luzart.ClassicModeController>();
            Debug.Log($"[DBG-CHAIN] A: domain={(domain==null?"null":"OK")}, classicMode={(classicMode==null?"null":"OK")}");
            if (classicMode != null) classicMode.StartGame();
            else domain?.Get<Luzart.GameController>()?.StartGameplay(); // fallback if not wired
        }
        else Debug.LogWarning("[DBG-CHAIN] A: SceneRootManager.Instance is NULL");

        // 3) Hide MainMenu, show GameplayHud.
        await Luzart.UIManager.Instance.ShowAsync(Luzart.UIId.SV_GameplayHud,
            ct: this.GetCancellationTokenOnDestroy());
        OnCloseButtonClicked(); // close MainMenu
    }

    private async void OnShop() => await SafeShowAsync(UIId.SV_Shop);
    private async void OnEquipment() => await SafeShowAsync(UIId.SV_ItemEquipment);
    private async void OnSettings() => await SafeShowAsync(UIId.SV_SettingsPopup);

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
