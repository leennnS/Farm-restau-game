using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Enhanced NarrativeManager with lantern interaction phase.
/// Guides player through waking up, finding lantern, turning it on, then transitioning.
/// </summary>
public class NarrativeManagerEnhanced : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI narrativeText;

    [SerializeField]
    private string[] wakeUpNarrative = new string[]
    {
        "Your eyes flutter open...",
        "The darkness is overwhelming.",
        "A shed. Your shed. But you can barely see.",
        "There... a lantern on the table.",
        "Press E to pick it up."
    };

    [SerializeField]
    private string[] lanternLitNarrative = new string[]
    {
        "Light floods the darkness.",
        "You can see clearly now.",
        "It's time to face the day.",
        "Press Space, Enter, or Click to continue outside..."
    };

    [SerializeField]
    private float typewriterSpeed = 0.05f;

    [SerializeField]
    private string sceneToLoadAfter = "FarmScene";

    [SerializeField]
    private KeyCode lanternPickupKey = KeyCode.E;

    private int currentSentenceIndex = 0;
    private bool isTyping = false;
    private bool isCurrentSentenceFinished = false;
    private Coroutine typewriterCoroutine;

    private LanternController lantern;
    private CharacterController2D playerController;
    private bool lanternPhaseActive = false;
    private bool allNarrativeComplete = false;

    private void Start()
    {
        if (narrativeText == null)
        {
            Debug.LogError("[NarrativeManager] TextMeshProUGUI component not assigned!");
            return;
        }

        lantern = FindFirstObjectByType<LanternController>();
        playerController = FindFirstObjectByType<CharacterController2D>();

        // Freeze player during intro so they can't move around
        if (playerController != null)
        {
            playerController.enabled = false;
            Debug.Log("[NarrativeManager] Player frozen during intro");
        }

        // Mark that we're coming from the intro scene
        PlayerPrefs.SetInt("FromIntroScene", 1);
        PlayerPrefs.Save();

        StartNextSentence();
    }

    private void Update()
    {
        // During wake-up phase: Space/Enter/Click to advance narrative
        if (!lanternPhaseActive &&
            (Input.GetKeyDown(KeyCode.Space) ||
             Input.GetKeyDown(KeyCode.Return) ||
             Input.GetKeyDown(KeyCode.KeypadEnter) ||
             Input.GetMouseButtonDown(0)))
        {
            HandleNarrativeInput();
        }

        // During lantern phase: Check if lantern is lit
        if (lanternPhaseActive && lantern != null && lantern.IsLit && !allNarrativeComplete)
        {
            StartNextSentence(); // Begin lantern-lit narrative
            lanternPhaseActive = false;
        }

        // After all narrative: Space/Enter/Click to transition
        if (allNarrativeComplete &&
            (Input.GetKeyDown(KeyCode.Space) ||
             Input.GetKeyDown(KeyCode.Return) ||
             Input.GetKeyDown(KeyCode.KeypadEnter) ||
             Input.GetMouseButtonDown(0)))
        {
            TransitionToNextScene();
        }
    }

    private void HandleNarrativeInput()
    {
        if (isTyping)
        {
            // If still typing, finish the current sentence immediately
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
            }

            narrativeText.text = wakeUpNarrative[currentSentenceIndex];
            isTyping = false;
            isCurrentSentenceFinished = true;
        }
        else if (isCurrentSentenceFinished)
        {
            // Move to next sentence
            currentSentenceIndex++;

            if (currentSentenceIndex >= wakeUpNarrative.Length)
            {
                // Wake-up narrative done - wait for lantern to be lit
                narrativeText.text = $"Press {lanternPickupKey} to pick up the lantern...";
                lanternPhaseActive = true;
                return;
            }

            StartNextSentence();
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

        // If we were in lantern phase, start the lantern-lit narrative
        if (lanternPhaseActive == false && currentSentenceIndex >= wakeUpNarrative.Length)
        {
            StartCoroutine(TypewriterLanternNarrative());
        }
        else if (currentSentenceIndex < wakeUpNarrative.Length)
        {
            typewriterCoroutine = StartCoroutine(TypewriterEffect(wakeUpNarrative[currentSentenceIndex]));
        }
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

    private IEnumerator TypewriterLanternNarrative()
    {
        for (int i = 0; i < lanternLitNarrative.Length; i++)
        {
            yield return StartCoroutine(TypewriterEffect(lanternLitNarrative[i]));

            // Wait for player to advance (except on last sentence)
            if (i < lanternLitNarrative.Length - 1)
            {
                yield return new WaitUntil(() =>
                    Input.GetKeyDown(KeyCode.Space) ||
                    Input.GetKeyDown(KeyCode.Return) ||
                    Input.GetKeyDown(KeyCode.KeypadEnter) ||
                    Input.GetMouseButtonDown(0));
            }
        }

        allNarrativeComplete = true;
    }

    private void TransitionToNextScene()
    {
        // Unfreeze player before transitioning
        if (playerController != null)
        {
            playerController.enabled = true;
            Debug.Log("[NarrativeManager] Player unfrozen, transitioning to farm...");
        }

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
