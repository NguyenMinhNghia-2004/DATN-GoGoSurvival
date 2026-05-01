using System;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    public event Action<long> OnCoinChanged;
    public event Action<int> OnGemChanged;
    public event Action<int, int> OnEnergyChanged;

    public long Coins { get; private set; }
    public int Gems { get; private set; }
    public int Energy { get; private set; }

    public const int MAX_ENERGY = 30;
    public const int ENERGY_COST_PER_PLAY = 5;
    public const int ENERGY_REGEN_AMOUNT = 5;
    public const float ENERGY_REGEN_SECONDS = 1200f;

    public bool HasEnoughEnergy => Energy >= ENERGY_COST_PER_PLAY;

    private float regenTimer;

    private bool isInitialized;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeData()
    {
        if (DataManager.Instance == null) return;

        if (DataManager.Instance.IsFirstPlay())
        {
            DataManager.Instance.SetCoins(100000L);
            DataManager.Instance.SetGems(100);
            DataManager.Instance.SetEnergy(MAX_ENERGY);
            DataManager.Instance.SetFirstPlayDone();
            DataManager.Instance.SaveData();
        }

        Coins = DataManager.Instance.GetCoins();
        Gems = DataManager.Instance.GetGems();
        Energy = DataManager.Instance.GetEnergy();

        RecoverOfflineEnergy();
        isInitialized = true;

        OnCoinChanged?.Invoke(Coins);
        OnGemChanged?.Invoke(Gems);
        OnEnergyChanged?.Invoke(Energy, MAX_ENERGY);
    }

    private void Start()
    {
        if (!isInitialized)
            StartCoroutine(WaitForDataManager());
        else
            regenTimer = GetRemainingRegenTime();
    }

    private System.Collections.IEnumerator WaitForDataManager()
    {
        while (DataManager.Instance == null)
            yield return null;

        InitializeData();
        regenTimer = GetRemainingRegenTime();
    }

    private void Update()
    {
        if (Energy >= MAX_ENERGY) return;

        regenTimer -= Time.deltaTime;
        if (regenTimer <= 0f)
        {
            AddEnergy(ENERGY_REGEN_AMOUNT);
            regenTimer = ENERGY_REGEN_SECONDS;
            SaveEnergyTimestamp();
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            SaveEnergyTimestamp();
        else
            RecoverOfflineEnergy();
    }

    private void OnApplicationQuit()
    {
        SaveEnergyTimestamp();
    }

    #region Coin

    public void AddCoins(long amount)
    {
        if (amount <= 0) return;
        Coins += amount;
        DataManager.Instance.SetCoins(Coins);
        OnCoinChanged?.Invoke(Coins);
    }

    public bool SpendCoins(long amount)
    {
        if (amount <= 0 || Coins < amount) return false;
        Coins -= amount;
        DataManager.Instance.SetCoins(Coins);
        OnCoinChanged?.Invoke(Coins);
        return true;
    }

    public void SetCoins(long value)
    {
        Coins = Math.Max(0, value);
        DataManager.Instance.SetCoins(Coins);
        OnCoinChanged?.Invoke(Coins);
    }

    #endregion

    #region Gem

    public void AddGems(int amount)
    {
        if (amount <= 0) return;
        Gems += amount;
        DataManager.Instance.SetGems(Gems);
        OnGemChanged?.Invoke(Gems);
    }

    public bool SpendGems(int amount)
    {
        if (amount <= 0 || Gems < amount) return false;
        Gems -= amount;
        DataManager.Instance.SetGems(Gems);
        OnGemChanged?.Invoke(Gems);
        return true;
    }

    public void SetGems(int value)
    {
        Gems = Mathf.Max(0, value);
        DataManager.Instance.SetGems(Gems);
        OnGemChanged?.Invoke(Gems);
    }

    #endregion

    #region Energy

    public void UseEnergy()
    {
        if (!HasEnoughEnergy) return;
        bool wasFull = Energy >= MAX_ENERGY;
        Energy -= ENERGY_COST_PER_PLAY;
        DataManager.Instance.SetEnergy(Energy);
        OnEnergyChanged?.Invoke(Energy, MAX_ENERGY);

        if (wasFull)
        {
            regenTimer = ENERGY_REGEN_SECONDS;
            SaveEnergyTimestamp();
        }
    }

    public void AddEnergy(int amount)
    {
        if (amount <= 0) return;
        Energy = Mathf.Min(Energy + amount, MAX_ENERGY);
        DataManager.Instance.SetEnergy(Energy);
        OnEnergyChanged?.Invoke(Energy, MAX_ENERGY);
    }

    public void SetEnergy(int value)
    {
        Energy = Mathf.Clamp(value, 0, MAX_ENERGY);
        DataManager.Instance.SetEnergy(Energy);
        OnEnergyChanged?.Invoke(Energy, MAX_ENERGY);
    }

    public float GetRegenCountdown()
    {
        if (Energy >= MAX_ENERGY) return 0f;
        return Mathf.Max(0f, regenTimer);
    }

    public string GetRegenTimerText()
    {
        if (Energy >= MAX_ENERGY) return "";
        float t = GetRegenCountdown();
        int minutes = Mathf.FloorToInt(t / 60f);
        int seconds = Mathf.FloorToInt(t % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    private void SaveEnergyTimestamp()
    {
        string ts = DateTime.UtcNow.ToBinary().ToString();
        DataManager.Instance.SetEnergyTimestamp(ts);
        DataManager.Instance.SaveData();
    }

    private void RecoverOfflineEnergy()
    {
        if (Energy >= MAX_ENERGY) return;
        string tsStr = DataManager.Instance.GetEnergyTimestamp();
        if (string.IsNullOrEmpty(tsStr)) return;

        if (!long.TryParse(tsStr, out long binary)) return;
        DateTime lastTime = DateTime.FromBinary(binary);
        double elapsed = (DateTime.UtcNow - lastTime).TotalSeconds;
        if (elapsed <= 0) return;

        int regenCycles = Mathf.FloorToInt((float)(elapsed / ENERGY_REGEN_SECONDS));
        if (regenCycles > 0)
        {
            int recovered = regenCycles * ENERGY_REGEN_AMOUNT;
            Energy = Mathf.Min(Energy + recovered, MAX_ENERGY);
            DataManager.Instance.SetEnergy(Energy);
            OnEnergyChanged?.Invoke(Energy, MAX_ENERGY);
        }

        float remainder = (float)(elapsed % ENERGY_REGEN_SECONDS);
        regenTimer = ENERGY_REGEN_SECONDS - remainder;
    }

    private float GetRemainingRegenTime()
    {
        string tsStr = DataManager.Instance.GetEnergyTimestamp();
        if (string.IsNullOrEmpty(tsStr)) return ENERGY_REGEN_SECONDS;

        if (!long.TryParse(tsStr, out long binary)) return ENERGY_REGEN_SECONDS;
        DateTime lastTime = DateTime.FromBinary(binary);
        double elapsed = (DateTime.UtcNow - lastTime).TotalSeconds;
        float remainder = (float)(elapsed % ENERGY_REGEN_SECONDS);
        return Mathf.Max(0f, ENERGY_REGEN_SECONDS - remainder);
    }

    #endregion

    #region Format

    public static string FormatNumber(long value)
    {
        if (value < 0) return "-" + FormatNumber(-value);
        if (value < 1000L) return value.ToString();

        if (value >= 1000000000L)
        {
            float b = value / 1000000000f;
            return b >= 100f ? $"{b:0}B" : b >= 10f ? $"{b:0.#}B" : $"{b:0.##}B";
        }

        if (value >= 1000000L)
        {
            float m = value / 1000000f;
            return m >= 100f ? $"{m:0}M" : m >= 10f ? $"{m:0.#}M" : $"{m:0.##}M";
        }

        float k = value / 1000f;
        return k >= 100f ? $"{k:0}K" : k >= 10f ? $"{k:0.#}K" : $"{k:0.##}K";
    }

    #endregion
}
