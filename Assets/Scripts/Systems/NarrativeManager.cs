using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Manages narrative sequences with typewriter effect.
/// Handles input to skip/advance dialogue and transitions to the next scene.
/// </summary>
public class NarrativeManager : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI narrativeText;

    [SerializeField]
    private string[] narrativeSequence = new string[]
    {
        "The sun rises over the horizon...",
        "You wake up in front of your old shed.",
        "It's time to start your new life.",
        "Press Space, Enter, or Click to continue..."
    };

    [SerializeField]
    private float typewriterSpeed = 0.05f;

    [SerializeField]
    private string sceneToLoadAfter = "FarmScene";

    private int currentSentenceIndex = 0;
    private bool isTyping = false;
    private bool isCurrentSentenceFinished = false;
    private Coroutine typewriterCoroutine;

    private void Start()
    {
        if (narrativeText == null)
        {
            Debug.LogError("[NarrativeManager] TextMeshProUGUI component not assigned!");
            return;
        }

        // Mark that we're coming from the intro scene
        PlayerPrefs.SetInt("FromIntroScene", 1);
        PlayerPrefs.Save();

        StartNextSentence();
    }

    private void Update()
    {
        // Check for Space, Enter, or Mouse Click input
        if (Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter) ||
            Input.GetMouseButtonDown(0))
        {
            HandleInput();
        }
    }

    private void HandleInput()
    {
        if (isTyping)
        {
            // If still typing, finish the current sentence immediately
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
            }

            narrativeText.text = narrativeSequence[currentSentenceIndex];
            isTyping = false;
            isCurrentSentenceFinished = true;
        }
        else if (isCurrentSentenceFinished)
        {
            // Move to next sentence
            currentSentenceIndex++;

            if (currentSentenceIndex >= narrativeSequence.Length)
            {
                // All sentences finished - transition to main scene
                TransitionToNextScene();
            }
            else
            {
                StartNextSentence();
            }
        }
    }

    private void StartNextSentence()
    {
        isCurrentSentenceFinished = false;
        narrativeText.text = "";

        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }

        typewriterCoroutine = StartCoroutine(TypewriterEffect(narrativeSequence[currentSentenceIndex]));
    }

    private IEnumerator TypewriterEffect(string text)
    {
        isTyping = true;
        narrativeText.text = "";

        foreach (char character in text)
        {
            narrativeText.text += character;
            yield return new WaitForSeconds(typewriterSpeed);
        }

        isTyping = false;
        isCurrentSentenceFinished = true;
    }

    private void TransitionToNextScene()
    {
        // One-shot intro spawn at shed door when entering Farm.
        PlayerPrefs.DeleteKey("ReturnToFarmFrom");
        PlayerPrefs.DeleteKey("SkipSpawnManagerOnce");
        PlayerPrefs.SetInt("FromIntroScene", 1);
        PlayerPrefs.SetInt("ForceShedDoorSpawnOnce", 1);
        PlayerPrefs.DeleteKey("FarmTutorialStarted");
        PlayerPrefs.DeleteKey("FarmTutorialCompleted");
        PlayerPrefs.SetInt("PendingFarmTutorial", 1);
        PlayerPrefs.Save();

        Debug.Log($"[NarrativeManager] Transitioning to {sceneToLoadAfter}");
        SceneManager.LoadScene(sceneToLoadAfter);
    }
}
