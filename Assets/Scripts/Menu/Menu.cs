using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Main menu controller - handles New Game, Continue, Options, and Quit
/// </summary>
public class Menu : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsPanel;
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
    }

    private void Start()
    {
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
        OpenOptions();
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
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
        if (optionsPanel != null)
            optionsPanel.SetActive(true);
    }

    /// <summary>
    /// Close the options menu and return to main menu
    /// </summary>
    public void CloseOptions()
    {
        PlayButtonSound();
        if (optionsPanel != null)
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
}
