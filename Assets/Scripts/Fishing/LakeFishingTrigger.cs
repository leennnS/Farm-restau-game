using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

/// <summary>
/// Detects when the player enters a fishing zone, shows the interaction prompt,
/// and triggers the fishing mini-game when the player presses E.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LakeFishingTrigger : MonoBehaviour
{
    [Header("Fishing Setup")]
    [SerializeField] private LakeZoneDefinition lakeZone;
    [SerializeField] private FishingSettings fishingSettings;
    [SerializeField] private FishingMiniGameController miniGameController;

    [Header("UI")]
    [SerializeField] private FishingUIController_Hold fishingUIController; // NEW: Uses Hold mechanic UI
    private VisualElement promptElement;
    private UIDocument promptUIDocument;

    [Header("Interaction")]
    [SerializeField] private KeyCode fishKey = KeyCode.E;
    [SerializeField] private string playerTag = "Player";

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private bool playerInZone = false;
    private bool fishingActive = false;
    private GameObject cachedPlayer;
    private CharacterController2D cachedController;

    private void Start()
    {
        // Make sure collider is set as trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Auto-find the FishingUIController_Hold if not assigned
        if (fishingUIController == null)
        {
            fishingUIController = FindObjectOfType<FishingUIController_Hold>();
            if (fishingUIController == null)
            {
                Debug.LogWarning("LakeFishingTrigger: Could not find FishingUIController_Hold in scene. Please assign it in the Inspector or add the component to the scene.");
            }
        }

        // Find or create a prompt UI document
        InitializePromptUI();
    }

    private void InitializePromptUI()
    {
        // Look for an existing UI document for the prompt
        promptUIDocument = GetComponentInChildren<UIDocument>();

        if (promptUIDocument != null)
        {
            promptElement = promptUIDocument.rootVisualElement.Q<VisualElement>("fishing-prompt");
        }
        else
        {
            Debug.LogWarning("LakeFishingTrigger: No UIDocument found for prompt. Create one as child of this object.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerInZone = true;
        DebugLog("Player entered fishing zone.");
        ShowPrompt();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerInZone = false;
        DebugLog("Player left fishing zone.");
        HidePrompt();
    }

    private void Update()
    {
        if (!playerInZone || fishingActive)
            return;

        // Check for fish input (E key)
        if (Input.GetKeyDown(fishKey))
        {
            StartFishingSession();
        }
    }

    private void ShowPrompt()
    {
        if (promptElement == null)
        {
            DebugLog("Warning: Prompt element not found.");
            return;
        }

        promptElement.style.display = DisplayStyle.Flex;

        // Optional: add fade-in animation
        if (fishingSettings != null && gameObject.activeInHierarchy)
        {
            promptElement.style.opacity = 0f;
            // Simple fade-in using coroutine
            StartCoroutine(FadeInPrompt(fishingSettings.promptFadeInDuration));
        }
    }

    private void HidePrompt()
    {
        if (promptElement == null)
            return;

        // Optional: fade out
        if (fishingSettings != null && gameObject.activeInHierarchy)
        {
            StartCoroutine(FadeOutPrompt(fishingSettings.promptFadeOutDuration));
        }
        else
        {
            promptElement.style.display = DisplayStyle.None;
        }
    }

    private IEnumerator FadeInPrompt(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            promptElement.style.opacity = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        promptElement.style.opacity = 1f;
    }

    private IEnumerator FadeOutPrompt(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            promptElement.style.opacity = Mathf.Clamp01(1f - (elapsed / duration));
            yield return null;
        }

        promptElement.style.display = DisplayStyle.None;
    }

    private void StartFishingSession()
    {
        if (lakeZone == null)
        {
            Debug.LogError("LakeFishingTrigger: Lake zone not assigned!");
            return;
        }

        if (miniGameController == null)
        {
            Debug.LogError("LakeFishingTrigger: MiniGameController not assigned!");
            return;
        }

        if (fishingUIController == null)
        {
            Debug.LogError("LakeFishingTrigger: FishingUIController_Hold not assigned and could not be found in scene! Add the FishingUIController_Hold component to a GameObject in the scene.");
            return;
        }

        DebugLog("Starting fishing session!");
        fishingActive = true;

        cachedPlayer = PlayerSetupPipeline.FindPlayerInLoadedScenes();
        if (cachedPlayer == null)
            cachedPlayer = GameObject.FindWithTag(playerTag);

        if (cachedPlayer != null)
        {
            PlayerSetupPipeline.PreparePlayerForSceneChange();
            cachedController = cachedPlayer.GetComponent<CharacterController2D>();
            if (cachedController != null)
                cachedController.SetMovementLocked(true);
        }

        // Hide the prompt
        HidePrompt();

        // Open the UI and start fishing with the zone
        fishingUIController.OpenFishingUI(lakeZone);

        // Re-enable movement after fishing
        StartCoroutine(WaitForFishingComplete());
    }

    private IEnumerator WaitForFishingComplete()
    {
        // Wait until fishing UI is closed - check if FishingMiniGameController is in Complete state
        FishingState lastState = FishingState.Casting;

        while (miniGameController != null && miniGameController.GetCurrentState() != FishingState.Complete)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // Wait a bit more for UI to close
        yield return new WaitForSeconds(0.5f);

        fishingActive = false;
        DebugLog("Fishing session ended. Ready for next session.");

        if (cachedController != null)
            cachedController.SetMovementLocked(false);

        if (cachedPlayer != null)
        {
            PlayerSetupPipeline.PreparePlayerForSceneChange();
            CameraFollowFix.RebindAllCamerasTo(cachedPlayer.transform);
        }

        cachedController = null;
        cachedPlayer = null;

        // Show prompt again if player still in zone
        if (playerInZone)
        {
            ShowPrompt();
        }
    }

    private void DebugLog(string message)
    {
        if (debugLogs || (fishingSettings != null && fishingSettings.debugMode))
        {
            Debug.Log($"[LakeFishing] {message}");
        }
    }

    // Inspector helper
    public LakeZoneDefinition GetLakeZone() => lakeZone;
    public bool IsPlayerInZone() => playerInZone;
    public bool IsFishingActive() => fishingActive;
}
