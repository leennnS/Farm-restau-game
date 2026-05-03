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
    [SerializeField] private float flashlightInnerRadius = 0.6741534f;
    [SerializeField] private float flashlightRange = 11.27706f;
    [SerializeField] private float flashlightIntensity = 0.85f;
    [SerializeField] private Color flashlightColor = new Color(1f, 0.95f, 0.85f); // Warm white
    [SerializeField] private float forwardOffset = 0.08f;
    [SerializeField] private float coneSpreadOffset = 0f;
    [SerializeField] private float coneOuterAngle = 65.18f;
    [SerializeField] private float coneInnerAngle = 7.6f;
    [SerializeField] private float falloffStrength = 0.5f;
    [SerializeField] private int blendStyleIndex = 0;
    [SerializeField] private int lightOrder = 0;
    [SerializeField] private float shadowStrength = 0.75f;
    [SerializeField] private float shadowSoftness = 0.3f;
    [SerializeField] private float shadowFalloffStrength = 0.5f;
    [SerializeField] private float nightStartHour = 18f;
    [SerializeField] private float nightEndHour = 6f;

    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private string allowedSceneName = "FarmScene";

    private bool isFlashlightOn = false;
    private float currentIntensity = 0f;
    private float targetIntensity = 0f;
    private Vector2 lastDirection = Vector2.right; // Default direction
    private Light2D[] coneLights; // Array of lights forming a cone
    private const int CONE_LIGHT_COUNT = 1; // Single 2D spot light matching the inspector settings
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
            light.pointLightInnerRadius = flashlightInnerRadius;
            light.pointLightOuterRadius = flashlightRange;
            light.pointLightOuterAngle = coneOuterAngle;
            light.pointLightInnerAngle = coneInnerAngle;
            light.falloffIntensity = falloffStrength;
            light.blendStyleIndex = blendStyleIndex;
            light.lightOrder = lightOrder;
            light.shadowsEnabled = true;
            light.shadowIntensity = shadowStrength;
            light.shadowSoftness = shadowSoftness;
            light.shadowSoftnessFalloffIntensity = shadowFalloffStrength;
            light.volumetricShadowsEnabled = false;
            light.color = flashlightColor;
            light.enabled = true;

            coneLights[i] = light;
        }
    }

    private void Update()
    {
        if (!isAllowedScene || !IsNightTime())
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
            float coneAngle = 0f;
            float angleStep = coneLights.Length > 1 ? coneAngle / (coneLights.Length - 1) : 0f;
            Vector2 forwardPosition = lastDirection * forwardOffset;

            for (int i = 0; i < CONE_LIGHT_COUNT; i++)
            {
                float currentAngle = coneLights.Length > 1 ? baseAngle - (coneAngle / 2) + (i * angleStep) : baseAngle;
                float radians = currentAngle * Mathf.Deg2Rad;

                // Position each light in a cone formation
                float offsetX = forwardPosition.x + Mathf.Cos(radians) * coneSpreadOffset;
                float offsetY = forwardPosition.y + Mathf.Sin(radians) * coneSpreadOffset;
                coneLights[i].transform.localPosition = new Vector3(offsetX, offsetY, 0);
                coneLights[i].transform.localRotation = Quaternion.AngleAxis(currentAngle - 90f, Vector3.forward);
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

    private bool IsNightTime()
    {
        DayNightCycleNice2D cycle = DayNightCycleNice2D.Instance;
        if (cycle == null)
            return true;

        float currentHour = cycle.TimeNormalized * 24f;
        if (nightStartHour < nightEndHour)
            return currentHour >= nightStartHour && currentHour < nightEndHour;

        return currentHour >= nightStartHour || currentHour < nightEndHour;
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
        if (!isAllowedScene || !IsNightTime())
            return;

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
