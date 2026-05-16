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
        if (btnResume != null) btnResume.onClick.AddListener(OnResume);
        if (btnSettings != null) btnSettings.onClick.AddListener(OnSettings);
        if (btnMainMenu != null) btnMainMenu.onClick.AddListener(OnMainMenu);
        return UniTask.CompletedTask;
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
        // Close pause + back to main menu.
        Time.timeScale = 1f;
        OnCloseButtonClicked();
        await UIManager.Instance.HideAllExceptSystemAsync();
        await UIManager.Instance.ShowAsync(UIId.SV_MainMenu, ct: this.GetCancellationTokenOnDestroy());
    }
}
