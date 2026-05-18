using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Luzart;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SV_WinData
{
    public int FinalScore;
    public int EnemiesKilled;
    public float SurvivalTime;
    public int CoinsEarned;
    public int XpEarned;
}

/// <summary>
/// Win screen — show stats + claim rewards.
/// Lane: Screen. Replace any currently-active gameplay screen.
/// </summary>
public class SV_WinScreenUI : UIBase<SV_WinData>
{
    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI killsText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private TextMeshProUGUI xpText;

    [Header("Buttons")]
    [SerializeField] private Button btnContinue;
    [SerializeField] private Button btnRetry;

    [Header("Anim")]
    [SerializeField] private CanvasGroup canvasGroup;

    public override UniTask OnCreateAsync(UIContext ctx, CancellationToken ct)
    {
        UIButtonSanitizer.SanitizeChildButtons(transform);
        if (btnContinue != null) btnContinue.onClick.AddListener(OnContinue);
        if (btnRetry != null) btnRetry.onClick.AddListener(OnRetry);
        return UniTask.CompletedTask;
    }

    protected override UniTask OnBeforeShowAsync(SV_WinData data, CancellationToken ct)
    {
        if (data == null) return UniTask.CompletedTask;
        if (scoreText != null) scoreText.text = data.FinalScore.ToString("N0");
        if (killsText != null) killsText.text = data.EnemiesKilled.ToString();
        if (timeText != null) timeText.text = $"{(int)data.SurvivalTime / 60:00}:{(int)data.SurvivalTime % 60:00}";
        if (coinsText != null) coinsText.text = $"+{data.CoinsEarned}";
        if (xpText != null) xpText.text = $"+{data.XpEarned} XP";
        return UniTask.CompletedTask;
    }

    public override async UniTask AnimateShowAsync(bool instant, CancellationToken ct)
    {
        if (canvasGroup == null || instant) { if (canvasGroup != null) canvasGroup.alpha = 1; return; }
        canvasGroup.alpha = 0f;
        await canvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutCubic).AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(ct);
    }

    private async void OnContinue()
    {
        await GameplayResetCoordinator.BackToMainMenuAsync();
    }

    private async void OnRetry()
    {
        await GameplayResetCoordinator.RetryAsync();
    }
}
