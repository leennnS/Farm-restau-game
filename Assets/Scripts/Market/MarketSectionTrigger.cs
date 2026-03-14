using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MarketSectionTrigger : MonoBehaviour
{
    [SerializeField] private MarketUIController marketUI;
    [SerializeField] private MarketSectionType sectionType;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private string promptMessage = "Press E to browse";

    private bool playerInside;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Update()
    {
        if (!playerInside || marketUI == null)
            return;

        if (marketUI.IsOpen)
            return;

        if (Input.GetKeyDown(interactKey))
        {
            marketUI.OpenSection(sectionType);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = true;

        if (marketUI != null && !marketUI.IsOpen)
            marketUI.SetInteractionHint(promptMessage, true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;

        if (marketUI != null)
            marketUI.SetInteractionHint(string.Empty, false);
    }
}