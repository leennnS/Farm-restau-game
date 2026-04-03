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

    [Header("Debug")]
    [SerializeField] public bool debugMode = false;
    [SerializeField] public bool skipBiteWait = false; // For testing
    [SerializeField] public bool autoSucceed = false; // Always catch
}
