using System.Threading;
using Cysharp.Threading.Tasks;
using Luzart;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// NinjaUI wrapper cho HUD trong gameplay (HP bar, scoring, kill count, reload bar).
/// Lane: Hud (persistent, không stack).
/// Cache: KeepLoaded.
///
/// Replaces: HUD logic scattered in DATN.Legacy.UIManager / GameManager.
/// Subscribe gameplay events to update bars instead of polling in Update().
/// </summary>
public class SV_GameplayHudUI : UIBase
{
    // Framework refs resolved on show.
    private PlayerCharacter _player;
    private GameController _gameController;
    private LevelConfig _levelConfig;
    private INumberWithSet _xp;
    private INumberWithSet _hp;
    private INumberWithSet _enemyDead;
    private IVariable<int> _currentLevel;
    private bool _subscribed;

    [Header("Health")]
    [SerializeField] private Image healthBar;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Score / Level")]
    [SerializeField] private Image scoringLevelBar;
    [SerializeField] private TextMeshProUGUI scoringValueText;
    [SerializeField] private TextMeshProUGUI scoringValueTextDeux;
    [SerializeField] private TextMeshProUGUI valueKilledText;
    [SerializeField] private TextMeshProUGUI currentCoinsText;

    [Header("Weapon reload")]
    [SerializeField] private Image reloadWeaponBar;
    [SerializeField] private Image fillingFlashBar;

    [Header("Pause button")]
    [SerializeField] private Button btnPause;

    public override UniTask OnCreateAsync(UIContext ctx, CancellationToken ct)
    {
        if (btnPause != null) btnPause.onClick.AddListener(OnPauseClicked);
        return UniTask.CompletedTask;
    }

    public override UniTask OnBeforeShowAsync(UIContext ctx, CancellationToken ct)
    {
        SubscribeFramework();
        return UniTask.CompletedTask;
    }

    public override UniTask OnHiddenAsync(UIHideReason reason, CancellationToken ct)
    {
        UnsubscribeFramework();
        return UniTask.CompletedTask;
    }

    private void SubscribeFramework()
    {
        if (_subscribed) return;
        if (SceneRootManager.Instance == null || SceneRootManager.Instance.Domain == null) return;

        var d = SceneRootManager.Instance.Domain;
        _player = d.Get<PlayerCharacter>();
        _gameController = d.Get<GameController>();
        _levelConfig = d.Get<LevelConfig>();
        if (_player == null || _player.Stats == null || _gameController == null) return;

        _xp = _player.Stats.GetRuntime(StatType.Runtime_XP);
        _hp = _player.Stats.GetRuntime(StatType.Runtime_HP);
        _currentLevel = _gameController.CurrentLevel;
        _enemyDead = _gameController.CountEnemyDead;

        _xp.Changed += OnXpChanged;
        _hp.Changed += OnHpChanged;
        _currentLevel.Changed += OnLevelChanged;
        _enemyDead.Changed += OnEnemyDeadChanged;

        // Push initial values so HUD reflects current state immediately.
        OnXpChanged(_xp);
        OnHpChanged(_hp);
        OnLevelChanged(_currentLevel);
        OnEnemyDeadChanged(_enemyDead);

        _subscribed = true;
    }

    private void UnsubscribeFramework()
    {
        if (!_subscribed) return;
        if (_xp != null) _xp.Changed -= OnXpChanged;
        if (_hp != null) _hp.Changed -= OnHpChanged;
        if (_currentLevel != null) _currentLevel.Changed -= OnLevelChanged;
        if (_enemyDead != null) _enemyDead.Changed -= OnEnemyDeadChanged;
        _subscribed = false;
    }

    private void OnXpChanged(INumber xp)
    {
        if (_levelConfig == null || _currentLevel == null) return;
        var thresholds = _levelConfig.ListXPRequirePerLevel;
        if (thresholds == null || thresholds.Count == 0) return;

        int lvl = _currentLevel.Value;
        double xpVal = xp.Value;
        double lower = lvl > 0 && lvl - 1 < thresholds.Count ? thresholds[lvl - 1] : 0;
        double upper = lvl < thresholds.Count ? thresholds[lvl] : lower;
        double span = upper - lower;
        float pct = span > 0 ? (float)((xpVal - lower) / span) : 1f;
        SetScoreLevelProgress(pct);
    }

    private void OnHpChanged(INumber hp)
    {
        float currentHp = (float)hp.Value;
        float maxHp = 100f;
        if (_player != null && _player.Stats != null)
        {
            var maxStat = _player.Stats.Get(StatType.HPMax);
            if (maxStat != null && maxStat.Value > 0) maxHp = (float)maxStat.Value;
        }
        SetHealth(currentHp, maxHp);
    }

    private void OnLevelChanged(IValue<int> level)
    {
        SetLevelText(level.Value);
        // Recompute XP bar against the new level threshold window.
        if (_xp != null) OnXpChanged(_xp);
    }

    private void OnEnemyDeadChanged(INumber count)
    {
        SetKillCount((int)count.Value);
    }

    /// <summary>External API for gameplay to push HP changes.</summary>
    public void SetHealth(float currentHP, float maxHP)
    {
        if (healthBar != null) healthBar.fillAmount = maxHP > 0 ? currentHP / maxHP : 0f;
        if (healthText != null) healthText.text = $"{Mathf.CeilToInt(currentHP)} / {Mathf.CeilToInt(maxHP)}";
    }

    public void SetScoreLevelProgress(float pct01)
    {
        if (scoringLevelBar != null) scoringLevelBar.fillAmount = Mathf.Clamp01(pct01);
    }

    public void SetReloadProgress(float pct01)
    {
        if (reloadWeaponBar != null) reloadWeaponBar.fillAmount = Mathf.Clamp01(pct01);
    }

    public void SetKillCount(int killed) {
        if (valueKilledText != null) valueKilledText.text = killed.ToString();
    }
    public void SetCoinCount(int coins) {
        if (currentCoinsText != null) currentCoinsText.text = coins.ToString();
    }
    public void SetLevelText(int level) {
        if (scoringValueText != null) scoringValueText.text = level.ToString();
        if (scoringValueTextDeux != null) scoringValueTextDeux.text = level.ToString();
    }

    private async void OnPauseClicked()
    {
        await UIManager.Instance.ShowAsync(UIId.SV_PausePopup, ct: this.GetCancellationTokenOnDestroy());
    }

    public override bool HandleEscape()
    {
        // ESC during gameplay → open pause popup, don't dismiss HUD.
        OnPauseClicked();
        return true;
    }
}
