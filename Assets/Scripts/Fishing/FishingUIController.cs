using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;

/// <summary>
/// Manages the fishing mini-game UI. Connects FishingMiniGameController events to UI elements.
/// Uses UI Toolkit (UXML + USS) for all UI presentation.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class FishingUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FishingMiniGameController miniGameController;
    [SerializeField] private InventoryController inventoryController;
    [SerializeField] private OrderListHUD orderListHUD;

    [Header("Settings")]
    [SerializeField] private FishingSettings fishingSettings;

    // UI References
    private UIDocument uiDocument;
    private VisualElement root;

    // Panels
    private VisualElement fishingPanel;
    private VisualElement castingPanel;
    private VisualElement waitingPanel;
    private VisualElement biteReactionPanel;
    private VisualElement catchPanel;
    private VisualElement resultPanel;

    // UI Elements
    private ProgressBar tensionBar;
    private ProgressBar catchProgressBar;
    private Label catchProgressLabel;
    private Label rhythmLabel;
    private Label phaseLabel;
    private Label tensionLabel;
    private Button reelButton;
    private Button biteReelButton;
    private Label catchableNameLabel;
    private Image catchableImage;

    // Rhythm energy tracking
    private float rhythmEnergy = 1f; // 0-1
    private float rhythmDecayRate = 0.08f; // Energy decreases per second - SLOWER decay

    // Result screen elements
    private Label resultStatusLabel;
    private Image resultCatchableImage;
    private Label resultDescriptionLabel;
    private Button continueButton;

    // State
    private bool isUIActive = false;
    private bool inputEnabled = false;

    // Animation tracking
    private Color targetTensionColor = Color.white;
    private float biteShakeTimer = 0f;
    private bool isShaking = false;

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
            Debug.LogError("FishingUIController: No UIDocument found!");
            return;
        }

        root = uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("FishingUIController: Could not get root visual element!");
            return;
        }

        // Find UI panels
        fishingPanel = root.Q<VisualElement>("fishing-panel");
        castingPanel = root.Q<VisualElement>("casting-panel");
        waitingPanel = root.Q<VisualElement>("waiting-panel");
        biteReactionPanel = root.Q<VisualElement>("bite-reaction-panel");
        catchPanel = root.Q<VisualElement>("catch-panel");
        resultPanel = root.Q<VisualElement>("result-panel");

        // Find UI elements
        tensionBar = root.Q<ProgressBar>("tension-bar");
        catchProgressBar = root.Q<ProgressBar>("catch-progress");
        catchProgressLabel = root.Q<Label>("catch-progress-label");
        rhythmLabel = root.Q<Label>("rhythm-label");
        phaseLabel = root.Q<Label>("phase-label");
        tensionLabel = root.Q<Label>("tension-label");
        reelButton = root.Q<Button>("reel-button");
        biteReelButton = root.Q<Button>("bite-reel-button");
        catchableNameLabel = root.Q<Label>("catchable-name-label");
        catchableImage = root.Q<Image>("catchable-image");

        // Result screen
        resultStatusLabel = root.Q<Label>("result-status-label");
        resultCatchableImage = root.Q<Image>("result-catchable-image");
        resultDescriptionLabel = root.Q<Label>("result-description-label");
        continueButton = root.Q<Button>("continue-button");

        // Setup button listeners
        if (biteReelButton != null)
        {
            biteReelButton.clicked += OnBiteReelButtonClicked;
        }

        if (reelButton != null)
        {
            reelButton.clicked += OnReelButtonClicked;
        }

        if (continueButton != null)
        {
            continueButton.clicked += OnContinueButtonClicked;
        }

        // Initial state: hide fishing UI
        if (fishingPanel != null)
            fishingPanel.style.display = DisplayStyle.None;
        if (resultPanel != null)
            resultPanel.style.display = DisplayStyle.None;

        UpdatePhaseUI(FishingState.Casting);
    }

    private void SubscribeToEvents()
    {
        if (miniGameController == null)
        {
            Debug.LogError("FishingUIController: MiniGameController not assigned!");
            return;
        }

        miniGameController.OnBiteOccurred += HandleBiteOccurred;
        miniGameController.OnTensionChanged += HandleTensionChanged;
        miniGameController.OnCatchComplete += HandleCatchComplete;
    }

    private void OnDestroy()
    {
        if (miniGameController != null)
        {
            miniGameController.OnBiteOccurred -= HandleBiteOccurred;
            miniGameController.OnTensionChanged -= HandleTensionChanged;
            miniGameController.OnCatchComplete -= HandleCatchComplete;
        }

        if (biteReelButton != null)
            biteReelButton.clicked -= OnBiteReelButtonClicked;

        if (reelButton != null)
            reelButton.clicked -= OnReelButtonClicked;

        if (continueButton != null)
            continueButton.clicked -= OnContinueButtonClicked;
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
        rhythmEnergy = 1f; // Reset energy for new catch

        if (fishingPanel != null)
        {
            fishingPanel.style.display = DisplayStyle.Flex;
        }

        // Start the mini-game with the specified zone
        if (miniGameController != null)
            miniGameController.StartFishing(zone);
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

        if (fishingPanel != null)
        {
            fishingPanel.style.display = DisplayStyle.None;
        }

        if (resultPanel != null)
        {
            resultPanel.style.display = DisplayStyle.None;
        }
    }

    private void Update()
    {
        // Handle bite shake animation
        if (isShaking)
        {
            biteShakeTimer -= Time.deltaTime;
            if (biteShakeTimer <= 0f)
            {
                isShaking = false;
                // Reset position
                if (catchPanel != null)
                    catchPanel.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(50));
            }
            else
            {
                // Shake effect
                if (catchPanel != null)
                {
                    float shake = Mathf.Sin(biteShakeTimer * 20f) * 8f;
                    catchPanel.style.translate = new Translate(new Length(shake, LengthUnit.Pixel), 0);
                }
            }
        }

        // Smooth tension bar color transitions
        if (tensionBar != null && targetTensionColor != Color.clear)
        {
            Color currentColor = tensionBar.style.backgroundColor.value;
            Color smoothColor = Color.Lerp(currentColor, targetTensionColor, Time.deltaTime * 5f);
            tensionBar.style.backgroundColor = smoothColor;
        }

        // Original game logic
        if (!isUIActive || !inputEnabled)
            return;

        // Update mini-game state
        miniGameController.UpdateFishing();

        // Handle phase display
        UpdatePhaseUI(miniGameController.GetCurrentState());

        // Handle player input during bite reaction phase
        if (miniGameController.GetCurrentState() == FishingState.Biting)
        {
            HandleBitePhaseInput();
        }

        // Handle player input during catch phase
        if (miniGameController.GetCurrentState() == FishingState.Catching)
        {
            HandleCatchPhaseInput();
            UpdateCatchProgress();
        }
    }

    private void UpdateCatchProgress()
    {
        // Decay rhythm energy over time
        rhythmEnergy = Mathf.Max(0f, rhythmEnergy - (rhythmDecayRate * Time.deltaTime));

        if (catchProgressBar != null)
        {
            float progress = miniGameController.GetCatchPhaseProgress();
            catchProgressBar.value = progress;
        }

        if (catchProgressLabel != null)
        {
            float progress = miniGameController.GetCatchPhaseProgress();
            float tension = miniGameController.GetCurrentTension();
            catchProgressLabel.text = $"TAP SPACE! Progress: {progress:P0} | Energy: {rhythmEnergy:P0} | Tension: {tension:P0}";
        }

        if (rhythmLabel != null)
        {
            if (rhythmEnergy < 0.3f)
            {
                rhythmLabel.text = "⚡ HOLD NOW OR FISH ESCAPES! ⚡";
                rhythmLabel.style.color = new Color(1f, 0.2f, 0.2f); // Red warning
            }
            else if (rhythmEnergy < 0.6f)
            {
                rhythmLabel.text = "⚡ KEEP HOLDING SPACE! ⚡";
                rhythmLabel.style.color = new Color(1f, 0.8f, 0.2f); // Yellow caution
            }
            else
            {
                rhythmLabel.text = "⚡ HOLD SPACE TO CATCH! ⚡";
                rhythmLabel.style.color = new Color(0.2f, 1f, 0.2f); // Green good
            }
        }

        // Fail if energy hits zero
        if (rhythmEnergy <= 0f)
        {
            Debug.Log("[FishingUI] Fish escaped! Energy depleted!");
            miniGameController.CompleteCatch(FishingResultType.Escaped);
            inputEnabled = false;
        }
    }

    private void HandleBitePhaseInput()
    {
        // React to bite - press Space or click button
        if (Input.GetKeyDown(KeyCode.Space))
        {
            miniGameController.OnPlayerInput_Reel();
        }
    }

    private void HandleCatchPhaseInput()
    {
        // Hold SPACE to continuously reel and restore energy
        if (Input.GetKey(KeyCode.Space))
        {
            miniGameController.OnPlayerInput_Reel();
            // Continuously boost rhythm energy while holding
            rhythmEnergy = Mathf.Min(1f, rhythmEnergy + (0.25f * Time.deltaTime)); // Smooth energy restoration
        }
        else
        {
            // Release to stop reeling when not holding
            miniGameController.OnPlayerInput_Release();
        }
    }

    private void UpdatePhaseUI(FishingState state)
    {
        // Hide all panels
        HideAllPhasePanels();

        switch (state)
        {
            case FishingState.Casting:
                if (castingPanel != null)
                    castingPanel.style.display = DisplayStyle.Flex;
                UpdatePhaseLabel("Getting ready...");
                break;

            case FishingState.Anticipating:
                if (waitingPanel != null)
                    waitingPanel.style.display = DisplayStyle.Flex;
                UpdatePhaseLabel("Waiting for a bite...");
                break;

            case FishingState.Biting:
                if (biteReactionPanel != null)
                    biteReactionPanel.style.display = DisplayStyle.Flex;
                UpdatePhaseLabel("BITE! React now!");
                break;

            case FishingState.Catching:
                if (catchPanel != null)
                    catchPanel.style.display = DisplayStyle.Flex;

                // Show the fish image during catching
                if (catchableImage != null && miniGameController != null)
                {
                    CatchableDefinition catchable = miniGameController.GetCurrentCatchable();
                    if (catchable != null && catchable.catchUICatchableImage != null)
                    {
                        catchableImage.image = catchable.catchUICatchableImage;
                    }
                }

                UpdatePhaseLabel("Reel in! Manage tension!");
                break;

            case FishingState.Complete:
                // Result panel handled separately
                break;
        }
    }

    private void HideAllPhasePanels()
    {
        if (castingPanel != null) castingPanel.style.display = DisplayStyle.None;
        if (waitingPanel != null) waitingPanel.style.display = DisplayStyle.None;
        if (biteReactionPanel != null) biteReactionPanel.style.display = DisplayStyle.None;
        if (catchPanel != null) catchPanel.style.display = DisplayStyle.None;
    }

    private void UpdatePhaseLabel(string text)
    {
        if (phaseLabel != null)
            phaseLabel.text = text;
    }

    private void HandleBiteOccurred()
    {
        // Visual/audio feedback for bite
        Debug.Log("[FishingUI] Bite occurred!");

        // Shake animation on catch panel
        if (catchPanel != null)
        {
            isShaking = true;
            biteShakeTimer = 0.3f; // Shake for 0.3 seconds
        }

        // Tension bar pulse
        if (tensionBar != null)
        {
            // Brief color flash
            tensionBar.style.backgroundColor = new Color(1f, 1f, 0.2f); // Bright yellow
        }

        // Scale up the catchable image briefly
        if (catchableImage != null)
        {
            StartCoroutine(ScaleImageBriefly(catchableImage, 1.1f, 0.15f));
        }

        // Could play sound effect, shake screen, etc.
        if (fishingSettings != null && fishingSettings.enableBiteVibration)
        {
            // Haptic feedback could be implemented here
        }
    }

    private void HandleTensionChanged(float newTension)
    {
        if (tensionBar != null)
        {
            tensionBar.value = newTension;

            // Pulse animation on high tension
            if (newTension > 0.75f)
            {
                StartCoroutine(PulseTensionBar());
            }
        }

        if (tensionLabel != null)
        {
            tensionLabel.text = $"Tension: {newTension:P0}";
        }

        // Change color based on tension level with smooth transitions
        if (tensionBar != null && fishingSettings != null)
        {
            if (newTension < fishingSettings.relaxedTensionZoneMax)
            {
                // Green - safe zone
                targetTensionColor = new Color(0.2f, 0.8f, 0.2f);
            }
            else if (newTension < fishingSettings.warningTensionStart)
            {
                // Yellow - caution
                targetTensionColor = new Color(1f, 0.8f, 0.2f);
            }
            else if (newTension < fishingSettings.lineBreakerThreshold)
            {
                // Red - danger
                targetTensionColor = new Color(1f, 0.2f, 0.2f);
            }
            else
            {
                // Dark red - critical
                targetTensionColor = new Color(0.8f, 0f, 0f);
            }
        }
    }

    private void HandleCatchComplete(FishingResultType result)
    {
        inputEnabled = false;
        ShowResultScreen(result);
    }

    private void ShowResultScreen(FishingResultType result)
    {
        HideAllPhasePanels();

        if (resultPanel != null)
        {
            resultPanel.style.display = DisplayStyle.Flex;
        }

        CatchableDefinition catchable = miniGameController.GetCurrentCatchable();

        // Update result UI based on outcome
        if (resultStatusLabel != null)
        {
            resultStatusLabel.text = GetResultText(result);
            resultStatusLabel.style.color = GetResultColor(result);
        }

        if (result == FishingResultType.Success && catchable != null)
        {
            if (resultCatchableImage != null && catchable.catchUICatchableImage != null)
            {
                resultCatchableImage.image = catchable.catchUICatchableImage;
            }

            if (resultDescriptionLabel != null)
            {
                resultDescriptionLabel.text = catchable.catchFlavor;
            }

            // Try to add to inventory
            AddCatchableToInventory(catchable);
        }
        else
        {
            if (resultDescriptionLabel != null)
            {
                resultDescriptionLabel.text = GetFailureReason(result);
            }
        }
    }

    private void OnReelButtonClicked()
    {
        miniGameController.OnPlayerInput_Reel();
    }

    private void OnBiteReelButtonClicked()
    {
        miniGameController.OnPlayerInput_Reel();
    }

    private void OnContinueButtonClicked()
    {
        CloseFishingUI();
    }

    private void AddCatchableToInventory(CatchableDefinition catchable)
    {
        if (catchable == null)
        {
            Debug.LogWarning("FishingUIController: Catchable is null!");
            return;
        }

        if (catchable.inventoryItem == null)
        {
            Debug.LogWarning($"FishingUIController: {catchable.catchableName} has no inventory item assigned!");
            return;
        }

        if (inventoryController == null)
        {
            inventoryController = FindFirstObjectByType<InventoryController>();
        }

        if (inventoryController == null)
        {
            Debug.LogError("FishingUIController: InventoryController not found in scene!");
            return;
        }

        // Add the item to inventory
        if (inventoryController.TryAdd(catchable.inventoryItem, 1))
        {
            Debug.Log($"[FishingUI] Successfully added {catchable.catchableName} to inventory!");
        }
        else
        {
            Debug.LogWarning($"[FishingUI] Inventory full! Could not add {catchable.catchableName}");
        }
    }

    private void DisablePlayerMovement()
    {
        // Disable player input while fishing
        CharacterController2D playerController = FindObjectOfType<CharacterController2D>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }
    }

    private void EnablePlayerMovement()
    {
        // Re-enable player movement
        CharacterController2D playerController = FindObjectOfType<CharacterController2D>();
        if (playerController != null)
        {
            playerController.enabled = true;
        }
    }

    private string GetResultText(FishingResultType result)
    {
        return result switch
        {
            FishingResultType.Success => "🎣 CAUGHT IT! 🎣",
            FishingResultType.MissedBite => "❌ MISSED THE BITE!",
            FishingResultType.TooMuchTension => "❌ LINE SNAPPED!",
            FishingResultType.LineBroke => "❌ TOO MUCH TENSION!",
            FishingResultType.Escaped => "❌ FISH GOT AWAY!",
            _ => "❌ FINISHED"
        };
    }

    private Color GetResultColor(FishingResultType result)
    {
        return result switch
        {
            FishingResultType.Success => new Color(0.2f, 1f, 0.2f), // Green
            _ => new Color(1f, 0.2f, 0.2f) // Red
        };
    }

    private string GetFailureReason(FishingResultType result)
    {
        return result switch
        {
            FishingResultType.MissedBite => "You didn't react fast enough to the bite!",
            FishingResultType.TooMuchTension => "The line snapped from too much tension.",
            FishingResultType.LineBroke => "The line couldn't handle the pressure.",
            FishingResultType.Escaped => "The fish wriggled free.",
            _ => "The catch was unsuccessful."
        };
    }

    // Animation helper methods

    private IEnumerator ScaleImageBriefly(VisualElement image, float targetScale, float duration)
    {
        if (image == null) yield break;

        float elapsed = 0f;

        // Scale up
        while (elapsed < duration && image != null)
        {
            float t = elapsed / duration;
            float scale = Mathf.Lerp(1f, targetScale, t);
            image.style.scale = new Scale(new Vector3(scale, scale, scale));
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Scale back down
        elapsed = 0f;
        while (elapsed < duration * 0.5f && image != null)
        {
            float t = elapsed / (duration * 0.5f);
            float scale = Mathf.Lerp(targetScale, 1f, t);
            image.style.scale = new Scale(new Vector3(scale, scale, scale));
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (image != null)
            image.style.scale = new Scale(Vector3.one);
    }

    private IEnumerator PulseTensionBar()
    {
        if (tensionBar == null) yield break;

        float duration = 0.2f;
        float elapsed = 0f;

        // Pulse out
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float scale = Mathf.Lerp(1f, 1.15f, t);
            tensionBar.style.scale = new Scale(new Vector3(scale, scale, scale));
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Pulse back
        elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float scale = Mathf.Lerp(1.15f, 1f, t);
            tensionBar.style.scale = new Scale(new Vector3(scale, scale, scale));
            elapsed += Time.deltaTime;
            yield return null;
        }

        tensionBar.style.scale = new Scale(Vector3.one);
    }
}
