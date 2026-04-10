using UnityEngine;
using UnityEngine.Rendering.Universal;
using System;
using UnityEngine.SceneManagement;

public class DayNightCycleNice2D : MonoBehaviour
{
    private static DayNightCycleNice2D s_instance;
    private static bool s_isFirstInitialization = true; // Track if Awake() has run in this session

    private const string SavedTimeKey = "DayNight_TimeNormalized";
    private const string SavedDayKey = "DayNight_DayIndex";

    private static bool s_hasPersistentState;
    private static float s_persistentTimeNormalized;
    private static int s_persistentDay;

    [Header("Persistence")]
    [Tooltip("If true, time is saved/loaded across game sessions via PlayerPrefs. If false (default), each play starts on a fresh day.")]
    [SerializeField] private bool persistAcrossSessions = false;

    /// <summary>
    /// Public static accessor to get the DayNightCycleNice2D instance safely.
    /// Preferred over FindFirstObjectByType for performance.
    /// </summary>
    public static DayNightCycleNice2D Instance => s_instance;

    /// <summary>
    /// Ensures statics reset at the start of each play session (works even if domain reload is disabled).
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        s_instance = null;
        s_isFirstInitialization = true;
        s_hasPersistentState = false;
        s_persistentTimeNormalized = 0f;
        s_persistentDay = 0;
    }

    [Header("References")]
    [SerializeField] private Light2D globalLight;          // Global Light 2D
    [SerializeField] private Light2D moonLight;            // Moon Light 2D (optional for night)
    [SerializeField] private Camera mainCamera;            // Main Camera (or leave empty)
    [SerializeField] private SpriteRenderer nightOverlay;  // NightOverlay sprite renderer
    [SerializeField] private Transform playerTransform;    // Player (auto-found by tag if empty)
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private PickupToastUIToolkit toastUI; // Toast notifications (auto-found if empty)

    [Header("Cycle")]
    [Tooltip("Real seconds for a full 24h cycle. 60 for testing (fast), 300+ for realistic")]
    [SerializeField] private float dayLengthSeconds = 60f;
    [SerializeField] private bool autoCreateGlobalClockHud = true;
    [SerializeField] private string lightingSceneName = "FarmScene";
    [SerializeField] private string[] stableIndoorSceneNames = { "RestaurantScene", "MarketScene" };

    [Range(0f, 1f)]
    [Tooltip("0 = midnight, 0.25 = 6AM, 0.5 = noon, 0.75 = 6PM")]
    [SerializeField] private float startTimeNormalized = 0.25f;

    [Header("Global Light Look")]
    [SerializeField] private Gradient lightColor;
    [SerializeField] private AnimationCurve lightIntensity;

    [Header("Moon Light Look")]
    [SerializeField] private Gradient moonColor;
    [SerializeField] private AnimationCurve moonIntensity;

    [Header("Overlay Look (makes night feel deep)")]
    [SerializeField] private Gradient overlayTint;
    [SerializeField] private AnimationCurve overlayAlpha;

    public float TimeNormalized { get; private set; } // 0..1

    // Day advancement event
    public static event Action OnDayAdvanced;
    private int currentDay = 0;
    private float lastTimeNormalized = 0f;
    private bool _warnedInvalidDayLength;
    private float _nextDiskSaveTime;
    private bool _hasShownNightNotification = false; // Track if night notification was shown today
    private Light2D _runtimeIndoorGlobalLight;

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
        ResolveRuntimeReferences();

        if (IsStableIndoorScene(scene.name))
        {
            ApplyIndoorLightingOverride();
            return;
        }

        if (!IsLightingScene(scene.name))
            return;

        RestoreFarmLightingTargets();

        // CRITICAL FIX: Re-find lights after scene load
        // When transitioning scenes, old lights become null, breaking the Apply() method
        if (globalLight == null && mode != LoadSceneMode.Additive)
        {
            globalLight = FindSceneGlobalLight();
        }

        FitOverlayToCamera();
        Apply();
    }

    private void ResolveRuntimeReferences()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
                playerTransform = player.transform;
        }

        if (toastUI == null)
        {
            toastUI = FindFirstObjectByType<PickupToastUIToolkit>();
        }
    }

    private void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_instance = this;
        DontDestroyOnLoad(gameObject);

        // FIX: Only use in-memory state for same-session scene transitions.
        // If this is the first initialization for the session, start fresh unless persistence is explicitly enabled.
        if (!s_isFirstInitialization && s_hasPersistentState)
        {
            TimeNormalized = s_persistentTimeNormalized;
            currentDay = s_persistentDay;

        }
        else if (persistAcrossSessions && PlayerPrefs.HasKey(SavedTimeKey))
        {
            TimeNormalized = Mathf.Repeat(PlayerPrefs.GetFloat(SavedTimeKey, startTimeNormalized), 1f);
            currentDay = Mathf.Max(0, PlayerPrefs.GetInt(SavedDayKey, 0));
            SavePersistentState();

        }
        else
        {
            // Fresh start - always begin on a bright new day
            TimeNormalized = startTimeNormalized;
            currentDay = 0;
            SavePersistentState();

            // If we are not persisting across sessions, clear any old PlayerPrefs keys to avoid stale loads elsewhere
            if (!persistAcrossSessions)
            {
                PlayerPrefs.DeleteKey(SavedTimeKey);
                PlayerPrefs.DeleteKey(SavedDayKey);
            }


        }

        // Mark that we've initialized once in this session
        s_isFirstInitialization = false;

        ResolveRuntimeReferences();

        if (autoCreateGlobalClockHud)
            EnsureGlobalClockHudExists();

        if (globalLight == null) globalLight = FindSceneGlobalLight();

        if (lightColor == null || lightColor.colorKeys.Length == 0 ||
            lightIntensity == null || lightIntensity.length == 0 ||
            overlayTint == null || overlayTint.colorKeys.Length == 0 ||
            overlayAlpha == null || overlayAlpha.length == 0)
        {
            InitializeDefaultGradients();
        }

        if (moonColor == null || moonColor.colorKeys.Length == 0 ||
            moonIntensity == null || moonIntensity.length == 0)
        {
            InitializeDefaultMoonLight();
        }

        // Debug moonLight setup
        if (moonLight != null)
        {
            if (moonLight.intensity <= 0)
            {
                // Intensity is too low
            }
        }

        Apply();
        FitOverlayToCamera();
        _nextDiskSaveTime = Time.unscaledTime + 1f;
    }

    private void EnsureGlobalClockHudExists()
    {
        GlobalClockHUD existingHud = FindFirstObjectByType<GlobalClockHUD>();
        if (existingHud != null)
            return;

        GameObject hudGo = new GameObject("GlobalClockHUD");
        hudGo.AddComponent<GlobalClockHUD>();
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

        lightIntensity = new AnimationCurve(
      new Keyframe(0.0f, 0.4f, 0f, 0f),
      new Keyframe(0.25f, 0.85f, 0f, 0f),
      new Keyframe(0.5f, 1.0f, 0f, 0f),
      new Keyframe(0.75f, 0.85f, 0f, 0f),
      new Keyframe(1.0f, 0.4f, 0f, 0f)
  );

        overlayAlpha = new AnimationCurve(
            new Keyframe(0.0f, 0.08f, 0f, 0f),
            new Keyframe(0.2f, 0.06f, 0f, 0f),
            new Keyframe(0.35f, 0.02f, 0f, 0f),
            new Keyframe(0.5f, 0.0f, 0f, 0f),
            new Keyframe(0.65f, 0.02f, 0f, 0f),
            new Keyframe(0.8f, 0.06f, 0f, 0f),
            new Keyframe(1.0f, 0.08f, 0f, 0f)
        );
    }

    private void InitializeDefaultMoonLight()
    {
        // Moon color gradient - pale bluish white
        moonColor = new Gradient();
        moonColor.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.8f, 0.8f, 1f), 0.0f),   // Midnight: full moon, pale blue-white
                new GradientColorKey(new Color(0.7f, 0.7f, 0.9f), 0.25f), // Morning: moon sets
                new GradientColorKey(new Color(1f, 1f, 1f), 0.5f),        // Noon: white (no moon)
                new GradientColorKey(new Color(0.7f, 0.7f, 0.9f), 0.75f), // Evening: moon rises
                new GradientColorKey(new Color(0.8f, 0.8f, 1f), 1.0f)    // Midnight: full moon again
            },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );

        // Moon intensity curve - visible only at night (roughly 18:00 to 6:00)
        moonIntensity = new AnimationCurve(
            new Keyframe(0.0f, 1.0f, 0f, 0f),   // Midnight: strong moonlight
            new Keyframe(0.25f, 0.3f, 0f, 0f),  // 6 AM: moon fading
            new Keyframe(0.35f, 0.0f, 0f, 0f),  // 8:24 AM: moon out
            new Keyframe(0.5f, 0.0f, 0f, 0f),   // Noon: no moonlight
            new Keyframe(0.65f, 0.0f, 0f, 0f),  // 3:36 PM: no moon
            new Keyframe(0.75f, 0.3f, 0f, 0f),  // 6 PM: moon rising
            new Keyframe(0.9f, 1.0f, 0f, 0f),   // 9:36 PM: strong moon
            new Keyframe(1.0f, 1.0f, 0f, 0f)    // Midnight: strong moonlight
        );
    }

    private void Update()
    {
        if (dayLengthSeconds <= 0f)
        {
            if (!_warnedInvalidDayLength)
            {

                _warnedInvalidDayLength = true;
            }

            dayLengthSeconds = 60f;
        }

        lastTimeNormalized = TimeNormalized;
        TimeNormalized += Time.deltaTime / dayLengthSeconds;

        // Check if we've crossed into a new day (time wrapped from < 1 to >= 1)
        if (TimeNormalized >= 1f)
        {
            TimeNormalized -= 1f;
            currentDay++;
            _hasShownNightNotification = false; // Reset notification flag for new day
            OnDayAdvanced?.Invoke();

        }

        // Show "turn on flashlight" notification at dusk (~6 PM, 0.75)
        if (TimeNormalized >= 0.75f && !_hasShownNightNotification)
        {
            _hasShownNightNotification = true;
            if (toastUI != null)
            {
                toastUI.Show("🌙 Night approaching! Press F to turn on your flashlight", 6.0f);
            }
        }

        Apply();
        SavePersistentState();

        if (Time.unscaledTime >= _nextDiskSaveTime)
        {
            SaveToDisk();
            _nextDiskSaveTime = Time.unscaledTime + 1f;
        }
    }

    private void SavePersistentState()
    {
        s_hasPersistentState = true;
        s_persistentTimeNormalized = TimeNormalized;
        s_persistentDay = currentDay;
    }

    private void SaveToDisk()
    {
        if (!persistAcrossSessions)
            return;

        PlayerPrefs.SetFloat(SavedTimeKey, Mathf.Repeat(TimeNormalized, 1f));
        PlayerPrefs.SetInt(SavedDayKey, Mathf.Max(0, currentDay));
        PlayerPrefs.Save();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SaveToDisk();
    }

    private void OnApplicationQuit()
    {
        SaveToDisk();
    }

    private void LateUpdate()
    {
        if (!IsLightingScene(SceneManager.GetActiveScene().name))
            return;

        ResolveRuntimeReferences();

        // CRITICAL: Re-find global light if it becomes null (e.g., after scene transitions)
        if (globalLight == null)
        {
            globalLight = FindSceneGlobalLight();
        }

        // Position overlay in LateUpdate so it matches final camera position (after Cinemachine)
        FitOverlayToCamera();

        // Position moonlight to follow player first (or camera fallback) and scale range dynamically
        if (moonLight != null && mainCamera != null)
        {
            Vector3 followPos = playerTransform != null ? playerTransform.position : mainCamera.transform.position;
            moonLight.transform.position = new Vector3(followPos.x, followPos.y, 0f);

            // Dynamically scale moonlight range based on camera size
            if (mainCamera.orthographic)
            {
                float cameraHeight = mainCamera.orthographicSize * 2f;
                moonLight.pointLightOuterRadius = cameraHeight * 1.5f;
            }

            // Enable/disable moonLight based on intensity
            float currentMoonIntensity = moonIntensity.Evaluate(TimeNormalized);
            moonLight.enabled = currentMoonIntensity > 0.05f;
        }
    }

    private void Apply()
    {
        if (!IsLightingScene(SceneManager.GetActiveScene().name))
            return;

        // Global light - handles both day and night coloring/brightness
        if (globalLight != null)
        {
            globalLight.color = lightColor.Evaluate(TimeNormalized);
            globalLight.intensity = lightIntensity.Evaluate(TimeNormalized);
        }

        // Moon light - adds extra brightness during night
        if (moonLight != null)
        {
            moonLight.color = moonColor.Evaluate(TimeNormalized);
            float moonIntensityValue = moonIntensity.Evaluate(TimeNormalized);

            // Multiply by 3.0 to boost the final brightness (stronger moonlight)
            moonLight.intensity = moonIntensityValue * 3.0f;

            // Also boost the global light at night to support moonlight visibility
            if (moonIntensityValue > 0.1f && globalLight != null)
            {
                globalLight.intensity = Mathf.Max(globalLight.intensity, 0.55f);
            }
        }

        // Overlay provides darkness and color tints (subtle enough for moonlight to shine through)
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

        // Follow player first for gameplay readability; fall back to camera.
        Vector3 followPos = playerTransform != null ? playerTransform.position : mainCamera.transform.position;
        nightOverlay.transform.position = new Vector3(followPos.x, followPos.y, nightOverlay.transform.position.z);

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

    private bool IsLightingScene(string sceneName)
    {
        return string.Equals(sceneName, lightingSceneName, StringComparison.Ordinal);
    }

    private bool IsStableIndoorScene(string sceneName)
    {
        if (stableIndoorSceneNames == null || stableIndoorSceneNames.Length == 0)
            return false;

        for (int i = 0; i < stableIndoorSceneNames.Length; i++)
        {
            if (string.Equals(sceneName, stableIndoorSceneNames[i], StringComparison.Ordinal))
                return true;
        }

        return false;
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

    public static string GetSavedTimeString()
    {
        float normalized = PlayerPrefs.GetFloat(SavedTimeKey, 0.25f);
        int totalMinutes = Mathf.RoundToInt(Mathf.Repeat(normalized, 1f) * 24f * 60f) % (24 * 60);
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
            new Keyframe(0.0f, 0.4f, 0f, 0f),   // Midnight: brighter with moonlight
            new Keyframe(0.25f, 0.85f, 0f, 0f),  // 6 AM: bright
            new Keyframe(0.5f, 1.0f, 0f, 0f),    // Noon: max brightness
            new Keyframe(0.75f, 0.85f, 0f, 0f),  // 6 PM: bright
            new Keyframe(1.0f, 0.4f, 0f, 0f)    // Midnight: brighter with moonlight
        );

        // Overlay alpha: dark at night, transparent at day
        overlayAlpha = new AnimationCurve(
            new Keyframe(0.0f, 0.08f, 0f, 0f),   // Midnight: subtle darkness + color
            new Keyframe(0.2f, 0.06f, 0f, 0f),    // Pre-dawn: subtle darkness
            new Keyframe(0.35f, 0.02f, 0f, 0f),   // Sunrise: fading
            new Keyframe(0.5f, 0.0f, 0f, 0f),    // Noon: no overlay
            new Keyframe(0.65f, 0.02f, 0f, 0f),   // Sunset: fading in
            new Keyframe(0.8f, 0.06f, 0f, 0f),    // Post-dusk: subtle darkness
            new Keyframe(1.0f, 0.08f, 0f, 0f)    // Midnight: subtle darkness + color
        );

        // Moon light
        Gradient moonColorGradient = new Gradient();
        moonColorGradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.8f, 0.8f, 1f), 0.0f),   // Midnight: full moon
                new GradientColorKey(new Color(0.7f, 0.7f, 0.9f), 0.25f), // Morning: moon sets
                new GradientColorKey(new Color(1f, 1f, 1f), 0.5f),        // Noon: white (no moon)
                new GradientColorKey(new Color(0.7f, 0.7f, 0.9f), 0.75f), // Evening: moon rises
                new GradientColorKey(new Color(0.8f, 0.8f, 1f), 1.0f)    // Midnight: full moon
            },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );
        moonColor = moonColorGradient;

        // Moon intensity
        moonIntensity = new AnimationCurve(
            new Keyframe(0.0f, 1.0f, 0f, 0f),   // Midnight: strong moonlight
            new Keyframe(0.25f, 0.3f, 0f, 0f),  // 6 AM: moon fading
            new Keyframe(0.35f, 0.0f, 0f, 0f),  // 8:24 AM: moon out
            new Keyframe(0.5f, 0.0f, 0f, 0f),   // Noon: no moonlight
            new Keyframe(0.65f, 0.0f, 0f, 0f),  // 3:36 PM: no moon
            new Keyframe(0.75f, 0.3f, 0f, 0f),  // 6 PM: moon rising
            new Keyframe(0.9f, 1.0f, 0f, 0f),   // 9:36 PM: strong moon
            new Keyframe(1.0f, 1.0f, 0f, 0f)    // Midnight: strong moonlight
        );


    }

    public int GetHour24() => Mathf.FloorToInt(TimeNormalized * 24f) % 24;

    /// <summary>
    /// Reset the day/night cycle to a fresh new day. Call this when starting a new game.
    /// </summary>
    public void ResetToNewDay()
    {
        TimeNormalized = startTimeNormalized;
        currentDay = 0;
        SavePersistentState();
        PlayerPrefs.DeleteKey(SavedTimeKey);
        PlayerPrefs.DeleteKey(SavedDayKey);
        PlayerPrefs.Save();
        Apply();

    }

    /// <summary>
    /// Force a complete day/night system refresh. Useful after scene transitions if lighting looks wrong.
    /// </summary>
    public void RefreshLighting()
    {
        globalLight = FindSceneGlobalLight();
        if (globalLight != null)
        {
            Apply();

        }
        else
        {

        }
    }

    private void ApplyIndoorLightingOverride()
    {
        EnsureIndoorNeutralGlobalLight();

        if (nightOverlay != null)
        {
            Color c = nightOverlay.color;
            nightOverlay.color = new Color(c.r, c.g, c.b, 0f);
            nightOverlay.enabled = false;
        }

        if (moonLight != null)
            moonLight.enabled = false;

        globalLight = null;

        Scene activeScene = SceneManager.GetActiveScene();
        Light2D[] lights = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
        for (int i = 0; i < lights.Length; i++)
        {
            Light2D l = lights[i];
            if (l == null)
                continue;

            if (l.gameObject.scene != activeScene)
                continue;

            if (l == _runtimeIndoorGlobalLight)
                continue;

            if (l.lightType != Light2D.LightType.Global)
                l.enabled = false;
        }
    }

    private void RestoreFarmLightingTargets()
    {
        if (nightOverlay != null)
            nightOverlay.enabled = true;

        if (moonLight == null)
            moonLight = FindScenePointLightByName("MoonLight");

        globalLight = FindSceneGlobalLight();
    }

    private Light2D FindSceneGlobalLight()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        Light2D[] lights = FindObjectsByType<Light2D>(FindObjectsSortMode.None);

        for (int i = 0; i < lights.Length; i++)
        {
            Light2D l = lights[i];
            if (l == null)
                continue;

            if (l.gameObject.scene != activeScene)
                continue;

            if (l.lightType == Light2D.LightType.Global)
                return l;
        }

        return null;
    }

    private Light2D FindScenePointLightByName(string objectName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        Light2D[] lights = FindObjectsByType<Light2D>(FindObjectsSortMode.None);

        for (int i = 0; i < lights.Length; i++)
        {
            Light2D l = lights[i];
            if (l == null)
                continue;

            if (l.gameObject.scene != activeScene)
                continue;

            if (!string.Equals(l.gameObject.name, objectName, StringComparison.Ordinal))
                continue;

            return l;
        }

        return null;
    }

    private void EnsureIndoorNeutralGlobalLight()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        if (_runtimeIndoorGlobalLight == null)
        {
            GameObject go = new GameObject("IndoorNeutralGlobalLight");
            SceneManager.MoveGameObjectToScene(go, activeScene);
            _runtimeIndoorGlobalLight = go.AddComponent<Light2D>();
            _runtimeIndoorGlobalLight.lightType = Light2D.LightType.Global;
            _runtimeIndoorGlobalLight.blendStyleIndex = 0;
        }
        else if (_runtimeIndoorGlobalLight.gameObject.scene != activeScene)
        {
            SceneManager.MoveGameObjectToScene(_runtimeIndoorGlobalLight.gameObject, activeScene);
        }

        _runtimeIndoorGlobalLight.color = Color.white;
        _runtimeIndoorGlobalLight.intensity = 1f;
        _runtimeIndoorGlobalLight.enabled = true;
    }
}