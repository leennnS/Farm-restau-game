using UnityEngine;

/// <summary>
/// Opens the pond refill UI only when the player is inside the pond trigger
/// and presses E.
/// Place this component on a pond GameObject with a 2D trigger collider.
/// </summary>
public class PondRefillTrigger : MonoBehaviour
{
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private PondRefillUI pondRefillUI;

    private bool playerInRange = false;
    private bool uiActive = false;

    private void Start()
    {
        if (pondRefillUI == null)
            pondRefillUI = FindFirstObjectByType<PondRefillUI>();

        if (pondRefillUI == null)
            Debug.LogError("[PondRefillTrigger] PondRefillUI not found in scene!");
    }

   private void Update()
{
    if (!playerInRange || pondRefillUI == null)
        return;

    if (!uiActive && Input.GetKeyDown(interactKey))
    {
        Debug.Log("[PondRefillTrigger] E pressed near pond. Showing refill UI.");
        pondRefillUI.SetTrigger(this);
        pondRefillUI.ShowRefillUI();
        uiActive = true;
    }
}

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        Debug.Log("[PondRefillTrigger] Player entered pond range.");
        playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        Debug.Log("[PondRefillTrigger] Player left pond range.");
        playerInRange = false;

        if (uiActive && pondRefillUI != null)
        {
            pondRefillUI.HideRefillUI();
            uiActive = false;
        }
    }

    public void NotifyUIClosed()
    {
        uiActive = false;
    }
}