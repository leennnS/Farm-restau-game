using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

/// <summary>
/// Animates the fish image during fishing to make it feel like it's struggling.
/// Wiggles and bounces the fish UI element during catch phase.
/// </summary>
public class FishStruggleDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FishingMiniGameController miniGameController;

    [Header("Animation Settings")]
    [SerializeField] private float wiggleAmount = 25f; // Pixels to wiggle (increased!)
    [SerializeField] private float wiggleSpeed = 6f;
    [SerializeField] private float maxBounce = 20f; // Pixels bounce (increased!)

    private VisualElement fishImageElement; // Auto-found, not in inspector
    private float wiggleTimer = 0f;
    private bool isAnimating = false;

    private void Start()
    {
        if (miniGameController == null)
            miniGameController = FindFirstObjectByType<FishingMiniGameController>();

        if (miniGameController != null)
        {
            miniGameController.OnBiteOccurred += StartStruggle;
            miniGameController.OnCatchComplete += StopStruggle;
        }

        FindFishImageElement();
    }

    private void FindFishImageElement()
    {
        var uiDoc = FindFirstObjectByType<UIDocument>();
        if (uiDoc != null && uiDoc.rootVisualElement != null)
        {
            fishImageElement = uiDoc.rootVisualElement.Q<VisualElement>("catchable-image");
            if (fishImageElement != null)
                return;

            var catchPanel = uiDoc.rootVisualElement.Q<VisualElement>("catch-panel");
            if (catchPanel != null)
            {
                var creatureInfo = catchPanel.Q<VisualElement>("creature-info");
                if (creatureInfo != null)
                {
                    fishImageElement = creatureInfo.Q<VisualElement>("catchable-image");
                }
            }
        }
    }

    private void Update()
    {
        if (isAnimating && fishImageElement != null)
        {
            AnimateFish();
        }
    }

    private void AnimateFish()
    {
        wiggleTimer += Time.deltaTime;

        // Wiggle left/right
        float wiggle = Mathf.Sin(wiggleTimer * wiggleSpeed) * wiggleAmount;

        // Bounce up/down
        float bounce = Mathf.Abs(Mathf.Sin(wiggleTimer * wiggleSpeed * 0.5f)) * maxBounce;

        // Apply translation
        fishImageElement.style.translate = new Translate(
            new Length(wiggle, LengthUnit.Pixel),
            new Length(bounce, LengthUnit.Pixel)
        );
    }

    private void StartStruggle()
    {
        isAnimating = true;
        wiggleTimer = 0f;
        Debug.Log("[FishStruggleDisplay] !! BITE OCCURRED !! Starting fish animation!");
    }

    private void StopStruggle(FishingResultType result)
    {
        isAnimating = false;
        wiggleTimer = 0f;
        Debug.Log("[FishStruggleDisplay] !! CATCH COMPLETE (" + result + ") !! Stopping animation");

        // Reset position
        if (fishImageElement != null)
        {
            fishImageElement.style.translate = new Translate(0, 0);
        }
    }

    private void OnDestroy()
    {
        if (miniGameController != null)
        {
            miniGameController.OnBiteOccurred -= StartStruggle;
            miniGameController.OnCatchComplete -= StopStruggle;
        }
    }
}
