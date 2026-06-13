// Minimal stubs for framework types that originally lived in deleted UI/View folder.
// Allows Cost/, Item/, MonoBehavior/ code to compile while NinjaUI migration is in progress.
// To be replaced/removed when each subsystem migrates to NinjaUI explicitly.

using UnityEngine;

namespace Luzart
{
    // NOTE: IView, ViewT<T>, ObjectView, ICostVisualResolver,
    // AssetCostVisualResolver_ResourcePool and EResourceCostView were moved to a
    // faithful IO_Training MVVM port under Assets/_Main/Scripts/_IOPort/.
    // They are no longer stubbed here.

    // ============================================================
    // Broadcaster payloads from old UI joystick.
    // ============================================================

    /// <summary>
    /// Broadcaster payload originally raised by old JoystickControllerView UI.
    /// Stubbed so `MoveMonoBehavior` (gameplay-side listener) compiles.
    /// When migrating Joystick to NinjaUI, raise this struct with real Direction.
    /// </summary>
    public struct JoystickBroadcastData : IBroadcastData
    {
        public Vector2 Direction;
    }

    /// <summary>
    /// Game-end broadcast originally from `ClassicMode`. Kept so `GameController`
    /// can still register/raise the event. Wire into NinjaUI EndGame popup later.
    /// </summary>
    public struct Data_ClassicEndGame : IBroadcastData
    {
        public bool IsWin;
        public int FinalScore;
        public int EnemiesKilled;
        public float SurvivalTime;
    }

    /// <summary>
    /// Popup data for skill-upgrade roll. Wire into NinjaUI's skill-upgrade screen
    /// when migrating `UpgradeSkillManager` to NinjaUI flow.
    /// </summary>
    public class PopupSkillUpgradeData
    {
        public System.Collections.Generic.List<Data_UpgradeSkill> Options;
        public void InitData(System.Collections.Generic.List<Data_UpgradeSkill> options) => Options = options;
    }

    // Data_UpgradeSkill and ListExtensions.GetShuffle live in `_LuzartGame/...` — not stubbed here.
}

// ============================================================
// W5/W6 nuke stubs — global namespace types whose .cs files were deleted,
// but a handful of Luzart consumers still type-reference them. Empty no-op
// shells keep compile clean; FindObjectOfType returns null at runtime
// (consumers null-guard).
// ============================================================

/// <summary>Post-nuke <c>CurrencyManager</c> upgraded from no-op stub → self-instantiating
/// singleton so the restored <c>Coin</c> pickup can award coins and notify subscribers.
/// SV_GameplayHudUI subscribes to <see cref="OnCoinChanged"/> for the HUD label;
/// <see cref="AddCoin"/> is the single write entry point.</summary>
public class CurrencyManager : UnityEngine.MonoBehaviour
{
    private static CurrencyManager _instance;
    public static CurrencyManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new UnityEngine.GameObject("[CurrencyManager]");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _instance = go.AddComponent<CurrencyManager>();
            }
            return _instance;
        }
    }
    public event System.Action<long> OnCoinChanged;
    public long Coins { get; private set; }

    private const string CoinSaveKey = "sv_coins_v1";

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        // Persist coins across sessions so the Shop is spendable from the menu (pickups
        // accumulate too). Seed a small starting balance on first ever run.
        if (UnityEngine.PlayerPrefs.HasKey(CoinSaveKey))
            Coins = (long)UnityEngine.PlayerPrefs.GetInt(CoinSaveKey);
        else
        {
            Coins = 500;
            UnityEngine.PlayerPrefs.SetInt(CoinSaveKey, (int)Coins);
            UnityEngine.PlayerPrefs.Save();
        }
    }

    /// <summary>Add <paramref name="amount"/> coins and broadcast <see cref="OnCoinChanged"/>.
    /// Called by <c>Coin</c> on pickup. Negative amounts allowed for spend flows.</summary>
    public void AddCoin(long amount)
    {
        if (amount == 0) return;
        Coins += amount;
        if (Coins < 0) Coins = 0;
        UnityEngine.PlayerPrefs.SetInt(CoinSaveKey, (int)Coins);
        UnityEngine.PlayerPrefs.Save();
        OnCoinChanged?.Invoke(Coins);
    }

    public static string FormatNumber(long value)
    {
        if (value < 0) return "-" + FormatNumber(-value);
        if (value < 1000L) return value.ToString();
        if (value >= 1000000000L) { float b = value / 1000000000f; return b >= 100f ? $"{b:0}B" : b >= 10f ? $"{b:0.#}B" : $"{b:0.##}B"; }
        if (value >= 1000000L)    { float m = value / 1000000f;    return m >= 100f ? $"{m:0}M" : m >= 10f ? $"{m:0.#}M" : $"{m:0.##}M"; }
        float k = value / 1000f;
        return k >= 100f ? $"{k:0}K" : k >= 10f ? $"{k:0.#}K" : $"{k:0.##}K";
    }
}

namespace DATN.Legacy
{
    /// <summary>Stub for the deleted <c>DATN.Legacy.UIManager</c>. SV_MainMenuUI and
    /// GameplayResetCoordinator type-reference PlayBtn / BackFinishSafe. With the legacy
    /// MonoBehaviour gone, FindObjectOfType returns null at runtime — consumers null-guard,
    /// so the legacy flow becomes a no-op. Luzart-native equivalents (ClassicMode.StartGame,
    /// GameController.SpawnDefaultLevel, etc.) take over.</summary>
    public class UIManager : UnityEngine.MonoBehaviour
    {
        public bool MapReady { get; set; }
        public bool DestroyEnemys { get; set; }
        public void PlayBtn() { }
        public void BackFinishSafe() { }
        public void BackFinish() { }
        public void BackBtn() { }
    }
}
