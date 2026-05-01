using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioCheckerPlayer : MonoBehaviour
{
    public GameObject UIManager;
    private BooleanManager BoolM;
    private AudioSource audioSource;

    private void Start()
    {
        UIManager = GameObject.Find("UI");
        GameObject controller = GameObject.Find("Controller");
        if (controller != null)
            BoolM = controller.GetComponent<BooleanManager>();
        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (UIManager.GetComponent<UIManager>().StopAllAudios == true)
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
