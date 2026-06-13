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
    private IVariable<int> _countTime;
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

    [Header("Timer")]
    [SerializeField] private TextMeshProUGUI timerText; // TMP slot (preferred)
    private UnityEngine.UI.Text _timerLegacyText;       // auto-found legacy Text fallback

    public override UniTask OnCreateAsync(UIContext ctx, CancellationToken ct)
    {
        UIButtonSanitizer.SanitizeChildButtons(transform);
        if (btnPause != null) btnPause.onClick.AddListener(OnPauseClicked);
        // Auto-find legacy "Timer" Text child if the SV_GameplayHud prefab still has the
        // cloned legacy timer Text (no TMP version exists in the cloned prefab).
        if (timerText == null) {
            foreach (var tr in GetComponentsInChildren<Transform>(true)) {
                if (tr.name == "Timer") {
                    _timerLegacyText = tr.GetComponent<UnityEngine.UI.Text>();
                    if (_timerLegacyText != null) break;
                }
            }
        }
        // HP is shown by a world-space bar above the player's head (PlayerHealthBar), not on the
        // HUD — so nothing to build here. healthBar/healthText stay unwired (SetHealth no-ops).
        return UniTask.CompletedTask;
    }

    public override async UniTask OnBeforeShowAsync(UIContext ctx, CancellationToken ct)
    {
        SubscribeFramework();
        // Wait for legacy GameStart() coroutine to SetActive(true) on /UI/GamePlay (parent of Joystick Table).
        // Without the wait, GameObject.Find returns null and we can't attach the joystick Canvas.
        await WaitForJoystickActiveAsync(ct);
        EnableLegacyJoystick(true);
        BumpPickupRadius();
    }

    /// <summary>Diamond drops only magnetize toward the player when they enter the
    /// DetecteurRoad trigger collider. The scene-authored size is 1×1, too tight
    /// (almost no diamonds ever home in). Standard Survivor.io pickup radius is small —
    /// roughly 1.5 units in either direction (3×3 box). The Hi-Power Magnet passive
    /// scales this up later (+100% to +500%).</summary>
    private const float DesiredPickupRadius = 3f;
    private void BumpPickupRadius()
    {
        var player = GameObject.Find("Player");
        if (player == null) return;
        var dr = player.transform.Find("DetecteurRoad");
        if (dr == null) return;
        var col = dr.GetComponent<BoxCollider2D>();
        if (col == null) return;
        col.size = new Vector2(DesiredPickupRadius, DesiredPickupRadius);
    }

    public override UniTask OnHiddenAsync(UIHideReason reason, CancellationToken ct)
    {
        UnsubscribeFramework();
        EnableLegacyJoystick(false);
        return UniTask.CompletedTask;
    }

    /// <summary>Poll up to 2s for the Joystick Table to be active. After the /UI →
    /// _LegacyManagers migration, the joystick lives under _NinjaUI/2_Hud (rendered by
    /// NinjaUI canvas), and the legacy GamePlay container lives under _LegacyManagers.</summary>
    private async UniTask WaitForJoystickActiveAsync(CancellationToken ct)
    {
        for (int i = 0; i < 120; i++) // ~2s @ 60fps
        {
            var jt = FindJoystickTable();
            if (jt != null && jt.gameObject.activeInHierarchy) return;
            try { await UniTask.NextFrame(ct); } catch { return; }
        }
    }

    /// <summary>Locate Joystick Table at its new post-migration path. Falls back to a
    /// scene-wide search if the canonical path doesn't resolve (defensive).</summary>
    private static Transform FindJoystickTable()
    {
        // New path under NinjaUI Hud lane.
        var go = GameObject.Find("/_NinjaUI/2_Hud/Joystick Table");
        if (go != null) return go.transform;
        // W4 nuke: legacy movementJoystick class deleted — fallback by-type lookup removed.
        // If the canonical path doesn't resolve, just return null; caller null-guards.
        return null;
    }

    /// <summary>After the /UI → _LegacyManagers migration, the Joystick Table sits directly
    /// under <c>_NinjaUI/2_Hud</c> and renders through the NinjaUI canvas (no separate
    /// canvas hack needed). This method just ensures the joystick GameObject is active
    /// while the HUD is shown.</summary>
    private void EnableLegacyJoystick(bool on)
    {
        var jt = FindJoystickTable();
        if (jt != null) jt.gameObject.SetActive(on);
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

        // Subscribe to CurrencyManager for coin display (Phase B2 migration — ManagerMecanique
        // legacy display path replaced by direct CurrencyManager → HUD wiring).
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCoinChanged += OnCoinChanged;
            OnCoinChanged(CurrencyManager.Instance.Coins); // push initial value
        }

        _xp = _player.Stats.GetRuntime(StatType.Runtime_XP);
        _hp = _player.Stats.GetRuntime(StatType.Runtime_HP);
        _currentLevel = _gameController.CurrentLevel;
        _enemyDead = _gameController.CountEnemyDead;
        _countTime = _gameController.CountTime;

        _xp.Changed += OnXpChanged;
        _hp.Changed += OnHpChanged;
        _currentLevel.Changed += OnLevelChanged;
        _enemyDead.Changed += OnEnemyDeadChanged;
        _countTime.Changed += OnTimeChanged;

        // Push initial values so HUD reflects current state immediately.
        OnXpChanged(_xp);
        OnHpChanged(_hp);
        OnLevelChanged(_currentLevel);
        OnEnemyDeadChanged(_enemyDead);
        OnTimeChanged(_countTime);

        _subscribed = true;
    }

    private void UnsubscribeFramework()
    {
        if (!_subscribed) return;
        if (_xp != null) _xp.Changed -= OnXpChanged;
        if (_hp != null) _hp.Changed -= OnHpChanged;
        if (_currentLevel != null) _currentLevel.Changed -= OnLevelChanged;
        if (_enemyDead != null) _enemyDead.Changed -= OnEnemyDeadChanged;
        if (_countTime != null) _countTime.Changed -= OnTimeChanged;
        if (CurrencyManager.Instance != null) CurrencyManager.Instance.OnCoinChanged -= OnCoinChanged;
        _subscribed = false;
    }

    private void OnCoinChanged(long coins)
    {
        if (currentCoinsText != null) currentCoinsText.text = CurrencyManager.FormatNumber(coins);
    }

    private void OnTimeChanged(IValue<int> t)
    {
        int sec = t.Value;
        int m = sec / 60, s = sec % 60;
        string formatted = string.Format("{0:00}:{1:00}", m, s);
        if (timerText != null) timerText.text = formatted;
        if (_timerLegacyText != null) _timerLegacyText.text = formatted;
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
