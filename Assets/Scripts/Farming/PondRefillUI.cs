using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

/// <summary>
/// Mini-game UI for refilling watering can at pond.
/// Player scrolls mouse wheel to pull water up and fill the bar.
/// </summary>
public class PondRefillUI : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private PickupToastUIToolkit pickupToast;
    [SerializeField] private FarmingInputHandler farmingInputHandler;

    [Header("Fill Settings")]
    [SerializeField] private float fillSpeed = 0.15f;  // How much scroll adds per unit
    [SerializeField] private float fillDuration = 2f;  // Seconds to auto-fill if not scrolled
    [SerializeField] private float autoFillPerSecond = 0.3f;  // Fill rate per second if idling

    private VisualElement root;
    private ProgressBar fillBar;
    private Label instructionLabel;
    private Label percentLabel;
    private VisualElement waterContainer;
    private float currentFill = 0f;
    private bool isActive = false;
    private float idleTimer = 0f;

    private void OnEnable()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument != null)
            root = uiDocument.rootVisualElement;

        if (farmingInputHandler == null)
            farmingInputHandler = FindFirstObjectByType<FarmingInputHandler>();

        if (pickupToast == null)
            pickupToast = FindFirstObjectByType<PickupToastUIToolkit>();
    }

    private void Update()
    {
        if (!isActive || root == null) return;

        // Get mouse scroll input
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (scrollInput != 0f)
        {
            // Player is scrolling - add to fill
            currentFill += scrollInput * fillSpeed;
            // Clamp to 0-1
            currentFill = Mathf.Clamp01(currentFill);
            idleTimer = 0f;
            UpdateFillBar();
        }
        else
        {
            // No scroll input - auto-fill slowly
            idleTimer += Time.deltaTime;
            if (idleTimer > 0.2f)  // Slight delay before auto-fill kicks in
            {
                currentFill += autoFillPerSecond * Time.deltaTime;
                currentFill = Mathf.Clamp01(currentFill);
                UpdateFillBar();
            }
        }

        // Check if full
        if (currentFill >= 1f)
        {
            CompleteRefill();
        }
    }

    public void ShowRefillUI()
    {
        Debug.Log("[PondRefillUI] ShowRefillUI called. Root is: " + (root != null ? "NOT NULL" : "NULL"));

        if (root == null) return;

        // Enable UI
        root.style.display = DisplayStyle.Flex;
        Debug.Log("[PondRefillUI] UI display set to Flex");
        isActive = true;
        currentFill = 0f;
        idleTimer = 0f;

        // Find UI elements
        fillBar = root.Q<ProgressBar>("fillBar");
        instructionLabel = root.Q<Label>("instructionLabel");
        percentLabel = root.Q<Label>("percentLabel");
        waterContainer = root.Q<VisualElement>("waterContainer");

        if (fillBar != null)
            fillBar.value = 0f;

        if (instructionLabel != null)
            instructionLabel.text = "Scroll up to pull water";

        if (percentLabel != null)
            percentLabel.text = "0%";

        // Play show animation
        StartCoroutine(ShowAnimation());
    }

    public void HideRefillUI()
    {
        if (root == null) return;

        isActive = false;
        StartCoroutine(HideAnimation());
    }

    private void UpdateFillBar()
    {
        if (fillBar == null) return;

        fillBar.value = currentFill;

        // Update percentage
        int percentage = (int)(currentFill * 100f);
        if (percentLabel != null)
            percentLabel.text = $"{percentage}%";

        if (instructionLabel != null)
        {
            if (currentFill < 0.25f)
                instructionLabel.text = "Scroll up to pull water";
            else if (currentFill < 0.5f)
                instructionLabel.text = "Keep scrolling...";
            else if (currentFill < 0.75f)
                instructionLabel.text = "Almost there!";
            else
                instructionLabel.text = "Nearly full!";
        }
    }

    private void CompleteRefill()
    {
        if (farmingInputHandler == null) return;

        // Perform refill
        bool refilled = farmingInputHandler.TryRefillWateringCan();

        if (refilled)
        {
            if (pickupToast != null)
                pickupToast.Show("Watering can refilled! ✓");
            Debug.Log("[PondRefillUI] Watering can refilled successfully.");
        }

        // Close UI
        HideRefillUI();
    }

    private IEnumerator ShowAnimation()
    {
        if (root == null) yield break;

        root.style.opacity = 0f;
        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            root.style.opacity = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        root.style.opacity = 1f;
    }

    private IEnumerator HideAnimation()
    {
        if (root == null) yield break;

        root.style.opacity = 1f;
        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            root.style.opacity = Mathf.Clamp01(1f - (elapsed / duration));
            yield return null;
        }

        root.style.opacity = 0f;
        root.style.display = DisplayStyle.None;
        isActive = false;
    }
}
