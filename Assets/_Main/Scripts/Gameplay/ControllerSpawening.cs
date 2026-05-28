using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DATN.Legacy;

public class ControllerSpawening : MonoBehaviour
{
    public GameObject Spawner;
    public UIManager Manager;public GameManager Managers;

    public GameObject SpawenOne;
    public GameObject SpawenTwo;
    internal bool CheckWork = true;
    internal bool KeepingGame = false;

    // Phase D.5 — MapReady source migrated from UIManager.MapReady to GameController.MapReady.
    // Manager Inspector ref kept null-safe for the BoolGameStart bridge but no longer required.
    private bool MapReady
    {
        get
        {
            var gc = Luzart.SceneRootManager.Instance?.Domain?.Get<Luzart.GameController>();
            if (gc != null) return gc.MapReady;
            return Manager != null && Manager.MapReady;
        }
    }

    void Update()
    {
        if(KeepingGame == true)
        {
            Managers.ResumeBtn();
        }
        if(MapReady == false)
        {
            Spawner.SetActive(false);
            CheckWork = true;
        }
        if (MapReady == true)
        {
            Spawner.SetActive(true);
            if(CheckWork == true)
            {
                StartCoroutine(SpawenOne.GetComponent<SpawenManager>().SpaweningManager());
                StartCoroutine(SpawenTwo.GetComponent<SpawenManager>().SpaweningManager());
                StartCoroutine(SpawenOne.GetComponent<SpawenManager>().stopSpawning());
                StartCoroutine(SpawenTwo.GetComponent<SpawenManager>().stopSpawning());
                CheckWork = false;
            }
        }
    }
    public void StartGame()
    {
        KeepingGame = true;
    }
    public void StopPause()
    {
        KeepingGame = false;
    }
}
