using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DATN.Legacy;

public class AudioCheckerPlayer : MonoBehaviour
{
    public GameObject UIManager;
    private BooleanManager BoolM;
    private AudioSource audioSource;

    private DATN.Legacy.UIManager _uiMgr;

    private void Start()
    {
        // After NinjaUI migration, the legacy UIManager GameObject was renamed/reparented
        // (no longer at root "UI"). Find by component type instead.
        _uiMgr = UnityEngine.Object.FindFirstObjectByType<DATN.Legacy.UIManager>();
        UIManager = _uiMgr != null ? _uiMgr.gameObject : null;
        GameObject controller = GameObject.Find("Controller");
        if (controller != null)
            BoolM = controller.GetComponent<BooleanManager>();
        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (_uiMgr != null && _uiMgr.StopAllAudios == true)
        {
            Destroy(this.gameObject);
            return;
        }

        if (BoolM != null && audioSource != null)
        {
            audioSource.mute = !BoolM.Music;
        }
    }
}
