using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Managers")]
    public BooleanManager BoolM;

    [Header("Componenet Sounds")]
    public GameObject Sound;
    public GameObject Music;
    public GameObject Vibration;

    [Header("Gameplay Check")]
    public GameObject ScreenGamePlay;

    private bool wasMusicActive;
    private bool wasInGamePlay;
    private bool hasStartedMusic;

    void Start()
    {
        wasMusicActive = BoolM.Music;
        wasInGamePlay = false;
        hasStartedMusic = false;
    }

    void Update()
    {
        Sound.SetActive(BoolM.Sound);

        bool isInGamePlay = ScreenGamePlay != null && ScreenGamePlay.activeSelf;

        if (BoolM.Music)
        {
            if (isInGamePlay)
            {
                Music.SetActive(false);
                hasStartedMusic = false;
            }
            else
            {
                Music.SetActive(true);
                AudioSource musicSource = Music.GetComponent<AudioSource>();
                if (musicSource != null && !musicSource.isPlaying)
                {
                    musicSource.Play();
                    hasStartedMusic = true;
                }
            }
        }
        else
        {
            Music.SetActive(false);
            hasStartedMusic = false;
        }
        wasMusicActive = BoolM.Music;
        wasInGamePlay = isInGamePlay;

        Vibration.SetActive(BoolM.Vibration);
    }
}

