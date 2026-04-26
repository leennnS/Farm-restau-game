using System;
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
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject continueButton; // Can be disabled if no save exists

    [Header("Audio")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private float buttonClickVolume = 0.7f;

    private AudioSource _audioSource;

    private void Awake()
    {
        AutoResolvePanelReferences();

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
        AutoResolvePanelReferences();

        if (optionsPanel == null)
        {
            Debug.LogError("[Menu] Cannot open options: optionsPanel is not assigned in the inspector.");
            return;
        }

        bool optionsNestedUnderMainMenu = IsOptionsNestedUnderMainMenu();

        // If Options is a child of Main Menu, disabling Main Menu would hide Options too.
        if (!optionsNestedUnderMainMenu && mainMenuPanel != null)
            mainMenuPanel.SetActive(false);

        optionsPanel.SetActive(true);
        optionsPanel.transform.SetAsLastSibling();

        CanvasGroup optionsGroup = optionsPanel.GetComponent<CanvasGroup>();
        if (optionsGroup != null)
        {
            optionsGroup.alpha = 1f;
            optionsGroup.interactable = true;
            optionsGroup.blocksRaycasts = true;
        }

        Debug.Log($"[Menu] OpenOptions -> mainMenuPanel:{(mainMenuPanel != null ? mainMenuPanel.name : "null")} activeSelf={(mainMenuPanel != null && mainMenuPanel.activeSelf)} | optionsPanel:{optionsPanel.name} activeSelf={optionsPanel.activeSelf}");
    }

    /// <summary>
    /// Close the options menu and return to main menu
    /// </summary>
    public void CloseOptions()
    {
        PlayButtonSound();
        AutoResolvePanelReferences();

        if (optionsPanel == null)
        {
            Debug.LogError("[Menu] Cannot close options: optionsPanel is not assigned in the inspector.");
            return;
        }

        bool optionsNestedUnderMainMenu = IsOptionsNestedUnderMainMenu();

        if (optionsPanel != null)
            optionsPanel.SetActive(false);

        if (!optionsNestedUnderMainMenu && mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    private void AutoResolvePanelReferences()
    {
        if (mainMenuPanel == null || mainMenuPanel.GetComponent<Button>() != null)
        {
            GameObject resolvedMain = FindPanelOnAnyCanvas("Menu");
            if (resolvedMain != null)
                mainMenuPanel = resolvedMain;
        }

        if (optionsPanel == null || optionsPanel.GetComponent<Button>() != null)
        {
            GameObject resolvedOptions = FindPanelOnAnyCanvas("Options");
            if (resolvedOptions != null)
                optionsPanel = resolvedOptions;
        }
    }

    private static GameObject FindPanelOnAnyCanvas(string panelName)
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            RectTransform[] rects = canvas.GetComponentsInChildren<RectTransform>(true);
            foreach (RectTransform rect in rects)
            {
                GameObject go = rect.gameObject;
                if (go == null || go == canvas.gameObject)
                    continue;

                if (!string.Equals(go.name, panelName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (go.GetComponent<Button>() != null)
                    continue;

                return go;
            }
        }

        return null;
    }

    private bool IsOptionsNestedUnderMainMenu()
    {
        if (mainMenuPanel == null || optionsPanel == null)
            return false;

        return optionsPanel.transform.IsChildOf(mainMenuPanel.transform);
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
