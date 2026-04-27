using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Bottom-left button that advances to the next day.
/// Visible only in FarmScene.
/// </summary>
public class GlobalNextDayButtonHUD : MonoBehaviour
{
    [SerializeField] private string farmSceneName = "FarmScene";
    [SerializeField] private Vector2 bottomLeftOffset = new Vector2(26f, 26f);
    [SerializeField] private Vector2 buttonSize = new Vector2(86f, 60f);
    [SerializeField] private Vector2 pauseButtonSize = new Vector2(58f, 60f);
    [SerializeField] private float buttonSpacing = 10f;
    [SerializeField] private int fontSize = 30;

    private DayNightCycleNice2D _cycle;
    private Canvas _canvas;
    private Button _pauseButton;
    private Image _pauseButtonImage;
    private Text _pauseButtonText;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        BuildButtonIfNeeded();
        RebindCycle();
        RefreshVisibility();
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
        RefreshVisibility();
    }

    private void Update()
    {
        if (_cycle == null)
            RebindCycle();

        RefreshPauseButtonVisual();
    }

    private void RebindCycle()
    {
        _cycle = DayNightCycleNice2D.Instance;
        if (_cycle == null)
            _cycle = FindFirstObjectByType<DayNightCycleNice2D>();
    }

    private void BuildButtonIfNeeded()
    {
        if (_canvas != null)
            return;

        Canvas existingCanvas = GetComponentInChildren<Canvas>(true);
        if (existingCanvas != null)
        {
            _canvas = existingCanvas;
            return;
        }

        GameObject canvasGo = new GameObject("NextDayButtonCanvas");
        canvasGo.transform.SetParent(transform, false);

        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 1002;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject buttonGo = new GameObject("NextDayButton");
        buttonGo.transform.SetParent(canvasGo.transform, false);

        RectTransform buttonRt = buttonGo.AddComponent<RectTransform>();
        buttonRt.anchorMin = new Vector2(0f, 0f);
        buttonRt.anchorMax = new Vector2(0f, 0f);
        buttonRt.pivot = new Vector2(0f, 0f);
        buttonRt.anchoredPosition = bottomLeftOffset;
        buttonRt.sizeDelta = buttonSize;

        Image buttonImage = buttonGo.AddComponent<Image>();
        buttonImage.color = new Color(0.08f, 0.45f, 0.16f, 0.9f);

        Button button = buttonGo.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.08f, 0.45f, 0.16f, 0.9f);
        colors.highlightedColor = new Color(0.12f, 0.6f, 0.22f, 0.95f);
        colors.pressedColor = new Color(0.05f, 0.3f, 0.12f, 0.95f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        button.onClick.AddListener(OnNextDayClicked);

        Outline outline = buttonGo.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.65f);
        outline.effectDistance = new Vector2(2f, -2f);

        GameObject textGo = new GameObject("NextDayText");
        textGo.transform.SetParent(buttonGo.transform, false);

        RectTransform textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        Text text = textGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = ">>";

        Shadow shadow = textGo.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
        shadow.effectDistance = new Vector2(1f, -1f);

        GameObject pauseButtonGo = new GameObject("PauseDayButton");
        pauseButtonGo.transform.SetParent(canvasGo.transform, false);

        RectTransform pauseRt = pauseButtonGo.AddComponent<RectTransform>();
        pauseRt.anchorMin = new Vector2(0f, 0f);
        pauseRt.anchorMax = new Vector2(0f, 0f);
        pauseRt.pivot = new Vector2(0f, 0f);
        pauseRt.anchoredPosition = new Vector2(bottomLeftOffset.x + buttonSize.x + buttonSpacing, bottomLeftOffset.y);
        pauseRt.sizeDelta = pauseButtonSize;

        _pauseButtonImage = pauseButtonGo.AddComponent<Image>();
        _pauseButtonImage.color = new Color(0.18f, 0.2f, 0.24f, 0.9f);

        _pauseButton = pauseButtonGo.AddComponent<Button>();
        ColorBlock pauseColors = _pauseButton.colors;
        pauseColors.normalColor = new Color(0.18f, 0.2f, 0.24f, 0.9f);
        pauseColors.highlightedColor = new Color(0.26f, 0.30f, 0.36f, 0.95f);
        pauseColors.pressedColor = new Color(0.12f, 0.14f, 0.17f, 0.95f);
        pauseColors.selectedColor = pauseColors.highlightedColor;
        _pauseButton.colors = pauseColors;
        _pauseButton.onClick.AddListener(OnPauseDayClicked);

        Outline pauseOutline = pauseButtonGo.AddComponent<Outline>();
        pauseOutline.effectColor = new Color(0f, 0f, 0f, 0.65f);
        pauseOutline.effectDistance = new Vector2(2f, -2f);

        GameObject pauseTextGo = new GameObject("PauseDayText");
        pauseTextGo.transform.SetParent(pauseButtonGo.transform, false);

        RectTransform pauseTextRt = pauseTextGo.AddComponent<RectTransform>();
        pauseTextRt.anchorMin = Vector2.zero;
        pauseTextRt.anchorMax = Vector2.one;
        pauseTextRt.offsetMin = Vector2.zero;
        pauseTextRt.offsetMax = Vector2.zero;

        _pauseButtonText = pauseTextGo.AddComponent<Text>();
        _pauseButtonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _pauseButtonText.fontSize = fontSize - 4;
        _pauseButtonText.fontStyle = FontStyle.Bold;
        _pauseButtonText.alignment = TextAnchor.MiddleCenter;
        _pauseButtonText.color = Color.white;
        _pauseButtonText.text = "||";

        Shadow pauseShadow = pauseTextGo.AddComponent<Shadow>();
        pauseShadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
        pauseShadow.effectDistance = new Vector2(1f, -1f);

        RefreshPauseButtonVisual();
    }

    private void OnNextDayClicked()
    {
        if (_cycle == null)
            RebindCycle();

        if (_cycle != null)
            _cycle.AdvanceToNextDay();
    }

    private void OnPauseDayClicked()
    {
        if (_cycle == null)
            RebindCycle();

        if (_cycle == null)
            return;

        _cycle.ToggleManualTimePaused();
        RefreshPauseButtonVisual();
    }

    private void RefreshPauseButtonVisual()
    {
        if (_pauseButtonImage == null || _pauseButtonText == null || _cycle == null)
            return;

        bool paused = _cycle.IsTimeManuallyPaused;

        _pauseButtonImage.color = paused
            ? new Color(0.65f, 0.28f, 0.15f, 0.92f)
            : new Color(0.18f, 0.2f, 0.24f, 0.9f);

        _pauseButtonText.text = paused ? ">" : "||";
    }

    private void RefreshVisibility()
    {
        if (_canvas == null)
            return;

        bool visible = SceneManager.GetActiveScene().name == farmSceneName;
        _canvas.enabled = visible;
    }
}
