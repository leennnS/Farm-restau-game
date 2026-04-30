using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class MainMenuBackgroundMusic : MonoBehaviour
{
    [Header("Music")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private string menuSceneName = "MAIN MENU";

    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        ConfigureAudioSource();
    }

    private void Start()
    {
        if (!IsMenuScene())
            return;

        PlayMusic();
    }

    private void OnDisable()
    {
        StopMusic();
    }

    private void OnDestroy()
    {
        StopMusic();
    }

    private bool IsMenuScene()
    {
        return string.Equals(SceneManager.GetActiveScene().name, menuSceneName, System.StringComparison.Ordinal);
    }

    private void ConfigureAudioSource()
    {
        if (_audioSource == null)
            return;

        _audioSource.playOnAwake = false;
        _audioSource.loop = true;
        _audioSource.spatialBlend = 0f;
    }

    private void PlayMusic()
    {
        if (_audioSource == null || backgroundMusic == null)
            return;

        _audioSource.clip = backgroundMusic;

        if (!_audioSource.isPlaying)
            _audioSource.Play();
    }

    private void StopMusic()
    {
        if (_audioSource == null)
            return;

        if (_audioSource.isPlaying)
            _audioSource.Stop();
    }
}