using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

/// <summary>
/// UI Toolkit clock display - shows in-game time (HH:MM) from DayNightCycleNice2D
/// </summary>
public class ClockHUDController : MonoBehaviour
{
    [SerializeField] private UIDocument clockDocument;
    [SerializeField] private DayNightCycleNice2D cycle;
    [SerializeField] private string labelName = "clockLabel";

    private Label _clockLabel;
    private string _lastDisplayedTime = "";

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
        RebindCycle();
    }

    private void RebindCycle()
    {
        if (cycle == null)
        {
            // Use public static accessor first (more reliable)
            cycle = DayNightCycleNice2D.Instance;

            // Fallback to FindFirstObjectByType if needed
            if (cycle == null)
                cycle = FindFirstObjectByType<DayNightCycleNice2D>();
        }
    }

    private void Start()
    {
        if (FindFirstObjectByType<GlobalClockHUD>() != null)
        {
            if (clockDocument == null)
                clockDocument = GetComponent<UIDocument>();

            if (clockDocument != null)
                clockDocument.rootVisualElement.style.display = DisplayStyle.None;

            enabled = false;
            return;
        }

        // Cache UI reference
        if (clockDocument == null)
        {
            clockDocument = GetComponent<UIDocument>();
            Debug.Log("[ClockHUD] Auto-found UIDocument on this GameObject");
        }

        if (clockDocument != null)
        {
            _clockLabel = clockDocument.rootVisualElement.Q<Label>(labelName);
            if (_clockLabel == null)
                Debug.LogError($"[ClockHUD] Label '{labelName}' not found in UXML!");
            else
                Debug.Log($"[ClockHUD] Found label '{labelName}'");
        }
        else
        {
            Debug.LogError("[ClockHUD] UIDocument not assigned!");
            return;
        }

        // Cache cycle reference
        if (cycle == null)
        {
            RebindCycle();
            Debug.Log("[ClockHUD] Auto-found DayNightCycleNice2D");
        }

        if (cycle == null)
        {
            Debug.LogError("[ClockHUD] DayNightCycleNice2D not found in scene!");
            return;
        }

        // Force initial update
        if (_clockLabel != null && cycle != null)
        {
            string initialTime = cycle.GetTimeString();
            _clockLabel.text = initialTime;
            _lastDisplayedTime = initialTime;
            Debug.Log($"[ClockHUD] Clock initialized: {initialTime}");
        }
    }

    private void Update()
    {
        if (cycle == null)
            RebindCycle();

        if (_clockLabel == null || cycle == null)
            return;

        // Get current time string
        string timeString = cycle.GetTimeString();

        // Always update (no optimization to ensure it works)
        _clockLabel.text = timeString;
    }
}
