using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

/// <summary>
/// Handles animations for the fishing UI - tension bar pulses, screen shakes, and color transitions.
/// Simpler approach focused on what actually works with UI Toolkit.
/// </summary>
public class FishingAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FishingMiniGameController miniGameController;

    [Header("Animation Settings")]
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeAmount = 8f;

    private ProgressBar tensionBar; // Auto-found
    private bool isShaking = false;
    private Coroutine shakeCoroutine;

    private void Start()
    {
        if (miniGameController == null)
            miniGameController = FindFirstObjectByType<FishingMiniGameController>();

        if (miniGameController != null)
        {
            miniGameController.OnBiteOccurred += HandleBiteOccurred;
            miniGameController.OnTensionChanged += HandleTensionChanged;
        }

        FindTensionBar();
    }

    private void FindTensionBar()
    {
        var uiDoc = FindFirstObjectByType<UIDocument>();
        if (uiDoc != null && uiDoc.rootVisualElement != null)
        {
            tensionBar = uiDoc.rootVisualElement.Q<ProgressBar>("tension-bar");
            if (tensionBar != null)
                return;

            var container = uiDoc.rootVisualElement.Q<VisualElement>("tension-container");
            if (container != null)
            {
                tensionBar = container.Q<ProgressBar>("tension-bar");
            }
        }
    }

    private void HandleBiteOccurred()
    {
        // Start shake animation on bite
        if (tensionBar != null)
        {
            if (shakeCoroutine != null)
                StopCoroutine(shakeCoroutine);

            shakeCoroutine = StartCoroutine(ShakeTensionBar());
        }
    }

    private void HandleTensionChanged(float tension)
    {
        // Just for future expansion - animations happen in FishingUIController
    }

    private IEnumerator ShakeTensionBar()
    {
        if (tensionBar == null) yield break;

        float elapsed = 0f;
        Vector3 originalPos = Vector3.zero;

        while (elapsed < shakeDuration)
        {
            float shake = Mathf.Sin(elapsed * 30f) * shakeAmount;
            tensionBar.style.translate = new Translate(new Length(shake, LengthUnit.Pixel), 0);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Reset
        tensionBar.style.translate = new Translate(0, 0);
    }

    private void OnDestroy()
    {
        if (miniGameController != null)
        {
            miniGameController.OnBiteOccurred -= HandleBiteOccurred;
            miniGameController.OnTensionChanged -= HandleTensionChanged;
        }
    }
}
