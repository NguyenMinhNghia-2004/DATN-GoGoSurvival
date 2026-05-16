using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Luzart;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SV_LoseData
{
    public int EnemiesKilled;
    public float SurvivalTime;
    public int CoinsEarned;
}

/// <summary>Game over screen.</summary>
public class SV_LoseScreenUI : UIBase<SV_LoseData>
{
    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI killsText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI coinsText;

    [Header("Buttons")]
    [SerializeField] private Button btnRetry;
    [SerializeField] private Button btnMainMenu;

    [Header("Anim")]
    [SerializeField] private CanvasGroup canvasGroup;

    public override UniTask OnCreateAsync(UIContext ctx, CancellationToken ct)
    {
        if (btnRetry != null) btnRetry.onClick.AddListener(OnRetry);
        if (btnMainMenu != null) btnMainMenu.onClick.AddListener(OnMainMenu);
        return UniTask.CompletedTask;
    }

    protected override UniTask OnBeforeShowAsync(SV_LoseData data, CancellationToken ct)
    {
        if (data == null) return UniTask.CompletedTask;
        if (killsText != null) killsText.text = data.EnemiesKilled.ToString();
        if (timeText != null) timeText.text = $"{(int)data.SurvivalTime / 60:00}:{(int)data.SurvivalTime % 60:00}";
        if (coinsText != null) coinsText.text = $"+{data.CoinsEarned}";
        return UniTask.CompletedTask;
    }

    public override async UniTask AnimateShowAsync(bool instant, CancellationToken ct)
    {
        if (canvasGroup == null || instant) { if (canvasGroup != null) canvasGroup.alpha = 1; return; }
        canvasGroup.alpha = 0f;
        await canvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutCubic).AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(ct);
    }

    private async void OnRetry()
    {
        await UIManager.Instance.HideAllExceptSystemAsync();
        // TODO: trigger gameplay restart.
    }

    private async void OnMainMenu()
    {
        await UIManager.Instance.HideAllExceptSystemAsync();
        await UIManager.Instance.ShowAsync(UIId.SV_MainMenu);
    }
}
