using UnityEngine;

/// <summary>
/// Detects when the player is near a pond and opens the refill mini-game UI.
/// Place this component on a pond GameObject with a trigger collider.
/// </summary>
public class PondRefillTrigger : MonoBehaviour
{
    [SerializeField] private float refillDistance = 2f;
    [SerializeField] private PondRefillUI pondRefillUI;

    private FarmingInputHandler farmingInputHandler;
    private InventoryController inventoryController;
    private Transform playerTransform;
    private bool playerInRange = false;
    private bool uiActive = false;

    private void Start()
    {
        // Find required components
        farmingInputHandler = FindFirstObjectByType<FarmingInputHandler>();
        inventoryController = FindFirstObjectByType<InventoryController>();

        if (pondRefillUI == null)
            pondRefillUI = FindFirstObjectByType<PondRefillUI>();

        // Find player by tag
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
            playerTransform = playerGO.transform;

        if (farmingInputHandler == null)
            Debug.LogError("[PondRefillTrigger] FarmingInputHandler not found in scene!");
        if (inventoryController == null)
            Debug.LogError("[PondRefillTrigger] InventoryController not found in scene!");
        if (playerTransform == null)
            Debug.LogError("[PondRefillTrigger] Player with 'Player' tag not found!");
        if (pondRefillUI == null)
            Debug.LogError("[PondRefillTrigger] PondRefillUI not found in scene!");
    }

    private void Update()
    {
        if (!playerInRange || playerTransform == null) return;

        // Check distance to player
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer > refillDistance)
        {
            playerInRange = false;
            if (uiActive)
            {
                pondRefillUI.HideRefillUI();
                uiActive = false;
            }
            return;
        }

        // Keep UI open while in range
        if (!uiActive && pondRefillUI != null)
        {
            pondRefillUI.ShowRefillUI();
            uiActive = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("[PondRefillTrigger] OnTriggerEnter2D with: " + other.gameObject.name + " (tag: " + other.tag + ")");
        if (other.CompareTag("Player"))
        {
            Debug.Log("[PondRefillTrigger] Player detected in trigger!");
            playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (uiActive && pondRefillUI != null)
            {
                pondRefillUI.HideRefillUI();
                uiActive = false;
            }
        }
    }
}
