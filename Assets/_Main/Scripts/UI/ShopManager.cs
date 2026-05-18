using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shop buttons. Phase B2 migrated: no longer depends on legacy ManagerMecanique mirror —
/// uses CurrencyManager singleton directly. CurrencyManager auto-saves and broadcasts
/// OnCoinChanged / OnGemChanged so all subscribers (SV_GameplayHud, etc.) auto-update.
/// </summary>
public class ShopManager : MonoBehaviour
{
    public Button btn0;
    public Button btn1;
    public Button btn2;
    public Button btn3;
    public Button btn4;
    public Button btn5;

    public void Geems()
    {
        if (CurrencyManager.Instance != null)
        {
            int bonus = CurrencyManager.Instance.Gems * 2;
            CurrencyManager.Instance.AddGems(bonus);
        }
        if (btn0 != null) btn0.interactable = false;
    }

    public void WeaponDesign()
    {
        if (btn1 != null) btn1.interactable = false;
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.SpendCoins(400);
    }

    public void BonePendant()
    {
        if (btn2 != null) btn2.interactable = false;
        if (CurrencyManager.Instance != null)
        {
            long bonus = CurrencyManager.Instance.Coins * 19;
            CurrencyManager.Instance.AddCoins(bonus);
        }
    }

    public void BoneM()
    {
        if (btn3 != null) btn3.interactable = false;
    }

    public void ClothingDesign()
    {
        if (btn4 != null) btn4.interactable = false;
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.SpendCoins(5000);
    }

    public void ShoesDesign()
    {
        if (btn5 != null) btn5.interactable = false;
    }
}
