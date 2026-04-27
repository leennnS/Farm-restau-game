using UnityEngine;
using System;

/// <summary>
/// Core fishing mini-game controller. Manages the state machine, tension, creature behavior,
/// and determines success/failure. Works with FishingUIController for UI updates.
/// </summary>
public class FishingMiniGameController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private FishingSettings fishingSettings;
    [SerializeField] private LakeZoneDefinition currentZone;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = false;

    // State machine
    private FishingState currentState;
    private float stateTimer = 0f;

    // Fishing data
    private CatchableDefinition currentCatchable;
    private float currentTension = 0f;
    private float catchPhaseTimer = 0f;
    private float catchPhaseDuration = 0f;
    private bool playerReactedToBite = false;
    private bool playerIsReeling = false;
    private float reelInputTimer = 0f;
    private float lowTensionTimer = 0f; // Tracks how long tension has been too low

    // Creature behavior
    private float creatureBehaviorTimer = 0f;
    private float nextBehaviorChangeTime = 0f;
    private float currentCreaturePullForce = 0f;

    // Hold mechanic tracking
    private float fishXPosition = 0f; // -1 to 1 (normalized position)
    private float fishYPosition = 0f; // -1 to 1 (normalized position)
    private float holdTimer = 0f; // Time player has been holding
    private bool isPlayerHolding = false;
    private bool useHoldMechanic = true; // NEW: Toggle between tension and hold modes

    // Events
    public event Action OnBiteOccurred;
    public event Action<float> OnTensionChanged; // Listener receives current tension (0-1)
    public event Action<FishingResultType> OnCatchComplete; // Success or failure

    // NEW EVENTS for hold mechanic
    public event Action<Vector2> OnFishPositionChanged; // Sends fish position (x, y) from -1 to 1
    public event Action<float> OnHoldProgressChanged; // Sends hold progress (0-1)

    private void Awake()
    {
        if (fishingSettings == null)
        {
            Debug.LogError("FishingMiniGameController: FishingSettings not assigned!");
        }
    }

    /// <summary>
    /// Begins a new fishing session. Called by LakeFishingTrigger.
    /// </summary>
    public void StartFishing(LakeZoneDefinition zone = null)
    {
        if (zone != null)
            currentZone = zone;

        if (currentZone == null)
        {
            Debug.LogError("FishingMiniGameController: No zone defined!");
            return;
        }

        // Select what to catch
        currentCatchable = CatchableSelector.SelectCatchable(currentZone);

        if (currentCatchable == null)
        {
            Debug.LogError("FishingMiniGameController: Could not select catchable!");
            return;
        }

        DebugLog($"Starting fishing session. Catchable: {currentCatchable.catchableName}");

        // Reset state
        currentState = useHoldMechanic ? FishingState.Catching : FishingState.Casting;
        stateTimer = 0f;
        currentTension = 0f;
        playerReactedToBite = false;
        playerIsReeling = false;
        catchPhaseTimer = 0f;
        creatureBehaviorTimer = 0f;
        lowTensionTimer = 0f;

        // Reset hold mechanic
        fishXPosition = 0f;
        fishYPosition = 0f;
        holdTimer = 0f;
        isPlayerHolding = false;

        if (useHoldMechanic)
        {
            catchPhaseDuration = fishingSettings.requiredHoldDuration;
        }

        OnTensionChanged?.Invoke(currentTension);
    }

    /// <summary>
    /// Main update loop. Called each frame while mini-game is active.
    /// </summary>
    public void UpdateFishing()
    {
        if (currentCatchable == null)
            return;

        stateTimer += Time.deltaTime;
        creatureBehaviorTimer += Time.deltaTime;

        if (useHoldMechanic)
        {
            UpdateHoldMechanic();
        }
        else
        {
            switch (currentState)
            {
                case FishingState.Casting:
                    UpdateCastingPhase();
                    break;

                case FishingState.Anticipating:
                    UpdateAnticipationPhase();
                    break;

                case FishingState.Biting:
                    UpdateBitingPhase();
                    break;

                case FishingState.Catching:
                    UpdateCatchingPhase();
                    break;

                case FishingState.Complete:
                    // Handled by external controller
                    break;
            }

            // Continuous tension decay (line relaxes over time)
            if (currentState == FishingState.Catching && !playerIsReeling)
            {
                // Harder fish decay slower - player must reel more to manage tension
                float scaledDecayRate = fishingSettings.tensionDecayRate * (1f - currentCatchable.difficultyScore * 0.5f);
                float scaledMinTension = fishingSettings.minTensionThreshold * (0.5f + currentCatchable.difficultyScore * 0.5f);

                currentTension = Mathf.Max(scaledMinTension, currentTension - scaledDecayRate * Time.deltaTime);
                OnTensionChanged?.Invoke(currentTension);
            }
        }
    }

    /// <summary>
    /// NEW: Update logic for hold-based fishing mechanic.
    /// Player must hold button while fish position enters catch zone.
    /// </summary>
    private void UpdateHoldMechanic()
    {
        // Update fish position (move around)
        UpdateFishPosition();

        // Check if player is holding and fish is in catch zone
        float distanceToFish = Mathf.Sqrt(fishXPosition * fishXPosition + fishYPosition * fishYPosition);
        float catchZoneSize = 0.55f; // INCREASED: Much larger catch zone (was 0.3)
        bool fishInCatchZone = distanceToFish < catchZoneSize;

        if (isPlayerHolding && fishInCatchZone)
        {
            // Player is holding and fish is catchable
            holdTimer += Time.deltaTime;
            OnHoldProgressChanged?.Invoke(Mathf.Clamp01(holdTimer / fishingSettings.requiredHoldDuration));

            // Check for success
            if (holdTimer >= fishingSettings.requiredHoldDuration)
            {
                DebugLog("Successfully caught fish with hold mechanic!");
                CompleteCatch(FishingResultType.Success);
            }
        }
        else if (!isPlayerHolding)
        {
            // Player released - reset hold timer (slower decay for more forgiving gameplay)
            holdTimer = Mathf.Max(0, holdTimer - Time.deltaTime * 0.5f);
            OnHoldProgressChanged?.Invoke(Mathf.Clamp01(holdTimer / fishingSettings.requiredHoldDuration));
        }
        else
        {
            // Fish escaped catch zone - lose progress slowly
            holdTimer = Mathf.Max(0, holdTimer - Time.deltaTime * 1.5f);
            OnHoldProgressChanged?.Invoke(Mathf.Clamp01(holdTimer / fishingSettings.requiredHoldDuration));
        }
    }

    /// <summary>
    /// NEW: Update fish position to move around the screen.
    /// </summary>
    private void UpdateFishPosition()
    {
        // Make fish move in a slow, smooth wave pattern
        float movementSpeed = fishingSettings.fishMovementSpeed * 0.003f; // SLOWER movement
        float movementRange = 0.7f; // Max distance from center (increased from ~0.75)

        fishXPosition = Mathf.Sin(creatureBehaviorTimer * movementSpeed) * movementRange;
        fishYPosition = Mathf.Cos(creatureBehaviorTimer * movementSpeed * 0.8f) * (movementRange * 0.6f);

        // Add gentle random drift (but not too much)
        if (creatureBehaviorTimer >= nextBehaviorChangeTime)
        {
            fishXPosition += UnityEngine.Random.Range(-0.05f, 0.05f) * (1f + currentCatchable.difficultyScore);
            fishYPosition += UnityEngine.Random.Range(-0.05f, 0.05f) * (1f + currentCatchable.difficultyScore);
            nextBehaviorChangeTime = creatureBehaviorTimer + UnityEngine.Random.Range(1f, 3f); // SLOWER changes
        }

        // Clamp to stay in bounds
        fishXPosition = Mathf.Clamp(fishXPosition, -0.8f, 0.8f);
        fishYPosition = Mathf.Clamp(fishYPosition, -0.6f, 0.6f);

        OnFishPositionChanged?.Invoke(new Vector2(fishXPosition, fishYPosition));
    }

    private void UpdateCastingPhase()
    {
        // Auto-cast after brief delay
        if (stateTimer >= fishingSettings.castPhaseDuration)
        {
            TransitionToAnticipation();
        }
    }

    private void UpdateAnticipationPhase()
    {
        // Wait for bite to occur
        float minDelay = currentCatchable.biteDelayMin * (1f - currentCatchable.difficultyScore); // Less difficult = faster bite
        float maxDelay = currentCatchable.biteDelayMax * (1f + currentCatchable.difficultyScore * 0.5f); // More difficult = slower/more variable

        // Consider fake out
        if (currentCatchable.canFakeOut && UnityEngine.Random.value < currentCatchable.fakeOutChance)
        {
            if (stateTimer >= minDelay * 0.7f) // Fake out earlier
            {
                OnBiteOccurred?.Invoke();
                DebugLog("Fake out! Not a real bite.");
                stateTimer = 0f;
                return; // Stay in anticipation, next check might be real
            }
        }

        // Real bite
        if (fishingSettings.skipBiteWait || stateTimer >= UnityEngine.Random.Range(minDelay, maxDelay))
        {
            TransitionToBite();
        }
    }

    private void UpdateBitingPhase()
    {
        // Player has ~successWindow seconds to react with button press
        float reactionTime = currentCatchable.successWindow;

        if (stateTimer >= reactionTime)
        {
            // Player didn't react in time
            if (!playerReactedToBite)
            {
                DebugLog("Player missed bite reaction window. Failure.");
                CompleteCatch(FishingResultType.MissedBite);
                return;
            }

            // Player reacted, move to catch phase
            TransitionToCatch();
        }
    }

    private void UpdateCatchingPhase()
    {
        // Manage tension from creature behavior
        UpdateCreatureBehavior();

        // Check for line break (too much tension)
        if (currentTension >= fishingSettings.lineBreakerThreshold)
        {
            DebugLog("Line broke! Too much tension.");
            CompleteCatch(FishingResultType.LineBroke);
            return;
        }

        // Check if tension has been too low for too long (line goes slack)
        float scaledMinTension = fishingSettings.minTensionThreshold * (0.5f + currentCatchable.difficultyScore * 0.5f);
        if (currentTension < scaledMinTension)
        {
            lowTensionTimer += Time.deltaTime;
            if (lowTensionTimer >= fishingSettings.minTensionBuffer)
            {
                DebugLog("Line went slack! Tension too low for too long. Failure.");
                CompleteCatch(FishingResultType.LineBroke);
                return;
            }
        }
        else
        {
            lowTensionTimer = 0f; // Reset timer if tension is back in range
        }

        // SUCCESS: Keep filling catch bar ONLY if tension stays in acceptable range
        if (currentTension >= scaledMinTension && currentTension < fishingSettings.lineBreakerThreshold)
        {
            catchPhaseTimer += Time.deltaTime;
        }
        // If tension is out of range, pause progress (don't tick the timer)

        if (catchPhaseTimer >= catchPhaseDuration)
        {
            DebugLog("Successfully caught!");
            CompleteCatch(FishingResultType.Success);
        }
    }

    private void UpdateCreatureBehavior()
    {
        // Creature applies resistance based on behavior type and difficulty
        // Scale by difficulty score so harder fish create more tension
        float baseResistance = currentCatchable.creaturePullStrength * currentZone.difficultyModifier * (1f + currentCatchable.difficultyScore);

        switch (currentCatchable.behaviorType)
        {
            case FishBehaviorType.Standard:
                currentCreaturePullForce = baseResistance * 2f;
                break;

            case FishBehaviorType.FastDarter:
                // Quick, erratic resistance
                if (creatureBehaviorTimer >= nextBehaviorChangeTime)
                {
                    currentCreaturePullForce = UnityEngine.Random.Range(baseResistance * 1.5f, baseResistance * 3f);
                    nextBehaviorChangeTime = creatureBehaviorTimer + UnityEngine.Random.Range(0.3f, 0.8f);
                }
                break;

            case FishBehaviorType.SlowHeavy:
                // Consistent strong resistance
                currentCreaturePullForce = baseResistance * 3f;
                break;

            case FishBehaviorType.Elusive:
                // Quickly loses tension, tries to escape
                currentCreaturePullForce = baseResistance * 1f;
                break;

            case FishBehaviorType.Aggressive:
                // Sudden spikes in resistance
                if (creatureBehaviorTimer >= nextBehaviorChangeTime)
                {
                    if (UnityEngine.Random.value < 0.4f) // 40% chance of aggressive dive
                    {
                        currentCreaturePullForce = baseResistance * 4f;
                    }
                    else
                    {
                        currentCreaturePullForce = baseResistance * 1.5f;
                    }
                    nextBehaviorChangeTime = creatureBehaviorTimer + UnityEngine.Random.Range(0.5f, 1.5f);
                }
                break;

            case FishBehaviorType.Cunning:
                // Tricky - switches between pulling and loose
                if (creatureBehaviorTimer >= nextBehaviorChangeTime)
                {
                    currentCreaturePullForce = UnityEngine.Random.value < 0.5f ? baseResistance * 2.5f : 0f;
                    nextBehaviorChangeTime = creatureBehaviorTimer + UnityEngine.Random.Range(0.4f, 1f);
                }
                break;
        }

        // Apply creature resistance to tension
        currentTension += currentCreaturePullForce * Time.deltaTime;

        // Clamp tension
        currentTension = Mathf.Clamp01(currentTension);
        OnTensionChanged?.Invoke(currentTension);
    }

    /// <summary>
    /// Called when player presses the reaction button (during bite or catch phase).
    /// </summary>
    public void OnPlayerInput_Reel()
    {
        if (currentState == FishingState.Biting && !playerReactedToBite)
        {
            // Successfully reacted to bite
            playerReactedToBite = true;
            DebugLog("Player reacted to bite successfully!");
            return;
        }

        if (currentState == FishingState.Catching)
        {
            // Start reeling - pull up on line, which REDUCES tension
            playerIsReeling = true;
            reelInputTimer = fishingSettings.reelInputDuration;

            // Reduce tension when reeling (pulling the line reduces tension)
            currentTension -= fishingSettings.reelTensionIncrement * 2f; // Double reduction for better control
            currentTension = Mathf.Max(fishingSettings.minTensionThreshold, currentTension);
            OnTensionChanged?.Invoke(currentTension);
        }
    }

    /// <summary>
    /// Called when player releases reel input (stops pulling).
    /// </summary>
    public void OnPlayerInput_Release()
    {
        if (currentState == FishingState.Catching)
        {
            playerIsReeling = false;
            DebugLog("Player released reel input.");
        }

        // NEW: For hold mechanic
        if (useHoldMechanic)
        {
            isPlayerHolding = false;
        }
    }

    /// <summary>
    /// NEW: Called when player holds down button (hold mechanic).
    /// </summary>
    public void OnPlayerInput_Hold()
    {
        if (useHoldMechanic && currentState == FishingState.Catching)
        {
            isPlayerHolding = true;
        }
    }

    /// <summary>
    /// NEW: Called when player releases hold button (hold mechanic).
    /// </summary>
    public void OnPlayerInput_ReleaseHold()
    {
        if (useHoldMechanic)
        {
            isPlayerHolding = false;
        }
    }

    private void TransitionToAnticipation()
    {
        currentState = FishingState.Anticipating;
        stateTimer = 0f;
        DebugLog("Transitioned to Anticipation phase. Waiting for bite...");
    }

    private void TransitionToBite()
    {
        currentState = FishingState.Biting;
        stateTimer = 0f;
        playerReactedToBite = false;
        OnBiteOccurred?.Invoke();

        // Apply tension bump from bite - scales with fish difficulty
        float biteTensionIncrease = fishingSettings.biteTensionIncrease * (1f + currentCatchable.difficultyScore);
        currentTension += biteTensionIncrease;
        OnTensionChanged?.Invoke(currentTension);

        if (fishingSettings.enableBiteVibration)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            // Could add haptic feedback here for supported platforms
#endif
        }

        DebugLog("BITE! Player must react now!");
    }

    private void TransitionToCatch()
    {
        currentState = FishingState.Catching;
        stateTimer = 0f;
        catchPhaseTimer = 0f;
        catchPhaseDuration = UnityEngine.Random.Range(currentCatchable.catchDurationMin, currentCatchable.catchDurationMax);
        nextBehaviorChangeTime = creatureBehaviorTimer + UnityEngine.Random.Range(0.5f, 1.5f);

        DebugLog($"Transitioned to Catch phase. Duration: {catchPhaseDuration:F2}s. Manage tension!");
    }

    public void CompleteCatch(FishingResultType result)
    {
        currentState = FishingState.Complete;
        DebugLog($"Catch completed. Result: {result}");
        OnCatchComplete?.Invoke(result);
    }

    // Public query methods
    public FishingState GetCurrentState() => currentState;
    public CatchableDefinition GetCurrentCatchable() => currentCatchable;
    public float GetCurrentTension() => currentTension;
    public float GetCatchPhaseProgress() => Mathf.Clamp01(catchPhaseTimer / catchPhaseDuration);

    private void DebugLog(string message)
    {
        if (debugLogging || fishingSettings.debugMode)
        {
            Debug.Log($"[FishingMiniGame] {message}");
        }
    }
}

public enum FishingState
{
    Casting,      // Initial cast
    Anticipating, // Waiting for bite
    Biting,       // Fish bites, player must react
    Catching,     // Player managing tension to catch
    Complete      // Finished (success or failure)
}

public enum FishingResultType
{
    Success,
    MissedBite,
    TooMuchTension,
    LineBroke,
    Escaped
}
