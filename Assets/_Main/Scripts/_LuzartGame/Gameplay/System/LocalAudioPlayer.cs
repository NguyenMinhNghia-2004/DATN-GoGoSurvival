namespace Luzart
{
    using UnityEngine;

    [RequireComponent(typeof(AudioSource))]
    public class LocalAudioPlayer : MonoBehaviour
    {
        private AudioSource _audioSource;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
        }

        public void PlaySFX(AudioClip clip, float volume = 1f)
        {
            if (clip != null)
            {
                _audioSource.PlayOneShot(clip, volume);
            }
        }
    }
}
