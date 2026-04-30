using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public sealed class AudioSettingsManager : MonoBehaviour
{
    private const string MasterVolumeKey = "Audio.MasterVolume";
    private const string MusicVolumeKey = "Audio.MusicVolume";
    private const string SfxVolumeKey = "Audio.SfxVolume";

    private const float DefaultMasterVolume = 0.8f;
    private const float DefaultMusicVolume = 0.7f;
    private const float DefaultSfxVolume = 0.8f;

    private static AudioSettingsManager _instance;

    private readonly Dictionary<int, ManagedAudioSourceState> _managedSources = new Dictionary<int, ManagedAudioSourceState>();

    private float _masterVolumeNormalized = DefaultMasterVolume;
    private float _musicVolumeNormalized = DefaultMusicVolume;
    private float _sfxVolumeNormalized = DefaultSfxVolume;

    public static AudioSettingsManager Instance
    {
        get
        {
            EnsureInstance();
            return _instance;
        }
    }

    public static bool HasInstance => _instance != null;

    public event Action SettingsChanged;

    public float MasterVolumeNormalized => _masterVolumeNormalized;
    public float MusicVolumeNormalized => _musicVolumeNormalized;
    public float SfxVolumeNormalized => _sfxVolumeNormalized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    private static void EnsureInstance()
    {
        if (_instance != null)
            return;

        AudioSettingsManager existing = FindExistingInstance();
        if (existing != null)
        {
            _instance = existing;
            return;
        }

        GameObject managerObject = new GameObject(nameof(AudioSettingsManager));
        _instance = managerObject.AddComponent<AudioSettingsManager>();
    }

    private static AudioSettingsManager FindExistingInstance()
    {
        AudioSettingsManager[] managers = FindObjectsByType<AudioSettingsManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < managers.Length; i++)
        {
            if (managers[i] != null)
                return managers[i];
        }

        return null;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
        ApplyCurrentSettingsToSceneAudio();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyCurrentSettingsToSceneAudio();
    }

    public void SetMasterVolumeNormalized(float value)
    {
        SetVolumes(value, _musicVolumeNormalized, _sfxVolumeNormalized, saveImmediately: true);
    }

    public void SetMusicVolumeNormalized(float value)
    {
        SetVolumes(_masterVolumeNormalized, value, _sfxVolumeNormalized, saveImmediately: true);
    }

    public void SetSfxVolumeNormalized(float value)
    {
        SetVolumes(_masterVolumeNormalized, _musicVolumeNormalized, value, saveImmediately: true);
    }

    public void SetVolumes(float masterNormalized, float musicNormalized, float sfxNormalized, bool saveImmediately)
    {
        _masterVolumeNormalized = Mathf.Clamp01(masterNormalized);
        _musicVolumeNormalized = Mathf.Clamp01(musicNormalized);
        _sfxVolumeNormalized = Mathf.Clamp01(sfxNormalized);

        if (saveImmediately)
            SaveSettings();

        ApplyCurrentSettingsToSceneAudio();
        SettingsChanged?.Invoke();
    }

    public void ApplyCurrentSettingsToSceneAudio()
    {
        AudioListener.volume = _masterVolumeNormalized;

        CleanupMissingSources();

        AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];
            if (source == null)
                continue;

            ApplyChannelMultiplier(source);
        }
    }

    public void RefreshAudioSource(AudioSource source)
    {
        if (source == null)
            return;

        ManagedAudioSourceState state = GetOrCreateState(source);
        float targetMultiplier = GetChannelMultiplier(state.Channel);
        ApplyMultiplierToSource(source, state, targetMultiplier);
    }

    public static void PlaySfxAtPoint(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null)
            return;

        float sfxMultiplier = HasInstance ? Instance.SfxVolumeNormalized : 1f;
        AudioSource.PlayClipAtPoint(clip, position, Mathf.Clamp01(volume * sfxMultiplier));
    }

    private void ApplyChannelMultiplier(AudioSource source)
    {
        ManagedAudioSourceState state = GetOrCreateState(source);
        float targetMultiplier = GetChannelMultiplier(state.Channel);
        ApplyMultiplierToSource(source, state, targetMultiplier);
    }

    private void ApplyMultiplierToSource(AudioSource source, ManagedAudioSourceState state, float targetMultiplier)
    {
        if (source == null || state == null)
            return;

        source.volume = Mathf.Clamp01(state.BaseVolume * targetMultiplier);
        state.LastAppliedMultiplier = targetMultiplier;
    }

    private ManagedAudioSourceState GetOrCreateState(AudioSource source)
    {
        if (source == null)
            return null;

        int sourceId = source.GetInstanceID();
        if (_managedSources.TryGetValue(sourceId, out ManagedAudioSourceState state))
            return state;

        state = new ManagedAudioSourceState
        {
            Source = source,
            Channel = DetermineChannel(source),
            BaseVolume = source.volume,
            LastAppliedMultiplier = 1f
        };

        _managedSources[sourceId] = state;
        return state;
    }

    private void CleanupMissingSources()
    {
        if (_managedSources.Count == 0)
            return;

        List<int> missingKeys = null;

        foreach (KeyValuePair<int, ManagedAudioSourceState> entry in _managedSources)
        {
            if (entry.Value == null || entry.Value.Source == null)
            {
                missingKeys ??= new List<int>();
                missingKeys.Add(entry.Key);
            }
        }

        if (missingKeys == null)
            return;

        for (int i = 0; i < missingKeys.Count; i++)
        {
            _managedSources.Remove(missingKeys[i]);
        }
    }

    private static AudioChannel DetermineChannel(AudioSource source)
    {
        if (source == null)
            return AudioChannel.Sfx;

        MonoBehaviour[] behaviours = source.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null)
                continue;

            string typeName = behaviour.GetType().Name;
            if (typeName.IndexOf("Music", StringComparison.OrdinalIgnoreCase) >= 0)
                return AudioChannel.Music;
        }

        return AudioChannel.Sfx;
    }

    private float GetChannelMultiplier(AudioChannel channel)
    {
        return channel == AudioChannel.Music ? _musicVolumeNormalized : _sfxVolumeNormalized;
    }

    private void LoadSettings()
    {
        _masterVolumeNormalized = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, DefaultMasterVolume));
        _musicVolumeNormalized = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, DefaultMusicVolume));
        _sfxVolumeNormalized = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, DefaultSfxVolume));
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetFloat(MasterVolumeKey, _masterVolumeNormalized);
        PlayerPrefs.SetFloat(MusicVolumeKey, _musicVolumeNormalized);
        PlayerPrefs.SetFloat(SfxVolumeKey, _sfxVolumeNormalized);
        PlayerPrefs.Save();
    }

    private sealed class ManagedAudioSourceState
    {
        public AudioSource Source;
        public AudioChannel Channel;
        public float BaseVolume = 1f;
        public float LastAppliedMultiplier = 1f;
    }

    private enum AudioChannel
    {
        Sfx = 0,
        Music = 1
    }
}