using UnityEngine;

/// <summary>
/// Trigger zone for the gardening/tree seeds section in the market.
/// When the player enters and presses E, opens the tree seeds shop UI.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class GardeningMarketTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MarketUIController marketUI;
    [SerializeField] private PickupToastUIToolkit toastUI;

    [Header("Interaction")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private string interactionPrompt = "Press E to browse tree seeds";

    private bool playerInZone = false;

    private void Start()
    {
        // Auto-find components if not assigned
        if (marketUI == null)
            marketUI = FindFirstObjectByType<MarketUIController>();

        if (toastUI == null)
            toastUI = FindFirstObjectByType<PickupToastUIToolkit>();

        // Ensure collider is a trigger
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
            collider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        playerInZone = true;

        // Show interaction hint
        if (marketUI != null)
            marketUI.SetInteractionHint(interactionPrompt, true);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        playerInZone = false;

        // Hide interaction hint
        if (marketUI != null)
            marketUI.SetInteractionHint(string.Empty, false);
    }

    private void Update()
    {
        if (!playerInZone)
            return;

        if (Input.GetKeyDown(interactionKey))
        {
            if (marketUI != null)
            {
                // Open the tree seeds section and lock to it
                marketUI.OpenSection(MarketSectionType.TreeSeeds, true);
            }
        }
    }
}
