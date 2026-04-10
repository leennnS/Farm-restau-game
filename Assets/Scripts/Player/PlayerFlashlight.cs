using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

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
    [SerializeField] private string allowedSceneName = "FarmScene";

    private bool isFlashlightOn = false;
    private float currentIntensity = 0f;
    private float targetIntensity = 0f;
    private Vector2 lastDirection = Vector2.right; // Default direction
    private Light2D[] coneLights; // Array of lights forming a cone
    private const int CONE_LIGHT_COUNT = 5; // Number of lights in the cone
    private bool isAllowedScene;

    private void Start()
    {
        SetupFlashlight();
        RefreshSceneAllowance(SceneManager.GetActiveScene().name);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshSceneAllowance(scene.name);
    }

    private void SetupFlashlight()
    {
        // Remove the old point light if it exists
        Light2D existingLight = GetComponent<Light2D>();
        if (existingLight != null)
        {
            Destroy(existingLight);
        }

        // Create an array of lights forming a cone shape
        coneLights = new Light2D[CONE_LIGHT_COUNT];

        for (int i = 0; i < CONE_LIGHT_COUNT; i++)
        {
            // Create child objects for each light
            GameObject lightObj = new GameObject($"ConeLightCenter");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.zero;

            Light2D light = lightObj.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.intensity = 0f;
            light.pointLightOuterRadius = flashlightRange;
            light.color = flashlightColor;
            light.enabled = true;

            coneLights[i] = light;
        }
    }

    private void Update()
    {
        if (!isAllowedScene)
        {
            ForceOffImmediate();

            return;
        }

        HandleInput();
        UpdatePlayerDirection();
        UpdateFlashlightIntensity();
    }

    private void UpdatePlayerDirection()
    {
        // Get movement input to determine flashlight direction
        float inputX = Input.GetAxis("Horizontal");
        float inputY = Input.GetAxis("Vertical");

        Vector2 inputDirection = new Vector2(inputX, inputY);

        // Only update direction if there's actual movement input
        if (inputDirection.magnitude > 0.1f)
        {
            lastDirection = inputDirection.normalized;
        }

        // Update cone light positions and angles
        if (coneLights != null && coneLights.Length > 0)
        {
            float baseAngle = Mathf.Atan2(lastDirection.y, lastDirection.x) * Mathf.Rad2Deg;
            float coneAngle = 60f;
            float coneRadius = flashlightRange * 0.15f; // Offset from center
            float angleStep = coneAngle / (CONE_LIGHT_COUNT - 1);

            for (int i = 0; i < CONE_LIGHT_COUNT; i++)
            {
                float currentAngle = baseAngle - (coneAngle / 2) + (i * angleStep);
                float radians = currentAngle * Mathf.Deg2Rad;

                // Position each light in a cone formation
                float offsetX = Mathf.Cos(radians) * coneRadius;
                float offsetY = Mathf.Sin(radians) * coneRadius;
                coneLights[i].transform.localPosition = new Vector3(offsetX, offsetY, 0);
            }
        }
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isFlashlightOn = !isFlashlightOn;
            targetIntensity = isFlashlightOn ? flashlightIntensity : 0f;
        }
    }

    private void UpdateFlashlightIntensity()
    {
        // Smooth fade in/out
        float duration = targetIntensity > currentIntensity ? fadeInDuration : fadeOutDuration;
        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime / duration);

        ApplyConeIntensity(currentIntensity);
    }

    private void ApplyConeIntensity(float intensity)
    {
        if (coneLights != null)
        {
            foreach (Light2D light in coneLights)
            {
                if (light != null)
                {
                    light.intensity = intensity / CONE_LIGHT_COUNT; // Divide by light count to prevent overlap brightness
                }
            }
        }
    }

    private void RefreshSceneAllowance(string sceneName)
    {
        isAllowedScene = string.Equals(sceneName, allowedSceneName, System.StringComparison.Ordinal);

        if (!isAllowedScene)
            ForceOffImmediate();
    }

    private void ForceOffImmediate()
    {
        if (!Mathf.Approximately(currentIntensity, 0f) || isFlashlightOn || !Mathf.Approximately(targetIntensity, 0f))
        {
            isFlashlightOn = false;
            targetIntensity = 0f;
            currentIntensity = 0f;
            ApplyConeIntensity(0f);
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
