using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BooleanManager : MonoBehaviour
{
    internal bool GameStart = false;

    internal bool Music = true;
    internal bool Sound = true;
    internal bool Vibration = true;

    public event Action OnSettingsChanged;

    private void Awake()
    {
        Music = PlayerPrefs.GetInt("setting_music", 1) == 1;
        Sound = PlayerPrefs.GetInt("setting_sound", 1) == 1;
        Vibration = PlayerPrefs.GetInt("setting_vibration", 1) == 1;
    }

    public void SetMusic(bool value)
    {
        Music = value;
        PlayerPrefs.SetInt("setting_music", value ? 1 : 0);
        PlayerPrefs.Save();
        OnSettingsChanged?.Invoke();
    }

    public void SetSound(bool value)
    {
        Sound = value;
        PlayerPrefs.SetInt("setting_sound", value ? 1 : 0);
        PlayerPrefs.Save();
        OnSettingsChanged?.Invoke();
    }

    public void SetVibration(bool value)
    {
        Vibration = value;
        PlayerPrefs.SetInt("setting_vibration", value ? 1 : 0);
        PlayerPrefs.Save();
        OnSettingsChanged?.Invoke();
    }

    private void Update()
    {
        if(GameStart == true)
        {
            
        }
    }
}

