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
    public string ChapterName;
    public float BestTime;
}

/// <summary>Game over screen.</summary>
public class SV_LoseScreenUI : UIBase<SV_LoseData>
{
    [Header("Stats")]
    [SerializeField] private Text killsText;
    [SerializeField] private Text timeText;
    [SerializeField] private Text bestTimeText;
    [SerializeField] private Text chapterText;
    [SerializeField] private Text coinsText;

    [Header("Buttons")]
    [SerializeField] private Button btnRetry;
    [SerializeField] private Button btnMainMenu;

    [Header("Anim")]
    [SerializeField] private CanvasGroup canvasGroup;

    public override UniTask OnCreateAsync(UIContext ctx, CancellationToken ct)
    {
        UIButtonSanitizer.SanitizeChildButtons(transform);
        // This defeat screen has a single "confirm" button that returns to the main menu — no
        // retry. The prefab wires the visible confirm button into the btnRetry slot (GO "Continue"),
        // which previously ran OnRetry → RetryAsync → restarted the run ("home" resumed the game").
        // Route EVERY result button to Home so the confirm button reliably goes back to the menu,
        // whichever serialized slot it sits in.
        if (btnRetry != null) btnRetry.onClick.AddListener(OnMainMenu);
        if (btnMainMenu != null) btnMainMenu.onClick.AddListener(OnMainMenu);
        // Show the (now updated) shop gold balance on the defeat screen.
        if (GetComponent<SV_GoldDisplay>() == null) gameObject.AddComponent<SV_GoldDisplay>();

        // Auto-find Text components
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
        }
    }
#endif

    protected override UniTask OnBeforeShowAsync(SV_LoseData data, CancellationToken ct)
    {
        if (data == null) return UniTask.CompletedTask;
        if (killsText != null) killsText.text = data.EnemiesKilled.ToString();
        if (timeText != null) timeText.text = $"{(int)data.SurvivalTime / 60:00}:{(int)data.SurvivalTime % 60:00}";
        if (bestTimeText != null) bestTimeText.text = $"{(int)data.BestTime / 60:00}:{(int)data.BestTime % 60:00}";
        if (chapterText != null) chapterText.text = !string.IsNullOrEmpty(data.ChapterName) ? data.ChapterName : "Chapter 1";
        if (coinsText != null) coinsText.text = $"+{data.CoinsEarned}";
        return UniTask.CompletedTask;
    }

    public override async UniTask AnimateShowAsync(bool instant, CancellationToken ct)
    {
        if (canvasGroup == null || instant) { if (canvasGroup != null) canvasGroup.alpha = 1; return; }
        canvasGroup.alpha = 0f;
        await canvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutCubic).AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(ct);
    }

    private async void OnMainMenu()
    {
        await GameplayResetCoordinator.BackToMainMenuAsync();
    }
}
