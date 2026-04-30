using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Main menu controller - handles New Game, Continue, Options, and Quit
/// </summary>
public class Menu : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    public GameObject optionsPanel;
    [SerializeField] private GameObject continueButton; // Can be disabled if no save exists

    [Header("Audio")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private float buttonClickVolume = 0.7f;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    private AudioSource _audioSource;
    private bool _syncingVolumeControls;

    private void Awake()
    {
        // Try to get AudioSource, create if doesn't exist
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        _audioSource.spatialBlend = 0f;

        ResolvePanels();
        ResolveVolumeControls();

        if (AudioSettingsManager.HasInstance)
            AudioSettingsManager.Instance.RefreshAudioSource(_audioSource);
    }

    private void Start()
    {
        ResolvePanels();
        ResolveVolumeControls();

        if (IsValidPanelTarget(optionsPanel))
            optionsPanel.SetActive(false);
        else if (optionsPanel != null)
            Debug.LogWarning("[Menu] optionsPanel currently points to a non-panel object. Skipping auto-hide.");

        BindAudioSettings();
        SyncVolumeControlsFromSettings();

        // Update Continue button visibility based on whether save exists
        UpdateContinueButtonState();
    }

    private void OnEnable()
    {
        AudioSettingsManager.Instance.SettingsChanged += SyncVolumeControlsFromSettings;
    }

    private void OnDisable()
    {
        if (AudioSettingsManager.HasInstance)
            AudioSettingsManager.Instance.SettingsChanged -= SyncVolumeControlsFromSettings;

        UnbindAudioSettings();
    }

    /// <summary>
    /// Check if a save exists and update Continue button accordingly
    /// </summary>
    private void UpdateContinueButtonState()
    {
        if (continueButton != null)
        {
            bool hasSave = GameManager.Instance.HasExistingSave();
            continueButton.SetActive(hasSave);
        }
    }

    /// <summary>
    /// New Game - start fresh from the beginning
    /// </summary>
    public void OnNewGameClicked()
    {
        PlayButtonSound();
        Debug.Log("[Menu] New Game clicked");
        GameManager.Instance.NewGame();
    }

    /// <summary>
    /// Continue - load and resume previous session
    /// </summary>
    public void OnContinueClicked()
    {
        PlayButtonSound();
        Debug.Log("[Menu] Continue clicked");

        if (GameManager.Instance.HasExistingSave())
        {
            GameManager.Instance.ContinueGame();
        }
        else
        {
            Debug.LogWarning("[Menu] Continue clicked but no save exists. Starting new game instead.");
            GameManager.Instance.NewGame();
        }
    }

    /// <summary>
    /// Options - open settings menu
    /// </summary>
    public void OnOptionsClicked()
    {
        PlayButtonSound();
        Debug.Log("[Menu] Options clicked");
        ToggleOptions();
    }

    public void ToggleOptions()
    {
        ResolvePanels();
        ResolveVolumeControls();

        if (!IsValidPanelTarget(optionsPanel))
        {
            Debug.LogWarning("[Menu] ToggleOptions failed: optionsPanel is not assigned or could not be resolved.");
            return;
        }

        bool nextActive = !optionsPanel.activeSelf;
        optionsPanel.SetActive(nextActive);

        if (nextActive)
            SyncVolumeControlsFromSettings();

        if (nextActive)
            optionsPanel.transform.SetAsLastSibling();

        Debug.Log($"[Menu] ToggleOptions -> optionsPanel active: {nextActive}");
    }

    /// <summary>
    /// Quit - exit the application
    /// </summary>
    public void OnQuitClicked()
    {
        PlayButtonSound();
        Debug.Log("[Menu] Quit clicked");

        // Save any pending game state before quitting
        if (GameManager.HasInstance)
        {
            GameManager.Instance.SaveGameState();
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    /// <summary>
    /// Open the options/settings menu
    /// </summary>
    public void OpenOptions()
    {
        ResolvePanels();
        ResolveVolumeControls();

        if (IsValidPanelTarget(optionsPanel))
        {
            optionsPanel.SetActive(true);
            optionsPanel.transform.SetAsLastSibling();
            SyncVolumeControlsFromSettings();
        }

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    /// <summary>
    /// Close the options menu and return to main menu
    /// </summary>
    public void CloseOptions()
    {
        PlayButtonSound();
        ResolvePanels();

        if (IsValidPanelTarget(optionsPanel))
            optionsPanel.SetActive(false);
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    /// <summary>
    /// Play button click sound effect
    /// </summary>
    private void PlayButtonSound()
    {
        if (_audioSource != null && buttonClickSound != null)
        {
            if (AudioSettingsManager.HasInstance)
                AudioSettingsManager.Instance.RefreshAudioSource(_audioSource);

            _audioSource.PlayOneShot(buttonClickSound, buttonClickVolume);
        }
    }

    private void BindAudioSettings()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeSliderChanged);

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeSliderChanged);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeSliderChanged);
    }

    private void UnbindAudioSettings()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeSliderChanged);

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeSliderChanged);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeSliderChanged);
    }

    private void ResolveVolumeControls()
    {
        if (!IsValidPanelTarget(optionsPanel))
            return;

        Transform optionsRoot = optionsPanel.transform;

        if (masterVolumeSlider == null)
            masterVolumeSlider = FindSlider(optionsRoot, "Sound (1)", "Sound");

        if (musicVolumeSlider == null)
            musicVolumeSlider = FindSlider(optionsRoot, "Music");

        if (sfxVolumeSlider == null)
            sfxVolumeSlider = FindSlider(optionsRoot, "Sound", "Sound (1)");
    }

    private void SyncVolumeControlsFromSettings()
    {
        if (!AudioSettingsManager.HasInstance)
            return;

        _syncingVolumeControls = true;

        if (masterVolumeSlider != null)
            masterVolumeSlider.SetValueWithoutNotify(AudioSettingsManager.Instance.MasterVolumeNormalized * 100f);

        if (musicVolumeSlider != null)
            musicVolumeSlider.SetValueWithoutNotify(AudioSettingsManager.Instance.MusicVolumeNormalized * 100f);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.SetValueWithoutNotify(AudioSettingsManager.Instance.SfxVolumeNormalized * 100f);

        _syncingVolumeControls = false;
    }

    private void OnMasterVolumeSliderChanged(float value)
    {
        if (_syncingVolumeControls)
            return;

        AudioSettingsManager.Instance.SetMasterVolumeNormalized(value / 100f);
    }

    private void OnMusicVolumeSliderChanged(float value)
    {
        if (_syncingVolumeControls)
            return;

        AudioSettingsManager.Instance.SetMusicVolumeNormalized(value / 100f);
    }

    private void OnSfxVolumeSliderChanged(float value)
    {
        if (_syncingVolumeControls)
            return;

        AudioSettingsManager.Instance.SetSfxVolumeNormalized(value / 100f);
    }

    private Slider FindSlider(Transform root, params string[] possibleNames)
    {
        if (root == null || possibleNames == null || possibleNames.Length == 0)
            return null;

        for (int i = 0; i < possibleNames.Length; i++)
        {
            Transform match = FindInHierarchy(root, possibleNames[i]);
            if (match == null)
                continue;

            Slider slider = match.GetComponent<Slider>();
            if (slider != null)
                return slider;
        }

        return null;
    }

    private Transform FindInHierarchy(Transform current, string targetName)
    {
        if (current == null || string.IsNullOrEmpty(targetName))
            return null;

        if (string.Equals(current.name, targetName, System.StringComparison.OrdinalIgnoreCase))
            return current;

        for (int i = 0; i < current.childCount; i++)
        {
            Transform found = FindInHierarchy(current.GetChild(i), targetName);
            if (found != null)
                return found;
        }

        return null;
    }

    private void ResolvePanels()
    {
        mainMenuPanel = ResolvePanelReference(mainMenuPanel, "MainMenuPanel", "Main Menu Panel", "MenuPanel", "MainMenu", "Menu");

        optionsPanel = ResolvePanelReference(optionsPanel, "OptionsPanel", "Options Panel", "OptionsMenu", "SettingsPanel", "Settings Panel", "Options");
    }

    private GameObject ResolvePanelReference(GameObject current, params string[] possibleNames)
    {
        if (IsMatchingPanel(current, possibleNames))
            return current;

        GameObject resolved = FindPanelByName(possibleNames);
        return IsMatchingPanel(resolved, possibleNames) ? resolved : current;
    }

    private bool IsValidPanelTarget(GameObject panel)
    {
        if (panel == null)
            return false;

        // A panel container should not be a clickable button itself.
        return panel.GetComponent<Button>() == null;
    }

    private bool IsMatchingPanel(GameObject panel, params string[] possibleNames)
    {
        if (panel == null || possibleNames == null)
            return false;

        // If a reference points at a UI button, it is not a panel container.
        if (panel.GetComponent<Button>() != null)
            return false;

        for (int i = 0; i < possibleNames.Length; i++)
        {
            if (string.Equals(panel.name, possibleNames[i], System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private GameObject FindPanelByName(params string[] possibleNames)
    {
        if (possibleNames == null || possibleNames.Length == 0)
            return null;

        GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject found = FindInHierarchy(roots[i].transform, possibleNames);
            if (found != null)
                return found;
        }

        return null;
    }

    private GameObject FindInHierarchy(Transform current, string[] possibleNames)
    {
        if (current == null)
            return null;

        for (int i = 0; i < possibleNames.Length; i++)
        {
            if (string.Equals(current.name, possibleNames[i], System.StringComparison.OrdinalIgnoreCase))
            {
                if (current.GetComponent<Button>() == null)
                    return current.gameObject;

                break;
            }
        }

        for (int i = 0; i < current.childCount; i++)
        {
            GameObject found = FindInHierarchy(current.GetChild(i), possibleNames);
            if (found != null)
                return found;
        }

        return null;
    }
}
