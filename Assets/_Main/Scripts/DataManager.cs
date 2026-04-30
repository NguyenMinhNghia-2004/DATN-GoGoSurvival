using System;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Phase 1: Direct wrapping around PlayerPrefs for easy centralized testing offline
    // Phase 2 will replace PlayerPrefs inside here with UGS Cloud Save logic

    public int GetCoins() => PlayerPrefs.GetInt("coins", 0);
    public void SetCoins(int value) => PlayerPrefs.SetInt("coins", value);

    public int GetGems() => PlayerPrefs.GetInt("gems", 0);
    public void SetGems(int value) => PlayerPrefs.SetInt("gems", value);

    public int GetFlash() => PlayerPrefs.GetInt("flash", 0);
    public void SetFlash(int value) => PlayerPrefs.SetInt("flash", value);

    public int GetCurrentScore() => PlayerPrefs.GetInt("currentScore", 1);
    public void SetCurrentScore(int value) => PlayerPrefs.SetInt("currentScore", value);

    public float GetScore() => PlayerPrefs.GetFloat("Score", 0f);
    public void SetScore(float value) => PlayerPrefs.SetFloat("Score", value);

    public int GetCurrentKilled() => PlayerPrefs.GetInt("CurrentKilled", 0);
    public void SetCurrentKilled(int value) => PlayerPrefs.SetInt("CurrentKilled", value);

    public int GetCurrentCurrency() => PlayerPrefs.GetInt("CurrentCurrency", 0);
    public void SetCurrentCurrency(int value) => PlayerPrefs.SetInt("CurrentCurrency", value);

    public string GetCheckEvolve() => PlayerPrefs.GetString("CheckEvolve", "");
    public void SetCheckEvolve(string value) => PlayerPrefs.SetString("CheckEvolve", value);

    public void SaveData()
    {
        PlayerPrefs.Save();
        // Future UGS sync method will be called here
    }
}
