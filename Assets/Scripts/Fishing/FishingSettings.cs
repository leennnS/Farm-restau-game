using UnityEngine;

/// <summary>
/// ScriptableObject with global fishing system settings.
/// Create one instance and reference it from LakeFishingTrigger and FishingMiniGameController.
/// </summary>
[CreateAssetMenu(menuName = "Fishing/Fishing Settings", fileName = "FishingSettings")]
public class FishingSettings : ScriptableObject
{
    [Header("UI Settings")]
    [SerializeField] public float promptFadeInDuration = 0.3f;
    [SerializeField] public float promptFadeOutDuration = 0.2f;
    [SerializeField] public float promptShowDistance = 3f; // Distance at which prompt appears

    [Header("Mini-Game Phases")]
    [SerializeField] public float castPhaseDuration = 1f; // How long before auto-cast
    [SerializeField] public float anticipationMinDuration = 1f; // Min wait before possible bite
    [SerializeField] public float anticipationMaxDuration = 4f; // Max wait before guaranteed bite
    [SerializeField] public float biteTensionIncrease = 0.3f; // Tension gain on bite

    [Header("Catch Phase")]
    [SerializeField] public float maxTension = 1f; // Danger threshold
    [SerializeField] public float lineBreakerThreshold = 1.0f; // If tension exceeds this, line breaks
    [SerializeField] public float relaxedTensionZoneMax = 0.7f; // Safe zone upper bound
    [SerializeField] public float warningTensionStart = 0.80f; // When to start warning UI
    [SerializeField] public float tensionDecayRate = 0.15f; // How fast tension relaxes when not reeling
    [SerializeField] public float minTensionThreshold = 0.3f; // Minimum tension to keep - line goes slack below this
    [SerializeField] public float minTensionBuffer = 0.1f; // How long player can stay below min before failure (seconds)

    [Header("Player Control")]
    [SerializeField] public float reelInputDuration = 0.15f; // How long to detect reel input
    [SerializeField] public float reelTensionIncrement = 0.02f; // How much tension increases when reeling - REDUCED
    [SerializeField] public float reelTensionMax = 0.85f; // Max tension when player is reeling hard

    [Header("Audio/Visual Feedback")]
    [SerializeField] public bool enableBiteVibration = true;
    [SerializeField] public float biteVibrationStrength = 0.5f;
    [SerializeField] public bool enableSoundEffects = true;

    [Header("Success Conditions")]
    [SerializeField] public float successTensionMin = 0.0f; // ANYTHING GOES - almost guaranteed win
    [SerializeField] public float successTensionMax = 1.5f; // ANYTHING GOES - almost guaranteed win

    [Header("Hold Mechanic Settings")]
    [SerializeField] public float requiredHoldDuration = 2f; // How long to hold to catch (2 seconds - reduced from 2.5)
    [SerializeField] public float fishMovementSpeed = 50f; // Slower movement (was 200)
    [SerializeField] public float fishMovementRange = 150f; // How far fish moves from center
    [SerializeField] public float catchableZoneRadius = 60f; // How close you need to be to catch
    [SerializeField] public float holdUIPositionX = 0.5f; // 0-1 screen position (0.5 = center)
    [SerializeField] public float holdUIPositionY = 0.5f; // 0-1 screen position (0.5 = center)

    [Header("Debug")]
    [SerializeField] public bool debugMode = false;
    [SerializeField] public bool skipBiteWait = false; // For testing
    [SerializeField] public bool autoSucceed = false; // Always catch
}
