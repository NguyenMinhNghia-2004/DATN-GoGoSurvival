using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DATN.Legacy;

public class LocalisationPresent : MonoBehaviour
{
    public GameObject UI;
    public GameObject ControllerSpawener;
    internal bool Check = false;

    private UIManager _uiMgr;

    private void Start()
    {
        ControllerSpawener = GameObject.Find("Camera");
        // After NinjaUI migration, find UIManager by component type rather than GameObject name.
        _uiMgr = UnityEngine.Object.FindFirstObjectByType<UIManager>();
        UI = _uiMgr != null ? _uiMgr.gameObject : null;
        StartCoroutine(WaitingActive());
    }
    private void Update()
    {
        if (_uiMgr != null && _uiMgr.MapReady == true && Check == true && ControllerSpawener != null)
        {
            var cs = ControllerSpawener.GetComponent<ControllerSpawening>();
            if (cs == null) return;
            if (cs.SpawenOne != null) cs.SpawenOne.GetComponent<SpawenManager>().SpawenLocalisation = this.gameObject;
            if (cs.SpawenTwo != null) cs.SpawenTwo.GetComponent<SpawenManager>().SpawenLocalisation = this.gameObject;
        }
    }
    IEnumerator WaitingActive()
    {
        yield return new WaitForEndOfFrame();
        Check = true;
    }
}
