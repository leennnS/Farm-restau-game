using UnityEngine;

/// <summary>
/// Handles boat interaction: proximity detection, E key prompts, and boarding/disembarking.
/// When the player presses E near the boat, they are parented to it and the boat begins moving.
/// Press E again to disembark.
/// </summary>
public class BoatInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private PickupToastUIToolkit toastUI;
    [SerializeField] private string boardPrompt = "Press E to board the boat";
    [SerializeField] private string disembarkPrompt = "Press E to disembark";
    [SerializeField] private Vector3 playerBoatOffset = Vector3.zero; // Adjust where player stands on boat

    [Header("References")]
    [SerializeField] private Transform playerTransform;
    private CharacterController2D playerController;
    private Rigidbody2D playerRigidbody;
    private BoatMovement boatMovement;

    private bool playerIsOnBoat = false;
    private Vector3 playerOriginalLocalScale;
    private bool inRangeLastFrame = false;

    private void OnDrawGizmosSelected()
    {
        // Draw a visual indicator of where the player will be positioned on the boat
        Gizmos.color = Color.green;
        Vector3 boardingPos = transform.position + playerBoatOffset;
        Gizmos.DrawSphere(boardingPos, 0.3f);
    }

    private void Start()
    {
        boatMovement = GetComponent<BoatMovement>();
        if (boatMovement == null)
        {
            Debug.LogError("BoatInteraction requires a BoatMovement component on the same GameObject!");
        }

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                playerController = player.GetComponent<CharacterController2D>();
                playerRigidbody = player.GetComponent<Rigidbody2D>();
            }
        }
    }

    private void Update()
    {
        if (playerTransform == null)
            TryResolvePlayer();

        if (playerTransform == null || boatMovement == null)
        {
            Debug.Log($"[Boat] Player not found or BoatMovement missing. Player: {playerTransform != null}, Boat: {boatMovement != null}");
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        bool inRange = distanceToPlayer <= interactionDistance && !playerIsOnBoat;
        bool inRangeOnBoat = playerIsOnBoat; // Always in range for disembarking

        Debug.Log($"[Boat] Distance: {distanceToPlayer:F2}, InRange: {inRange}, OnBoat: {playerIsOnBoat}");

        // Show toast UI
        if ((inRange || inRangeOnBoat) && !inRangeLastFrame)
        {
            string prompt = playerIsOnBoat ? disembarkPrompt : boardPrompt;
            if (toastUI != null)
            {
                toastUI.Show(prompt);
                Debug.Log($"[Boat] Toast showing: {prompt}");
            }
            else
            {
                Debug.LogWarning("[Boat] Toast UI is null!");
            }
            inRangeLastFrame = true;
        }
        else if (!inRange && !inRangeOnBoat && inRangeLastFrame)
        {
            inRangeLastFrame = false;
        }

        // Handle WASD input while on boat
        if (playerIsOnBoat)
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector2 moveInput = new Vector2(horizontal, vertical).normalized;

            if (moveInput != Vector2.zero)
            {
                // Player is pressing movement keys - move the boat
                boatMovement.SetDirection(moveInput);
                if (!boatMovement.IsMoving)
                {
                    boatMovement.StartMoving();
                    Debug.Log($"[Boat] Started moving in direction: {moveInput}");
                }
            }
            else
            {
                // No movement keys pressed - stop the boat
                if (boatMovement.IsMoving)
                {
                    boatMovement.StopMoving();
                    Debug.Log("[Boat] No keys pressed, boat stopped");
                }
            }
        }

        // Handle E key press
        if ((inRange || inRangeOnBoat) && Input.GetKeyDown(interactionKey))
        {
            Debug.Log($"[Boat] E pressed! OnBoat: {playerIsOnBoat}");
            if (!playerIsOnBoat)
            {
                BoardPlayer();
            }
            else if (playerIsOnBoat)
            {
                // Disembark
                DisembarkPlayer();
            }
        }
    }

    private void TryResolvePlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerController = player.GetComponent<CharacterController2D>();
            playerRigidbody = player.GetComponent<Rigidbody2D>();
        }
    }

    private void BoardPlayer()
    {
        if (playerTransform == null)
            return;

        playerIsOnBoat = true;

        // Store original scale before parenting
        playerOriginalLocalScale = playerTransform.localScale;

        // Parent player to boat (move to boat's center)
        playerTransform.SetParent(transform);
        playerTransform.localPosition = playerBoatOffset; // Use configurable offset
        playerTransform.localRotation = Quaternion.identity;

        // Disable player movement controls
        if (playerController != null)
            playerController.enabled = false;

        // Disable player's rigidbody physics (boat will handle movement)
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.isKinematic = true; // Completely disable physics for player
            Debug.Log("[Boat] Player rigidbody set to kinematic");
        }

        Debug.Log("[Boat] Player boarded the boat");
        if (toastUI != null)
            toastUI.Show("Press WASD to move, E to disembark");
    }

    private void DisembarkPlayer()
    {
        if (playerTransform == null)
            return;

        playerIsOnBoat = false;

        // Unparent player from boat
        playerTransform.SetParent(null);
        playerTransform.localScale = playerOriginalLocalScale;

        // Re-enable player movement controls
        if (playerController != null)
            playerController.enabled = true;

        // Re-enable player's rigidbody physics
        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = false; // Re-enable physics
            Debug.Log("[Boat] Player rigidbody re-enabled");
        }

        // Stop boat movement
        if (boatMovement != null)
            boatMovement.StopMoving();

        Debug.Log("[Boat] Player disembarked");
        if (toastUI != null)
            toastUI.Show(boardPrompt);
    }

    public bool IsPlayerOnBoat => playerIsOnBoat;
}
