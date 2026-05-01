using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum CurrencyType
{
    Coin,
    Gem,
    Energy
}

public class CurrencyDisplay : MonoBehaviour
{
    [SerializeField] private CurrencyType type;
    [SerializeField] private Text legacyText;
    [SerializeField] private TextMeshProUGUI tmpText;

    [Header("Energy Only")]
    [SerializeField] private Text legacyTimerText;
    [SerializeField] private TextMeshProUGUI tmpTimerText;

    private bool isSubscribed;

    private void OnEnable()
    {
        if (CurrencyManager.Instance != null)
        {
            Subscribe();
            Refresh();
        }
        else
        {
            StartCoroutine(WaitForCurrencyManager());
        }
    }

    private void OnDisable()
    {
        if (isSubscribed && CurrencyManager.Instance != null)
            Unsubscribe();
    }

    private IEnumerator WaitForCurrencyManager()
    {
        while (CurrencyManager.Instance == null)
            yield return null;

        Subscribe();
        Refresh();
    }

    private void Subscribe()
    {
        if (isSubscribed) return;
        switch (type)
        {
            case CurrencyType.Coin:
                CurrencyManager.Instance.OnCoinChanged += OnCoinChanged;
                break;
            case CurrencyType.Gem:
                CurrencyManager.Instance.OnGemChanged += OnGemChanged;
                break;
            case CurrencyType.Energy:
                CurrencyManager.Instance.OnEnergyChanged += OnEnergyChanged;
                break;
        }
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed) return;
        switch (type)
        {
            case CurrencyType.Coin:
                CurrencyManager.Instance.OnCoinChanged -= OnCoinChanged;
                break;
            case CurrencyType.Gem:
                CurrencyManager.Instance.OnGemChanged -= OnGemChanged;
                break;
            case CurrencyType.Energy:
                CurrencyManager.Instance.OnEnergyChanged -= OnEnergyChanged;
                break;
        }
        isSubscribed = false;
    }

    private void Update()
    {
        if (type == CurrencyType.Energy && CurrencyManager.Instance != null)
            UpdateTimerText();
    }

    public void Refresh()
    {
        if (CurrencyManager.Instance == null) return;
        switch (type)
        {
            case CurrencyType.Coin:
                SetText(CurrencyManager.FormatNumber(CurrencyManager.Instance.Coins));
                break;
            case CurrencyType.Gem:
                SetText(CurrencyManager.Instance.Gems.ToString());
                break;
            case CurrencyType.Energy:
                SetText($"{CurrencyManager.Instance.Energy}/{CurrencyManager.MAX_ENERGY}");
                UpdateTimerText();
                break;
        }
    }

    private void OnCoinChanged(long value)
    {
        SetText(CurrencyManager.FormatNumber(value));
    }

    private void OnGemChanged(int value)
    {
        SetText(value.ToString());
    }

    private void OnEnergyChanged(int current, int max)
    {
        SetText($"{current}/{max}");
    }

    private void UpdateTimerText()
    {
        string timer = CurrencyManager.Instance.GetRegenTimerText();
        if (legacyTimerText != null) legacyTimerText.text = timer;
        if (tmpTimerText != null) tmpTimerText.text = timer;
    }

    private void SetText(string value)
    {
        if (legacyText != null) legacyText.text = value;
        if (tmpText != null) tmpText.text = value;
    }
}

