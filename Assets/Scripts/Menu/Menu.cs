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

    private AudioSource _audioSource;

    private void Awake()
    {
        // Try to get AudioSource, create if doesn't exist
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        ResolvePanels();
    }

    private void Start()
    {
        ResolvePanels();

        if (IsValidPanelTarget(optionsPanel))
            optionsPanel.SetActive(false);
        else if (optionsPanel != null)
            Debug.LogWarning("[Menu] optionsPanel currently points to a non-panel object. Skipping auto-hide.");

        // Update Continue button visibility based on whether save exists
        UpdateContinueButtonState();
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

        if (!IsValidPanelTarget(optionsPanel))
        {
            Debug.LogWarning("[Menu] ToggleOptions failed: optionsPanel is not assigned or could not be resolved.");
            return;
        }

        bool nextActive = !optionsPanel.activeSelf;
        optionsPanel.SetActive(nextActive);

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

        if (IsValidPanelTarget(optionsPanel))
        {
            optionsPanel.SetActive(true);
            optionsPanel.transform.SetAsLastSibling();
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
            _audioSource.PlayOneShot(buttonClickSound, buttonClickVolume);
        }
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
