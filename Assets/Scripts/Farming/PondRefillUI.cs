using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

/// <summary>
/// Refill mini-game for watering can.
/// Scroll mouse wheel upward to raise the water level.
/// If the player stops, the water slowly drops.
/// </summary>
public class PondRefillUI : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private PickupToastUIToolkit pickupToast;
    [SerializeField] private FarmingInputHandler farmingInputHandler;

    [Header("Optional")]
    [SerializeField] private PondRefillTrigger currentTrigger;

    [Header("Fill Settings")]
    [SerializeField] private float fillSpeed = 50f;
    [SerializeField] private float drainPerSecond = 0.00001f;
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;

    private VisualElement documentRoot;
    private VisualElement refillRoot;
    private VisualElement fillArea;
    private VisualElement waveHighlight;
    private Label percentLabel;
    private Label instructionLabel;
    private Label flavorLabel;

    private float currentFill = 0f;
    private bool isActive = false;
    private float waveTimer = 0f;

    private void OnEnable()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument != null)
        {
            documentRoot = uiDocument.rootVisualElement;
            refillRoot = documentRoot.Q<VisualElement>("refillRoot");
            fillArea = documentRoot.Q<VisualElement>("fillArea");
            waveHighlight = documentRoot.Q<VisualElement>("waveHighlight");
            percentLabel = documentRoot.Q<Label>("percentLabel");
            instructionLabel = documentRoot.Q<Label>("instructionLabel");
            flavorLabel = documentRoot.Q<Label>("flavorLabel");
        }

        if (farmingInputHandler == null)
            farmingInputHandler = FindFirstObjectByType<FarmingInputHandler>();

        if (pickupToast == null)
            pickupToast = FindFirstObjectByType<PickupToastUIToolkit>();

        if (refillRoot != null)
        {
            refillRoot.style.display = DisplayStyle.None;
            refillRoot.style.opacity = 0f;
        }

        if (fillArea != null)
            fillArea.style.height = new Length(0, LengthUnit.Percent);
    }

    private void Update()
    {
        if (!isActive || refillRoot == null)
            return;

        if (Input.GetKeyDown(closeKey))
        {
            HideRefillUI();
            return;
        }

        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        Debug.Log($"[PondRefillUI] Scroll input: {scrollInput}, fillSpeed: {fillSpeed}, drain: {drainPerSecond}");

        if (scrollInput > 0f)
        {
            currentFill += scrollInput * fillSpeed;
            Debug.Log($"[PondRefillUI] Adding to fill! New fill: {currentFill}");
        }
        else
        {
            currentFill -= drainPerSecond * Time.deltaTime;
        }

        currentFill = Mathf.Clamp01(currentFill);

        UpdateVisuals(scrollInput > 0f);
        AnimateWave();

        if (currentFill >= 1f)
            CompleteRefill();
    }

    public void SetTrigger(PondRefillTrigger trigger)
    {
        currentTrigger = trigger;
    }

    public void ShowRefillUI()
    {
        if (refillRoot == null)
            return;

        StopAllCoroutines();

        refillRoot.style.display = DisplayStyle.Flex;
        refillRoot.style.opacity = 0f;

        isActive = true;
        currentFill = 0f;
        waveTimer = 0f;

        UpdateVisuals(false);
        StartCoroutine(ShowAnimation());
    }

    public void HideRefillUI()
    {
        if (refillRoot == null)
            return;

        StopAllCoroutines();
        isActive = false;
        StartCoroutine(HideAnimation());
    }

    private void UpdateVisuals(bool scrolledThisFrame)
    {
        int percent = Mathf.RoundToInt(currentFill * 100f);

        if (fillArea != null)
            fillArea.style.height = new Length(percent, LengthUnit.Percent);

        if (percentLabel != null)
            percentLabel.text = percent + "%";

        if (instructionLabel != null)
        {
            if (currentFill < 0.2f)
                instructionLabel.text = "Scroll up to raise the water.";
            else if (currentFill < 0.5f)
                instructionLabel.text = "Good. Keep turning upward.";
            else if (currentFill < 0.85f)
                instructionLabel.text = "Nice. The can is filling well.";
            else
                instructionLabel.text = "Almost full. Just a bit more.";
        }

        if (flavorLabel != null)
        {
            if (scrolledThisFrame)
            {
                if (currentFill < 0.3f)
                    flavorLabel.text = "Small ripples gather into the can.";
                else if (currentFill < 0.7f)
                    flavorLabel.text = "The water level rises steadily.";
                else
                    flavorLabel.text = "The can feels nearly full now.";
            }
            else
            {
                flavorLabel.text = "If you stop, the water slowly drops.";
            }
        }
    }

    private void AnimateWave()
    {
        if (waveHighlight == null)
            return;

        waveTimer += Time.deltaTime * 2.3f;
        float xOffset = Mathf.Sin(waveTimer) * 2f;
        waveHighlight.style.translate = new Translate(xOffset, 0, 0);
    }

    private void CompleteRefill()
    {
        if (farmingInputHandler == null)
            farmingInputHandler = FindFirstObjectByType<FarmingInputHandler>();

        if (farmingInputHandler == null)
        {
            if (pickupToast != null)
                pickupToast.Show("Watering can refill failed.");

            HideRefillUI();
            return;
        }

        bool refilled = farmingInputHandler.TryRefillWateringCan();

        if (refilled && pickupToast != null)
            pickupToast.Show("Watering can refilled! ✓");

        HideRefillUI();
    }

    private IEnumerator ShowAnimation()
    {
        if (refillRoot == null)
            yield break;

        float elapsed = 0f;
        float duration = 0.2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            refillRoot.style.opacity = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        refillRoot.style.opacity = 1f;
    }

    private IEnumerator HideAnimation()
    {
        if (refillRoot == null)
            yield break;

        float elapsed = 0f;
        float duration = 0.16f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            refillRoot.style.opacity = Mathf.Clamp01(1f - (elapsed / duration));
            yield return null;
        }

        refillRoot.style.opacity = 0f;
        refillRoot.style.display = DisplayStyle.None;
        isActive = false;

        if (currentTrigger != null)
            currentTrigger.NotifyUIClosed();
    }
}
