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
    public string ChapterName;
    public float BestTime;
}

/// <summary>
/// Win screen — show stats + claim rewards.
/// Lane: Screen. Replace any currently-active gameplay screen.
/// </summary>
public class SV_WinScreenUI : UIBase<SV_WinData>
{
    [Header("Stats")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Text killsText;
    [SerializeField] private Text timeText;
    [SerializeField] private Text coinsText;
    [SerializeField] private Text xpText;
    [SerializeField] private Text bestTimeText;
    [SerializeField] private Text chapterText;

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
        // Show the (now updated) shop gold balance on the win screen.
        if (GetComponent<SV_GoldDisplay>() == null) gameObject.AddComponent<SV_GoldDisplay>();

        var container = transform.Find("Background/Container");
        if (container != null)
        {
            if (timeText == null)
            {
                var t = container.Find("Time/Time");
                if (t != null) timeText = t.GetComponent<Text>();
            }
            if (chapterText == null)
            {
                var t = container.Find("Time/ChapitersPart");
                if (t != null) chapterText = t.GetComponent<Text>();
            }
            if (bestTimeText == null)
            {
                var t = container.Find("BestTime/Time");
                if (t != null) bestTimeText = t.GetComponent<Text>();
            }
            if (killsText == null)
            {
                var t = container.Find("Killed/Icon/Text (Legacy)");
                if (t != null) killsText = t.GetComponent<Text>();
            }
            if (coinsText == null)
            {
                var t = container.Find("Down/xCoins/Text (Legacy)");
                if (t != null) coinsText = t.GetComponent<Text>();
            }
            if (xpText == null)
            {
                var t = container.Find("Down/xExp/Text (Legacy)");
                if (t != null) xpText = t.GetComponent<Text>();
            }
        }

        return UniTask.CompletedTask;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        var container = transform.Find("Background/Container");
        if (container != null)
        {
            if (timeText == null)
            {
                var t = container.Find("Time/Time");
                if (t != null) timeText = t.GetComponent<Text>();
            }
            if (chapterText == null)
            {
                var t = container.Find("Time/ChapitersPart");
                if (t != null) chapterText = t.GetComponent<Text>();
            }
            if (bestTimeText == null)
            {
                var t = container.Find("BestTime/Time");
                if (t != null) bestTimeText = t.GetComponent<Text>();
            }
            if (killsText == null)
            {
                var t = container.Find("Killed/Icon/Text (Legacy)");
                if (t != null) killsText = t.GetComponent<Text>();
            }
            if (coinsText == null)
            {
                var t = container.Find("Down/xCoins/Text (Legacy)");
                if (t != null) coinsText = t.GetComponent<Text>();
            }
            if (xpText == null)
            {
                var t = container.Find("Down/xExp/Text (Legacy)");
                if (t != null) xpText = t.GetComponent<Text>();
            }
        }
    }
#endif

    protected override UniTask OnBeforeShowAsync(SV_WinData data, CancellationToken ct)
    {
        if (data == null) return UniTask.CompletedTask;
        if (scoreText != null) scoreText.text = data.FinalScore.ToString("N0");
        if (killsText != null) killsText.text = data.EnemiesKilled.ToString();
        if (timeText != null) timeText.text = $"{(int)data.SurvivalTime / 60:00}:{(int)data.SurvivalTime % 60:00}";
        if (bestTimeText != null) bestTimeText.text = $"{(int)data.BestTime / 60:00}:{(int)data.BestTime % 60:00}";
        if (chapterText != null) chapterText.text = !string.IsNullOrEmpty(data.ChapterName) ? data.ChapterName : "Chapter 1";
        if (coinsText != null) coinsText.text = $"+{data.CoinsEarned}";
        if (xpText != null) xpText.text = $"+{data.XpEarned}";
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
