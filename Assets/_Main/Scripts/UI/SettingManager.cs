using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class SettingManager : MonoBehaviour
{
    [Header("Manager")]
    public BooleanManager BoolM;

    [Header("Sound")]
    public GameObject SActiveBtn;
    public GameObject SInactiveBtn;
    public GameObject SIconActive;
    public GameObject SIconInactive;

    [Header("Music")]
    public GameObject MActiveBtn;
    public GameObject MInactiveBtn;
    public GameObject MIconActive;
    public GameObject MIconInactive;

    [Header("Vibration")]
    public GameObject VActiveBtn;
    public GameObject VInactiveBtn;
    public GameObject VIconActive;
    public GameObject VIconInactive;

    private void OnEnable()
    {
        SyncUI();
        if (BoolM != null)
            BoolM.OnSettingsChanged += SyncUI;
    }

    private void OnDisable()
    {
        if (BoolM != null)
            BoolM.OnSettingsChanged -= SyncUI;
    }

    private void SyncUI()
    {
        // Sound
        SActiveBtn.SetActive(BoolM.Sound);
        SInactiveBtn.SetActive(!BoolM.Sound);
        SIconActive.SetActive(BoolM.Sound);
        SIconInactive.SetActive(!BoolM.Sound);

        // Music
        MActiveBtn.SetActive(BoolM.Music);
        MInactiveBtn.SetActive(!BoolM.Music);
        MIconActive.SetActive(BoolM.Music);
        MIconInactive.SetActive(!BoolM.Music);

        // Vibration
        VActiveBtn.SetActive(BoolM.Vibration);
        VInactiveBtn.SetActive(!BoolM.Vibration);
        VIconActive.SetActive(BoolM.Vibration);
        VIconInactive.SetActive(!BoolM.Vibration);
    }

    public void SoundActive()
    {
        BoolM.SetSound(true);
    }
    public void SoundInActive()
    {
        BoolM.SetSound(false);
    }
    public void MusicActive()
    {
        BoolM.SetMusic(true);
    }
    public void MusicInactiveActive()
    {
        BoolM.SetMusic(false);
    }
    public void VibrationActive()
    {
        BoolM.SetVibration(true);
    }
    public void VibrationInactiveActive()
    {
        BoolM.SetVibration(false);
    }
}

