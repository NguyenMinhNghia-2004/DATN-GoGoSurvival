using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Luzart;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pause popup. Pauses gameplay while shown.
/// Lane: Popup. Cache: PoolOnClose.
/// </summary>
public class SV_PausePopupUI : UIBase
{
    [Header("Refs")]
    [SerializeField] private Button btnResume;
    [SerializeField] private Button btnSettings;
    [SerializeField] private Button btnMainMenu;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Anim")]
    [SerializeField] private float showDuration = 0.18f;
    [SerializeField] private Ease showEase = Ease.OutCubic;

    public override UniTask OnCreateAsync(UIContext ctx, CancellationToken ct)
    {
        UIButtonSanitizer.SanitizeChildButtons(transform);
        // Auto-find by common legacy names if Inspector refs aren't wired.
        if (btnResume == null) btnResume = FindByName("Resume") ?? FindByName("BtnResume");
        if (btnSettings == null) btnSettings = FindByName("Setting") ?? FindByName("Settings") ?? FindByName("BtnSetting");
        if (btnMainMenu == null) btnMainMenu = FindByName("Home") ?? FindByName("MainMenu") ?? FindByName("BtnHome");
        if (btnResume != null) btnResume.onClick.AddListener(OnResume);
        if (btnSettings != null) btnSettings.onClick.AddListener(OnSettings);
        if (btnMainMenu != null) btnMainMenu.onClick.AddListener(OnMainMenu);
        return UniTask.CompletedTask;
    }

    private Button FindByName(string name)
    {
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (t.name.TrimEnd() == name)
            {
                var b = t.GetComponent<Button>();
                if (b != null) return b;
            }
        return null;
    }

    public override UniTask OnBeforeShowAsync(UIContext ctx, CancellationToken ct)
    {
        Time.timeScale = 0f;
        return UniTask.CompletedTask;
    }

    public override async UniTask AnimateShowAsync(bool instant, CancellationToken ct)
    {
        if (canvasGroup == null) return;
        if (instant) { canvasGroup.alpha = 1f; return; }
        canvasGroup.alpha = 0f;
        var tween = canvasGroup.DOFade(1f, showDuration).SetEase(showEase).SetUpdate(true);
        await tween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(ct);
    }

    public override async UniTask AnimateHideAsync(bool instant, CancellationToken ct)
    {
        if (canvasGroup == null) return;
        if (instant) { canvasGroup.alpha = 0f; return; }
        var tween = canvasGroup.DOFade(0f, showDuration).SetEase(showEase).SetUpdate(true);
        await tween.AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(ct);
    }

    public override UniTask OnHiddenAsync(UIHideReason reason, CancellationToken ct)
    {
        Time.timeScale = 1f;
        return UniTask.CompletedTask;
    }

    private void OnResume() => OnCloseButtonClicked();

    private async void OnSettings()
    {
        await UIManager.Instance.ShowAsync(UIId.SV_SettingsPopup, ct: this.GetCancellationTokenOnDestroy());
    }

    private async void OnMainMenu()
    {
        // Quit from pause = abandon the run → funnel through the single end door (counts as a
        // loss). ClassicMode.EndGame stops the sim + despawns enemies, then SV_LoseScreen shows
        // (its Continue returns to MainMenu). This replaces the old "just hide UI + show MainMenu",
        // which left the run still simulating in the background.
        Time.timeScale = 1f;
        OnCloseButtonClicked(); // close the pause popup first
        var classicMode = Luzart.SceneRootManager.Instance?.Domain?.Get<Luzart.ClassicModeController>();
        if (classicMode != null && classicMode.IsPlaying)
        {
            classicMode.EndGame(Luzart.EndReason.QuitFromPause);
            return;
        }
        // Fallback (ClassicMode not wired / not playing): old direct-to-menu behaviour.
        await UIManager.Instance.HideAllExceptSystemAsync();
        await UIManager.Instance.ShowAsync(UIId.SV_MainMenu, ct: this.GetCancellationTokenOnDestroy());
    }
}
