using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent clock HUD shown across scenes.
/// Reads live time from DayNightCycleNice2D and falls back to saved time.
/// </summary>
public class GlobalClockHUD : MonoBehaviour
{
    [SerializeField] private Vector2 topLeftOffset = new Vector2(24f, -24f);
    [SerializeField] private int fontSize = 26;

    private DayNightCycleNice2D _cycle;
    private Text _clockText;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        BuildHudIfNeeded();
        RebindCycle();
        Refresh();
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
        RebindCycle();
        Refresh();
    }

    private void Update()
    {
        if (_cycle == null)
            RebindCycle();

        Refresh();
    }

    private void RebindCycle()
    {
        // Use public static accessor first (more reliable and performant)
        _cycle = DayNightCycleNice2D.Instance;

        // Fallback to FindFirstObjectByType if needed
        if (_cycle == null)
            _cycle = FindFirstObjectByType<DayNightCycleNice2D>();
    }

    private void BuildHudIfNeeded()
    {
        if (_clockText != null)
            return;

        Canvas canvas = GetComponentInChildren<Canvas>(true);
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("GlobalClockCanvas");
            canvasGo.transform.SetParent(transform, false);

            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1001;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            CanvasGroup group = canvasGo.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        GameObject panelGo = new GameObject("ClockPanel");
        panelGo.transform.SetParent(canvas.transform, false);

        RectTransform panelRt = panelGo.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0f, 1f);
        panelRt.anchorMax = new Vector2(0f, 1f);
        panelRt.pivot = new Vector2(0f, 1f);
        panelRt.anchoredPosition = topLeftOffset;
        panelRt.sizeDelta = new Vector2(170f, 54f);

        Image panelImage = panelGo.AddComponent<Image>();
        panelImage.color = new Color(0.12f, 0.09f, 0.06f, 0.68f);

        Outline outline = panelGo.AddComponent<Outline>();
        outline.effectColor = new Color(0.93f, 0.72f, 0.38f, 0.82f);
        outline.effectDistance = new Vector2(1.6f, -1.6f);

        GameObject textGo = new GameObject("ClockText");
        textGo.transform.SetParent(panelGo.transform, false);

        RectTransform rt = textGo.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        _clockText = textGo.AddComponent<Text>();
        _clockText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _clockText.fontSize = fontSize;
        _clockText.fontStyle = FontStyle.Bold;
        _clockText.alignment = TextAnchor.MiddleCenter;
        _clockText.color = new Color(1f, 0.95f, 0.78f, 1f);
        _clockText.raycastTarget = false;

        Shadow shadow = textGo.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.05f, 0.03f, 0.02f, 0.9f);
        shadow.effectDistance = new Vector2(1.4f, -1.4f);
    }

    private void Refresh()
    {
        if (_clockText == null)
            return;

        _clockText.text = _cycle != null ? _cycle.GetTimeString() : DayNightCycleNice2D.GetSavedTimeString();
    }
}
