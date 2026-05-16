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
        if (btnPlay != null) btnPlay.onClick.AddListener(OnPlay);
        if (btnShop != null) btnShop.onClick.AddListener(OnShop);
        if (btnEquipment != null) btnEquipment.onClick.AddListener(OnEquipment);
        if (btnSettings != null) btnSettings.onClick.AddListener(OnSettings);
        if (btnMessages != null) btnMessages.onClick.AddListener(OnMessages);
        return UniTask.CompletedTask;
    }

    private async void OnPlay()
    {
        // 1) Kick legacy DATN gameplay flow (sets Bool.GameStart, activates weapons, etc.)
        var legacyUIManager = FindObjectOfType<DATN.Legacy.UIManager>();
        if (legacyUIManager != null) legacyUIManager.PlayBtn();

        // 2) Start the framework gameplay loop (waves, XP, level-up).
        //    Without this, GameController would tick from scene load — but we deferred it.
        if (Luzart.SceneRootManager.Instance != null)
        {
            var gc = Luzart.SceneRootManager.Instance.Domain?.Get<Luzart.GameController>();
            gc?.StartGameplay();
        }

        // 3) Hide MainMenu, show GameplayHud.
        await Luzart.UIManager.Instance.ShowAsync(Luzart.UIId.SV_GameplayHud,
            ct: this.GetCancellationTokenOnDestroy());
        OnCloseButtonClicked(); // close MainMenu
    }

    private async void OnShop()
    {
        await UIManager.Instance.ShowAsync(UIId.SV_Shop, ct: this.GetCancellationTokenOnDestroy());
    }

    private async void OnEquipment()
    {
        await UIManager.Instance.ShowAsync(UIId.SV_ItemEquipment, ct: this.GetCancellationTokenOnDestroy());
    }

    private async void OnSettings()
    {
        await UIManager.Instance.ShowAsync(UIId.SV_SettingsPopup, ct: this.GetCancellationTokenOnDestroy());
    }

    private void OnMessages()
    {
        // Messages panel — keep legacy SetActive pattern for now.
        if (panelMessages != null) panelMessages.SetActive(true);
    }

    public override bool HandleEscape()
    {
        // Don't dismiss MainMenu via ESC — instead show settings/quit confirm.
        return false;
    }
}
