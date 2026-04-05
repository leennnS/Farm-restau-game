using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Player-held flashlight that toggles with 'F' key.
/// Creates a dynamic light following the player for night exploration.
/// </summary>
public class PlayerFlashlight : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F;

    [Header("Light Settings")]
    [SerializeField] private float flashlightRange = 8f;
    [SerializeField] private float flashlightIntensity = 1.2f;
    [SerializeField] private Color flashlightColor = new Color(1f, 0.95f, 0.85f); // Warm white

    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    private Light2D flashlight;
    private bool isFlashlightOn = false;
    private float currentIntensity = 0f;
    private float targetIntensity = 0f;

    private void Start()
    {
        SetupFlashlight();
    }

    private void SetupFlashlight()
    {
        // Create or find a flashlight light on this object
        flashlight = GetComponent<Light2D>();

        if (flashlight == null)
        {
            flashlight = gameObject.AddComponent<Light2D>();
        }

        // Configure flashlight properties
        flashlight.lightType = Light2D.LightType.Point;
        flashlight.intensity = 0f;
        flashlight.pointLightOuterRadius = flashlightRange;
        flashlight.color = flashlightColor;
        flashlight.enabled = true; // Keep enabled, control via intensity

        Debug.Log("[PlayerFlashlight] Flashlight created! Press 'F' to toggle.");
    }

    private void Update()
    {
        HandleInput();
        UpdateFlashlightIntensity();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isFlashlightOn = !isFlashlightOn;
            targetIntensity = isFlashlightOn ? flashlightIntensity : 0f;

            string status = isFlashlightOn ? "ON" : "OFF";
            Debug.Log($"[PlayerFlashlight] Flashlight turned {status}");
        }
    }

    private void UpdateFlashlightIntensity()
    {
        // Smooth fade in/out
        float duration = targetIntensity > currentIntensity ? fadeInDuration : fadeOutDuration;
        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime / duration);

        if (flashlight != null)
        {
            flashlight.intensity = currentIntensity;
        }
    }

    // Public methods for external control
    public void TurnOn()
    {
        isFlashlightOn = true;
        targetIntensity = flashlightIntensity;
    }

    public void TurnOff()
    {
        isFlashlightOn = false;
        targetIntensity = 0f;
    }

    public bool IsOn => isFlashlightOn;
}
