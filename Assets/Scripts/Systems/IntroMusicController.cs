using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class IntroMusicController : MonoBehaviour
{
    [SerializeField] private AudioClip introMusic;
    [SerializeField, Range(0f, 1f)] private float baseVolume = 1f;
    [SerializeField, Range(1, 6)] private int stackedSources = 4;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        AudioSource[] sources = GetComponents<AudioSource>();
        int sourceCount = Mathf.Max(1, stackedSources);

        while (sources.Length < sourceCount)
        {
            gameObject.AddComponent<AudioSource>();
            sources = GetComponents<AudioSource>();
        }

        for (int i = 0; i < sources.Length; i++)
        {
            bool shouldPlay = i < sourceCount;
            ConfigureSource(sources[i], shouldPlay);

            if (shouldPlay && sources[i].clip != null)
                sources[i].Play();
        }
    }

    private void ConfigureSource(AudioSource source, bool shouldPlay)
    {
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = shouldPlay ? baseVolume : 0f;
        source.clip = shouldPlay ? introMusic : null;

        if (AudioSettingsManager.HasInstance)
            AudioSettingsManager.Instance.RefreshAudioSource(source);
    }
}
