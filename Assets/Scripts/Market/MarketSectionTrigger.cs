using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MarketSectionTrigger : MonoBehaviour
{
    [SerializeField] private MarketUIController marketUI;
    [SerializeField] private MarketSectionType sectionType;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private string promptMessage = "Press E to browse";

    private static MarketSectionTrigger activeTrigger;
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

        if (activeTrigger != this)
            return;

        if (marketUI.IsOpen)
            return;

        if (Input.GetKeyDown(interactKey))
        {
            marketUI.OpenSection(sectionType, true);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = true;
        activeTrigger = this;

        if (marketUI != null && !marketUI.IsOpen)
            marketUI.SetInteractionHint(promptMessage, true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = false;

        if (activeTrigger == this)
            activeTrigger = null;

        if (marketUI != null)
            marketUI.SetInteractionHint(string.Empty, false);
    }

    private void OnDisable()
    {
        if (activeTrigger == this)
            activeTrigger = null;
    }
}