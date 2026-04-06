using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Automatically toggles a Light2D based on day/night cycle.
/// Attach to any GameObject with a Light2D component.
/// </summary>
public class NightLight : MonoBehaviour
{
    [Header("Light Settings")]
    [SerializeField] private Light2D light2D;

    [Header("Night Time Settings")]
    [Tooltip("Time when light turns ON (0-24). Default 18 = 6 PM")]
    [SerializeField] private float nightStartTime = 18f;

    [Tooltip("Time when light turns OFF (0-24). Default 6 = 6 AM")]
    [SerializeField] private float nightEndTime = 6f;

    [SerializeField]
    private AnimationCurve nightIntensityCurve = new AnimationCurve(
        new Keyframe(0f, 0.5f),
        new Keyframe(0.5f, 1.0f),
        new Keyframe(1f, 0.5f)
    );

    private DayNightCycleNice2D dayNightCycle;

    private void Start()
    {
        // Auto-find Light2D if not assigned
        if (light2D == null)
            light2D = GetComponent<Light2D>();

        if (light2D == null)
        {

            enabled = false;
            return;
        }

        // Find the day/night cycle
        dayNightCycle = DayNightCycleNice2D.Instance;
        if (dayNightCycle == null)
        {

            enabled = false;
            return;
        }
    }

    private void Update()
    {
        if (dayNightCycle == null || light2D == null)
            return;

        // Get current normalized time (0-1)
        float timeNormalized = dayNightCycle.TimeNormalized;

        // Convert normalized time to 0-24 hour format
        float currentHour = timeNormalized * 24f;

        // Check if it's night time
        bool isNight = IsNightTime(currentHour);

        // Update light state
        if (isNight)
        {
            light2D.enabled = true;

            // Calculate intensity curve based on night progression
            float nightProgress = GetNightProgress(currentHour);
            float intensity = nightIntensityCurve.Evaluate(nightProgress);
            light2D.intensity = intensity;
        }
        else
        {
            light2D.enabled = false;
        }
    }

    private bool IsNightTime(float currentHour)
    {
        if (nightStartTime < nightEndTime)
        {
            // Normal case: e.g., 6 PM to 6 AM next day
            return currentHour >= nightStartTime || currentHour < nightEndTime;
        }
        else
        {
            // Edge case: wrapped around midnight
            return currentHour >= nightStartTime || currentHour < nightEndTime;
        }
    }

    private float GetNightProgress(float currentHour)
    {
        // Returns 0-1 progress through the night cycle
        if (nightStartTime < nightEndTime)
        {
            // Normal: 6 PM (18) to 6 AM (6)
            if (currentHour >= nightStartTime)
            {
                return (currentHour - nightStartTime) / (24f - nightStartTime);
            }
            else
            {
                return (currentHour + (24f - nightStartTime)) / (24f - nightStartTime);
            }
        }
        else
        {
            // Normal case
            return (currentHour - nightStartTime) / (nightEndTime + 24f - nightStartTime);
        }
    }
}
