using UnityEngine;

public enum FarmTutorialZoneType
{
    House,
    Market,
    Restaurant
}

/// <summary>
/// Trigger marker for farm tutorial location steps.
/// Attach to zone colliders in FarmScene.
/// </summary>
[DisallowMultipleComponent]
public class FarmTutorialZoneTrigger : MonoBehaviour
{
    [SerializeField] private FarmTutorialZoneType zoneType;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private FarmTutorialManager tutorialManager;

    private void Awake()
    {
        if (tutorialManager == null)
            tutorialManager = FindFirstObjectByType<FarmTutorialManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        if (tutorialManager == null)
            tutorialManager = FindFirstObjectByType<FarmTutorialManager>();

        if (tutorialManager != null)
            tutorialManager.MarkZoneVisited(zoneType);
    }
}
