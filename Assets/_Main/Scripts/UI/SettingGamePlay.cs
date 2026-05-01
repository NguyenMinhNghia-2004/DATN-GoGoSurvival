using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingGamePlay : MonoBehaviour
{
    public GameObject MSettings;

    void Start()
    {
        
    }
    void Update()
    {
        
    }
    public void OpenSetting()
    {
        MSettings.SetActive(true);
    }
    public void ExitSetting()
    {
        MSettings.SetActive(false);
    }
}
