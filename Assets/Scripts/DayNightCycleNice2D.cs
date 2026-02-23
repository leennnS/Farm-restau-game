using UnityEngine;
using UnityEngine.Rendering.Universal;
using System;

public class DayNightCycleNice2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light2D globalLight;          // Global Light 2D
    [SerializeField] private Camera mainCamera;            // Main Camera (or leave empty)
    [SerializeField] private SpriteRenderer nightOverlay;  // NightOverlay sprite renderer

    [Header("Cycle")]
    [Tooltip("Real seconds for a full 24h cycle. 60 for testing (fast), 300+ for realistic")]
    [SerializeField] private float dayLengthSeconds = 60f;

    [Range(0f, 1f)]
    [Tooltip("0 = midnight, 0.25 = 6AM, 0.5 = noon, 0.75 = 6PM")]
    [SerializeField] private float startTimeNormalized = 0.25f;

    [Header("Global Light Look")]
    [SerializeField] private Gradient lightColor;
    [SerializeField] private AnimationCurve lightIntensity;

    [Header("Overlay Look (makes night feel deep)")]
    [SerializeField] private Gradient overlayTint;
    [SerializeField] private AnimationCurve overlayAlpha;

    public float TimeNormalized { get; private set; } // 0..1

    // Day advancement event
    public static event Action OnDayAdvanced;
    private int currentDay = 0;
    private float lastTimeNormalized = 0f;

    private void Awake()
    {
        TimeNormalized = startTimeNormalized;

        if (mainCamera == null) mainCamera = Camera.main;

        // safe auto-find
        if (globalLight == null) globalLight = FindFirstObjectByType<Light2D>();

        // Initialize gradients/curves if empty
        if (lightColor == null || lightColor.colorKeys.Length == 0)
            InitializeDefaultGradients();

        Apply();
        FitOverlayToCamera();
    }

    private void InitializeDefaultGradients()
    {
        // Light color gradient
        lightColor = new Gradient();
        lightColor.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.15f, 0.1f, 0.25f), 0.0f),   // Midnight: deep purple
                new GradientColorKey(new Color(1f, 0.5f, 0.2f), 0.15f),      // Pre-dawn: orange
                new GradientColorKey(new Color(1f, 0.95f, 0.8f), 0.25f),     // Sunrise: warm
                new GradientColorKey(new Color(1f, 1f, 1f), 0.5f),           // Noon: white
                new GradientColorKey(new Color(1f, 0.95f, 0.8f), 0.75f),     // Sunset: warm
                new GradientColorKey(new Color(1f, 0.5f, 0.2f), 0.85f),      // Post-dusk: orange
                new GradientColorKey(new Color(0.15f, 0.1f, 0.25f), 1.0f)    // Midnight: deep purple
            },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );

        // Overlay tint gradient
        overlayTint = new Gradient();
        overlayTint.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.05f, 0.1f, 0.25f), 0.0f),
                new GradientColorKey(new Color(0.2f, 0.15f, 0.35f), 0.2f),
                new GradientColorKey(new Color(0.7f, 0.6f, 0.8f), 0.4f),
                new GradientColorKey(new Color(1f, 1f, 1f), 0.5f),
                new GradientColorKey(new Color(0.7f, 0.6f, 0.8f), 0.6f),
                new GradientColorKey(new Color(0.2f, 0.15f, 0.35f), 0.8f),
                new GradientColorKey(new Color(0.05f, 0.1f, 0.25f), 1.0f)
            },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );

        // Light intensity curve
        lightIntensity = new AnimationCurve(
            new Keyframe(0.0f, 0.15f, 0f, 0f),
            new Keyframe(0.25f, 0.85f, 0f, 0f),
            new Keyframe(0.5f, 1.0f, 0f, 0f),
            new Keyframe(0.75f, 0.85f, 0f, 0f),
            new Keyframe(1.0f, 0.15f, 0f, 0f)
        );

        // Overlay alpha curve
        overlayAlpha = new AnimationCurve(
            new Keyframe(0.0f, 0.75f, 0f, 0f),
            new Keyframe(0.2f, 0.4f, 0f, 0f),
            new Keyframe(0.35f, 0.1f, 0f, 0f),
            new Keyframe(0.5f, 0.0f, 0f, 0f),
            new Keyframe(0.65f, 0.1f, 0f, 0f),
            new Keyframe(0.8f, 0.4f, 0f, 0f),
            new Keyframe(1.0f, 0.75f, 0f, 0f)
        );
    }

    private void Update()
    {
        if (dayLengthSeconds <= 0f) return;

        lastTimeNormalized = TimeNormalized;
        TimeNormalized += Time.deltaTime / dayLengthSeconds;

        // Check if we've crossed into a new day (time wrapped from < 1 to >= 1)
        if (TimeNormalized >= 1f)
        {
            TimeNormalized -= 1f;
            currentDay++;
            OnDayAdvanced?.Invoke();
            Debug.Log($"[DayNightCycleNice2D] Day {currentDay} started!");
        }

        Apply();
    }

    private void LateUpdate()
    {
        // Position overlay in LateUpdate so it matches final camera position (after Cinemachine)
        FitOverlayToCamera();
    }

    private void Apply()
    {
        // Global light
        if (globalLight != null)
        {
            globalLight.color = lightColor.Evaluate(TimeNormalized);
            globalLight.intensity = lightIntensity.Evaluate(TimeNormalized);
        }

        // World overlay
        if (nightOverlay != null)
        {
            Color tint = overlayTint.Evaluate(TimeNormalized);
            float a = Mathf.Clamp01(overlayAlpha.Evaluate(TimeNormalized));
            nightOverlay.color = new Color(tint.r, tint.g, tint.b, a);
        }
    }

    private void FitOverlayToCamera()
    {
        if (nightOverlay == null || mainCamera == null) return;

        // Follow camera position
        Vector3 camPos = mainCamera.transform.position;
        nightOverlay.transform.position = new Vector3(camPos.x, camPos.y, nightOverlay.transform.position.z);

        // Scale to cover camera view (orthographic)
        if (mainCamera.orthographic)
        {
            float height = mainCamera.orthographicSize * 2f;
            float width = height * mainCamera.aspect;

            // Assumes sprite is 1 world-unit in size (Unity's default square sprite)
            nightOverlay.transform.localScale = new Vector3(width, height, 1f);
        }
        else
        {
            // If perspective, just scale large enough to cover view
            nightOverlay.transform.localScale = Vector3.one * 100f;
        }
    }

    /// <summary>
    /// Returns current in-game time as HH:MM string (24-hour format)
    /// </summary>
    public string GetTimeString()
    {
        // Convert normalized time (0-1) to total minutes in a 24-hour day
        int totalMinutes = Mathf.RoundToInt(TimeNormalized * 24f * 60f);
        totalMinutes = totalMinutes % (24 * 60); // Ensure within 24 hours

        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;

        return $"{hours:D2}:{minutes:D2}";
    }

    /// <summary>
    /// Auto-fill gradients and curves with Stardew Valley-like defaults
    /// Call via right-click → Reset to Defaults in Inspector
    /// </summary>
    [ContextMenu("Reset to Defaults")]
    public void Reset()
    {
        // Set test day length if not already customized
        if (dayLengthSeconds == 300f)
            dayLengthSeconds = 60f; // Fast cycle for testing

        // Light color gradient: night → sunrise → day → sunset → night
        Gradient lightColorGradient = new Gradient();
        lightColorGradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.15f, 0.1f, 0.25f), 0.0f),   // Midnight: deep purple
                new GradientColorKey(new Color(1f, 0.5f, 0.2f), 0.15f),      // Pre-dawn: orange
                new GradientColorKey(new Color(1f, 0.95f, 0.8f), 0.25f),     // Sunrise: warm
                new GradientColorKey(new Color(1f, 1f, 1f), 0.5f),           // Noon: white
                new GradientColorKey(new Color(1f, 0.95f, 0.8f), 0.75f),     // Sunset: warm
                new GradientColorKey(new Color(1f, 0.5f, 0.2f), 0.85f),      // Post-dusk: orange
                new GradientColorKey(new Color(0.15f, 0.1f, 0.25f), 1.0f)    // Midnight: deep purple
            },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );
        lightColor = lightColorGradient;

        // Overlay tint: dark blue night, fading through purple/pink dawn/dusk
        Gradient overlayTintGradient = new Gradient();
        overlayTintGradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.05f, 0.1f, 0.25f), 0.0f),   // Midnight: deep blue
                new GradientColorKey(new Color(0.2f, 0.15f, 0.35f), 0.2f),   // Pre-dawn: purple
                new GradientColorKey(new Color(0.7f, 0.6f, 0.8f), 0.4f),     // Dawn: light purple
                new GradientColorKey(new Color(1f, 1f, 1f), 0.5f),           // Noon: white (alpha 0)
                new GradientColorKey(new Color(0.7f, 0.6f, 0.8f), 0.6f),     // Dusk: light purple
                new GradientColorKey(new Color(0.2f, 0.15f, 0.35f), 0.8f),   // Post-dusk: purple
                new GradientColorKey(new Color(0.05f, 0.1f, 0.25f), 1.0f)    // Midnight: deep blue
            },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );
        overlayTint = overlayTintGradient;

        // Light intensity: low at night, peak at noon
        lightIntensity = new AnimationCurve(
            new Keyframe(0.0f, 0.15f, 0f, 0f),   // Midnight: very dim
            new Keyframe(0.25f, 0.85f, 0f, 0f),  // 6 AM: bright
            new Keyframe(0.5f, 1.0f, 0f, 0f),    // Noon: max brightness
            new Keyframe(0.75f, 0.85f, 0f, 0f),  // 6 PM: bright
            new Keyframe(1.0f, 0.15f, 0f, 0f)    // Midnight: very dim
        );

        // Overlay alpha: dark at night, transparent at day
        overlayAlpha = new AnimationCurve(
            new Keyframe(0.0f, 0.75f, 0f, 0f),   // Midnight: very dark
            new Keyframe(0.2f, 0.4f, 0f, 0f),    // Pre-dawn: darkening
            new Keyframe(0.35f, 0.1f, 0f, 0f),   // Sunrise: fading
            new Keyframe(0.5f, 0.0f, 0f, 0f),    // Noon: no overlay
            new Keyframe(0.65f, 0.1f, 0f, 0f),   // Sunset: fading in
            new Keyframe(0.8f, 0.4f, 0f, 0f),    // Post-dusk: darkening
            new Keyframe(1.0f, 0.75f, 0f, 0f)    // Midnight: very dark
        );

        Debug.Log("DayNightCycleNice2D reset to Stardew-like defaults!");
    }

    public int GetHour24() => Mathf.FloorToInt(TimeNormalized * 24f) % 24;
}