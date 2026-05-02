using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class IntroMusicController : MonoBehaviour
{
    [SerializeField] private AudioClip introMusic;
    [SerializeField, Range(0f, 1f)] private float baseVolume = 1f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;
        audioSource.volume = baseVolume;
        audioSource.clip = introMusic;

        if (AudioSettingsManager.HasInstance)
            AudioSettingsManager.Instance.RefreshAudioSource(audioSource);

        if (audioSource.clip != null)
            audioSource.Play();
    }
}
