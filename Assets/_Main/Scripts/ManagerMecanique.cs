using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;


public class ManagerMecanique : MonoBehaviour
{
    [Header("Manager Controller")]
    public GameManager Manager;
    public UIManager UIManager;

    [Header("Texting Names")]
    public Text Flash;
    public Text Gems;
    public Text Coins;

    [Header("Times Controller")]
    public Text FlashTime;
    public Text GemsTime;
    public Text CoinsTime;

    [Header("Levels Manager Score")]
    public Image BarLevel;
    public Text ValueLevel;

    [Header("Int Manager")]
    internal int FlashInt;
    internal int GemsInt;
    internal int CoinsInt;
    internal int LevelInt;

    [Header("Floating Manager")]
    internal float LevelLevel;

    void Start()
    {
        FlashInt = DataManager.Instance.GetFlash();
        GemsInt = DataManager.Instance.GetGems();
        CoinsInt = DataManager.Instance.GetCoins();
        LevelInt = DataManager.Instance.GetCurrentScore();
        LevelLevel = DataManager.Instance.GetScore();
        FlashTime.text = "";
        GemsTime.text = "";
        CoinsTime.text = "";
    }
    void Update()
    {
        AppearValues();
        LevelScore();
    }
    void AppearValues()
    {
        Flash.text = FlashInt + "/100";
        Gems.text = GemsInt + "";
        Coins.text = CoinsInt + "";
    }
    void LevelScore()
    {
        BarLevel.fillAmount = LevelLevel;
        if(BarLevel.fillAmount == 1)
        {
            LevelInt += 1;
            LevelLevel = 0;
            DataManager.Instance.SetScore(LevelLevel);
            DataManager.Instance.SetCurrentScore(LevelInt);
        }
        ValueLevel.text = LevelInt + "";
    }
    public void SetStart()
    {
        FlashInt += 3;
        DataManager.Instance.SetFlash(FlashInt);
    }
}
