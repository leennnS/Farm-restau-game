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

    // Creature behavior
    private float creatureBehaviorTimer = 0f;
    private float nextBehaviorChangeTime = 0f;
    private float currentCreaturePullForce = 0f;

    // Events
    public event Action OnBiteOccurred;
    public event Action<float> OnTensionChanged; // Listener receives current tension (0-1)
    public event Action<FishingResultType> OnCatchComplete; // Success or failure

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
        currentState = FishingState.Casting;
        stateTimer = 0f;
        currentTension = 0f;
        playerReactedToBite = false;
        playerIsReeling = false;
        catchPhaseTimer = 0f;
        creatureBehaviorTimer = 0f;

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
            currentTension = Mathf.Max(0f, currentTension - fishingSettings.tensionDecayRate * Time.deltaTime);
            OnTensionChanged?.Invoke(currentTension);
        }
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

        // Check for line break
        if (currentTension >= fishingSettings.lineBreakerThreshold)
        {
            DebugLog("Line broke! Too much tension.");
            CompleteCatch(FishingResultType.LineBroke);
            return;
        }

        // Check for success - just fill the progress bar!
        catchPhaseTimer += Time.deltaTime;

        if (catchPhaseTimer >= catchPhaseDuration)
        {
            // Just succeed if you filled the bar - tension almost doesn't matter anymore
            DebugLog("Successfully caught!");
            CompleteCatch(FishingResultType.Success);
        }
    }

    private void UpdateCreatureBehavior()
    {
        // Creature applies resistance based on behavior type and difficulty
        float baseResistance = currentCatchable.creaturePullStrength * currentZone.difficultyModifier;

        switch (currentCatchable.behaviorType)
        {
            case FishBehaviorType.Standard:
                currentCreaturePullForce = baseResistance * 0.5f;
                break;

            case FishBehaviorType.FastDarter:
                // Quick, erratic resistance
                if (creatureBehaviorTimer >= nextBehaviorChangeTime)
                {
                    currentCreaturePullForce = UnityEngine.Random.Range(baseResistance * 0.3f, baseResistance);
                    nextBehaviorChangeTime = creatureBehaviorTimer + UnityEngine.Random.Range(0.3f, 0.8f);
                }
                break;

            case FishBehaviorType.SlowHeavy:
                // Consistent strong resistance
                currentCreaturePullForce = baseResistance * 1.2f;
                break;

            case FishBehaviorType.Elusive:
                // Quickly loses tension, tries to escape
                currentCreaturePullForce = baseResistance * 0.2f;
                break;

            case FishBehaviorType.Aggressive:
                // Sudden spikes in resistance
                if (creatureBehaviorTimer >= nextBehaviorChangeTime)
                {
                    if (UnityEngine.Random.value < 0.4f) // 40% chance of aggressive dive
                    {
                        currentCreaturePullForce = baseResistance * 1.5f;
                    }
                    else
                    {
                        currentCreaturePullForce = baseResistance * 0.5f;
                    }
                    nextBehaviorChangeTime = creatureBehaviorTimer + UnityEngine.Random.Range(0.5f, 1.5f);
                }
                break;

            case FishBehaviorType.Cunning:
                // Tricky - switches between pulling and loose
                if (creatureBehaviorTimer >= nextBehaviorChangeTime)
                {
                    currentCreaturePullForce = UnityEngine.Random.value < 0.5f ? baseResistance * 0.8f : 0f;
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
            currentTension = Mathf.Max(0f, currentTension);
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

        // Apply tension bump from bite
        currentTension += fishingSettings.biteTensionIncrease;
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
