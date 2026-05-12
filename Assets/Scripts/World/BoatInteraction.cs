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

    [Header("Audio")]
    [SerializeField] private AudioClip boardSound;
    [SerializeField] private AudioClip disembarkSound;
    [SerializeField, Range(0f, 1f)] private float interactionVolume = 0.8f;

    [Header("References")]
    [SerializeField] private Transform playerTransform;
    private CharacterController2D playerController;
    private Rigidbody2D playerRigidbody;
    private BoatMovement boatMovement;
    private AudioSource interactionAudioSource;

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
        SetupInteractionAudioSource();

        if (boatMovement == null)
        {
            Debug.LogError("BoatInteraction requires a BoatMovement component on the same GameObject!");
        }

        if (playerTransform == null)
            TryResolvePlayer();
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

            UpdateBoardedPlayerPosition();
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
        GameObject player = PlayerSetupPipeline.FindPlayerInLoadedScenes();
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
            return;

        playerTransform = player.transform;
        playerController = player.GetComponent<CharacterController2D>();
        playerRigidbody = player.GetComponent<Rigidbody2D>();
    }

    private void BoardPlayer()
    {
        if (playerTransform == null)
            return;

        playerIsOnBoat = true;

        // Store original scale before parenting
        playerOriginalLocalScale = playerTransform.localScale;

        // Keep player in DontDestroyOnLoad by not parenting to the boat
        PlayerSetupPipeline.PreparePlayerForSceneChange();
        playerTransform.position = transform.position + playerBoatOffset;

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
        PlayInteractionSound(boardSound);
        if (toastUI != null)
            toastUI.Show("Press WASD to move, E to disembark");
    }

    private void DisembarkPlayer()
    {
        if (playerTransform == null)
            return;

        playerIsOnBoat = false;

        playerTransform.localScale = playerOriginalLocalScale;

        PlayerSetupPipeline.PreparePlayerForSceneChange();

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
        PlayInteractionSound(disembarkSound);
        if (toastUI != null)
            toastUI.Show(boardPrompt);
    }

    private void UpdateBoardedPlayerPosition()
    {
        if (playerTransform == null)
            return;

        playerTransform.position = transform.position + playerBoatOffset;
    }

    private void SetupInteractionAudioSource()
    {
        interactionAudioSource = gameObject.AddComponent<AudioSource>();
        interactionAudioSource.playOnAwake = false;
        interactionAudioSource.loop = false;
        interactionAudioSource.spatialBlend = 0f;
        interactionAudioSource.volume = interactionVolume;

        if (AudioSettingsManager.HasInstance)
            AudioSettingsManager.Instance.RefreshAudioSource(interactionAudioSource);
    }

    private void PlayInteractionSound(AudioClip clip)
    {
        if (interactionAudioSource == null || clip == null)
            return;

        interactionAudioSource.PlayOneShot(clip);
    }

    public bool IsPlayerOnBoat => playerIsOnBoat;
}
