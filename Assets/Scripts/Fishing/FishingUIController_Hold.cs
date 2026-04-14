using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;

/// <summary>
/// NEW: Manages the fishing mini-game UI with HOLD MECHANIC.
/// Shows a small panel with a moving fish that player must hold to catch.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class FishingUIController_Hold : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FishingMiniGameController miniGameController;
    [SerializeField] private OrderListHUD orderListHUD;

    [Header("Settings")]
    [SerializeField] private FishingSettings fishingSettings;

    // UI References
    private UIDocument uiDocument;
    private VisualElement root;
    private VisualElement holdPanel;

    // Hold mechanic UI elements
    private VisualElement fishTarget;
    private VisualElement catchZone;
    private ProgressBar holdProgressBar;
    private Label holdPercentageLabel;
    private Label statusLabel;
    private Label catchableNameLabel;

    // State
    private bool isUIActive = false;
    private bool inputEnabled = false;
    private bool fishInZone = false; // Track if fish is in catch zone

    private void Start()
    {
        InitializeUI();
        SubscribeToEvents();
    }

    private void InitializeUI()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("FishingUIController_Hold: No UIDocument found!");
            return;
        }

        root = uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("FishingUIController_Hold: Could not get root visual element!");
            return;
        }

        // Find the hold panel and its elements
        holdPanel = root.Q<VisualElement>("fishing-hold-panel");
        fishTarget = root.Q<VisualElement>("fish-target");
        catchZone = root.Q<VisualElement>("catch-zone");
        holdProgressBar = root.Q<ProgressBar>("hold-progress");
        holdPercentageLabel = root.Q<Label>("hold-percentage");
        statusLabel = root.Q<Label>("status-label");
        catchableNameLabel = root.Q<Label>("catchable-name-label");

        // Initial state: hide fishing UI
        if (holdPanel != null)
            holdPanel.style.display = DisplayStyle.None;

        Debug.Log("[FishingUIController_Hold] UI initialized successfully!");
    }

    private void SubscribeToEvents()
    {
        if (miniGameController == null)
        {
            Debug.LogError("FishingUIController_Hold: MiniGameController not assigned!");
            return;
        }

        miniGameController.OnFishPositionChanged += HandleFishPositionChanged;
        miniGameController.OnHoldProgressChanged += HandleHoldProgressChanged;
        miniGameController.OnCatchComplete += HandleCatchComplete;
    }

    private void OnDestroy()
    {
        if (miniGameController != null)
        {
            miniGameController.OnFishPositionChanged -= HandleFishPositionChanged;
            miniGameController.OnHoldProgressChanged -= HandleHoldProgressChanged;
            miniGameController.OnCatchComplete -= HandleCatchComplete;
        }
    }

    /// <summary>
    /// Opens the fishing UI and starts the mini-game.
    /// </summary>
    public void OpenFishingUI(LakeZoneDefinition zone = null)
    {
        if (isUIActive)
            return;

        isUIActive = true;
        inputEnabled = true;

        if (holdPanel != null)
        {
            holdPanel.style.display = DisplayStyle.Flex;
        }

        // Update fish name
        if (miniGameController != null)
        {
            miniGameController.StartFishing(zone);
            CatchableDefinition catchable = miniGameController.GetCurrentCatchable();
            if (catchable != null && catchableNameLabel != null)
            {
                catchableNameLabel.text = catchable.catchableName;
            }
        }

        // Initialize UI
        if (holdProgressBar != null)
            holdProgressBar.value = 0f;
        if (holdPercentageLabel != null)
            holdPercentageLabel.text = "0%";
        if (statusLabel != null)
            statusLabel.text = "Hold SPACEBAR when fish enters the circle!";
    }

    /// <summary>
    /// Closes the fishing UI and returns to the game world.
    /// </summary>
    public void CloseFishingUI()
    {
        if (!isUIActive)
            return;

        isUIActive = false;
        inputEnabled = false;

        if (holdPanel != null)
        {
            holdPanel.style.display = DisplayStyle.None;
        }
    }

    private void Update()
    {
        if (!isUIActive || !inputEnabled)
            return;

        // Update mini-game state
        miniGameController.UpdateFishing();

        // Handle hold input
        if (Input.GetKeyDown(KeyCode.Space))
        {
            miniGameController.OnPlayerInput_Hold();
        }
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            miniGameController.OnPlayerInput_ReleaseHold();
        }
    }

    /// <summary>
    /// Event handler: Fish position changed
    /// </summary>
    private void HandleFishPositionChanged(Vector2 position)
    {
        if (fishTarget == null)
            return;

        // Position is normalized (-1 to 1 range)
        // Translate fish based on normalized movement
        // Scale movement to visible area bounds
        float translateX = position.x * 100f; // ±100 pixels horizontal movement
        float translateY = position.y * 80f;  // ±80 pixels vertical movement

        // Use translate for smooth movement from centered origin
        fishTarget.style.translate = new Translate(new Length(translateX, LengthUnit.Pixel), new Length(translateY, LengthUnit.Pixel));

        // Check if fish is in catch zone
        float distanceToFish = Mathf.Sqrt(position.x * position.x + position.y * position.y);
        fishInZone = distanceToFish < 0.55f; // Same as in controller

        // Update visual feedback based on fish position
        if (statusLabel != null && !inputEnabled)
        {
            if (fishInZone)
            {
                statusLabel.text = "FISH IN ZONE! HOLD NOW!";
                statusLabel.style.color = new Color(0f, 1f, 0f);
            }
            else
            {
                statusLabel.text = "Move fish into the circle!";
                statusLabel.style.color = new Color(1f, 1f, 0.7f);
            }
        }
    }

    /// <summary>
    /// Event handler: Hold progress changed (0-1)
    /// </summary>
    private void HandleHoldProgressChanged(float progress)
    {
        if (holdProgressBar != null)
        {
            holdProgressBar.value = progress;
        }

        if (holdPercentageLabel != null)
        {
            holdPercentageLabel.text = $"{(progress * 100f):F0}%";
        }

        // Update status based on progress
        if (statusLabel != null && progress > 0)
        {
            if (progress < 0.33f)
            {
                statusLabel.text = "Getting closer...";
                statusLabel.style.color = new Color(1f, 1f, 0.5f);
            }
            else if (progress < 0.66f)
            {
                statusLabel.text = "Almost there! Hold tight!";
                statusLabel.style.color = new Color(1f, 1f, 0.3f);
            }
            else if (progress < 1f)
            {
                statusLabel.text = "KEEP HOLDING! You got this!";
                statusLabel.style.color = new Color(0.5f, 1f, 0.5f);
            }
        }
    }

    /// <summary>
    /// Event handler: Catch complete (success or failure)
    /// </summary>
    private void HandleCatchComplete(FishingResultType result)
    {
        inputEnabled = false;

        HandleCatchResult(result);
    }

    private void HandleCatchResult(FishingResultType result)
    {
        string resultText = "";
        string statusText = "";
        Color statusColor = Color.white;

        switch (result)
        {
            case FishingResultType.Success:
                resultText = "✓ CAUGHT!";
                statusText = "Success! You caught it!";
                statusColor = new Color(0.5f, 1f, 0.5f, 1f); // Green

                // Add catch to inventory
                if (miniGameController != null)
                {
                    CatchableDefinition catchable = miniGameController.GetCurrentCatchable();
                    if (catchable != null && catchable.inventoryItem != null)
                    {
                        bool success = InventoryController.Instance.TryAdd(catchable.inventoryItem, 1);
                        if (success)
                        {
                            Debug.Log($"[FishingUI] Added {catchable.catchableName} to inventory!");
                        }
                        else
                        {
                            Debug.LogWarning($"[FishingUI] Inventory full! Could not add {catchable.catchableName}");
                        }
                    }
                }
                break;

            case FishingResultType.MissedBite:
                resultText = "✗ MISSED!";
                statusText = "Fish got away - missed the reaction window!";
                statusColor = new Color(1f, 0.5f, 0.5f, 1f); // Red
                break;

            case FishingResultType.LineBroke:
                resultText = "✗ LINE BROKE!";
                statusText = "The line snapped under pressure!";
                statusColor = new Color(1f, 0.5f, 0.5f, 1f); // Red
                break;

            case FishingResultType.Escaped:
                resultText = "✗ ESCAPED!";
                statusText = "The fish got away!";
                statusColor = new Color(1f, 0.7f, 0.5f, 1f); // Orange
                break;

            case FishingResultType.TooMuchTension:
                resultText = "✗ TENSION!";
                statusText = "Too much tension on the line!";
                statusColor = new Color(1f, 0.5f, 0.5f, 1f); // Red
                break;
        }

        if (statusLabel != null)
        {
            statusLabel.text = statusText;
            statusLabel.style.color = statusColor;
        }

        // Close UI after delay
        StartCoroutine(CloseUIAfterDelay(2f));
    }

    private IEnumerator CloseUIAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CloseFishingUI();
    }
}
