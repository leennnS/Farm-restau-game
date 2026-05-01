using System;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-900)]
public sealed class GlobalAudioSettingsHUD : MonoBehaviour
{
    private const int SortingOrder = 1002;

    private static GlobalAudioSettingsHUD _instance;

    private readonly Slider[] _sliders = new Slider[3];
    private readonly Text[] _valueLabels = new Text[3];

    private Canvas _canvas;
    private GameObject _settingsButton;
    private GameObject _popup;
    private GameObject _eventSystemObject;
    private float _previousTimeScale = 1f;
    private bool _pausedByPopup;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    private static void EnsureInstance()
    {
        if (_instance != null)
            return;

        GlobalAudioSettingsHUD existing = FindFirstObjectByType<GlobalAudioSettingsHUD>();
        if (existing != null)
        {
            _instance = existing;
            return;
        }

        GameObject hudObject = new GameObject(nameof(GlobalAudioSettingsHUD));
        _instance = hudObject.AddComponent<GlobalAudioSettingsHUD>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        BuildHud();
        ApplySceneVisibility(SceneManager.GetActiveScene().name);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        AudioSettingsManager.Instance.SettingsChanged += SyncFromAudioSettings;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (AudioSettingsManager.HasInstance)
            AudioSettingsManager.Instance.SettingsChanged -= SyncFromAudioSettings;
    }

    private void OnDestroy()
    {
        if (_pausedByPopup)
            Time.timeScale = _previousTimeScale;

        if (_instance == this)
            _instance = null;
    }

    private void Update()
    {
        if (_popup != null && _popup.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            ClosePopup();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySceneVisibility(scene.name);
        SyncFromAudioSettings();
    }

    private void BuildHud()
    {
        if (_canvas != null)
            return;

        GameObject canvasObject = new GameObject("GlobalAudioSettingsCanvas");
        canvasObject.transform.SetParent(transform, false);

        _canvas = canvasObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = SortingOrder;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        _settingsButton = CreateSettingsButton(canvasObject.transform);
        _popup = CreatePopup(canvasObject.transform);
        _popup.SetActive(false);

        EnsureEventSystem();
        SyncFromAudioSettings();
    }

    private GameObject CreateSettingsButton(Transform parent)
    {
        GameObject buttonObject = new GameObject("AudioSettingsButton");
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-24f, -100f);
        rect.sizeDelta = new Vector2(82f, 82f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.52f, 0.29f, 0.13f, 0.76f);

        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.23f, 0.13f, 0.06f, 0.78f);
        outline.effectDistance = new Vector2(2f, -2f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(TogglePopup);

        Text label = CreateText("Icon", buttonObject.transform, "\u266B", 48, FontStyle.Bold, new Color(1f, 0.91f, 0.67f, 1f));
        label.alignment = TextAnchor.MiddleCenter;
        Stretch(label.rectTransform, 0f, 0f, 0f, 0f);

        return buttonObject;
    }

    private GameObject CreatePopup(Transform parent)
    {
        GameObject popupObject = new GameObject("AudioSettingsPopup");
        popupObject.transform.SetParent(parent, false);

        RectTransform rect = popupObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(720f, 520f);

        Image image = popupObject.AddComponent<Image>();
        image.color = new Color(0.98f, 0.86f, 0.58f, 0.88f);

        Outline outline = popupObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.27f, 0.16f, 0.08f, 0.85f);
        outline.effectDistance = new Vector2(3f, -3f);

        Text title = CreateText("Title", popupObject.transform, "Audio", 42, FontStyle.Bold, new Color(0.26f, 0.16f, 0.08f, 1f));
        title.alignment = TextAnchor.MiddleLeft;
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -30f), new Vector2(320f, 58f), new Vector2(0f, 1f));

        Button closeButton = CreateSmallButton("CloseButton", popupObject.transform, "X", new Vector2(1f, 1f), new Vector2(-38f, -32f), new Vector2(58f, 58f));
        closeButton.onClick.AddListener(ClosePopup);

        CreateSliderRow(popupObject.transform, 0, "Master", 130f, OnMasterChanged);
        CreateSliderRow(popupObject.transform, 1, "Music", 220f, OnMusicChanged);
        CreateSliderRow(popupObject.transform, 2, "SFX", 310f, OnSfxChanged);

        Button mainMenuButton = CreatePanelButton("MainMenuButton", popupObject.transform, "Main Menu", new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(300f, 66f));
        mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        return popupObject;
    }

    private void CreateSliderRow(Transform parent, int index, string labelText, float top, UnityEngine.Events.UnityAction<float> callback)
    {
        Text label = CreateText(labelText + "Label", parent, labelText, 28, FontStyle.Bold, new Color(0.28f, 0.19f, 0.1f, 1f));
        label.alignment = TextAnchor.MiddleLeft;
        SetRect(label.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(50f, -top), new Vector2(140f, 48f), new Vector2(0f, 1f));

        Slider slider = CreateSlider(labelText + "Slider", parent);
        SetRect(slider.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(210f, -top - 4f), new Vector2(310f, 52f), new Vector2(0f, 1f));
        slider.onValueChanged.AddListener(callback);

        Text value = CreateText(labelText + "Value", parent, "80", 26, FontStyle.Bold, new Color(0.28f, 0.19f, 0.1f, 1f));
        value.alignment = TextAnchor.MiddleRight;
        SetRect(value.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(546f, -top), new Vector2(62f, 48f), new Vector2(0f, 1f));

        _sliders[index] = slider;
        _valueLabels[index] = value;
    }

    private Slider CreateSlider(string objectName, Transform parent)
    {
        GameObject sliderObject = new GameObject(objectName);
        sliderObject.transform.SetParent(parent, false);

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.wholeNumbers = true;

        GameObject backgroundObject = new GameObject("Background");
        backgroundObject.transform.SetParent(sliderObject.transform, false);
        Image background = backgroundObject.AddComponent<Image>();
        background.color = new Color(0.7f, 0.55f, 0.31f, 0.72f);
        RectTransform backgroundRect = background.rectTransform;
        Stretch(backgroundRect, 0f, 15f, 0f, 15f);

        GameObject fillAreaObject = new GameObject("Fill Area");
        fillAreaObject.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillAreaObject.AddComponent<RectTransform>();
        Stretch(fillAreaRect, 12f, 15f, 12f, 15f);

        GameObject fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(fillAreaObject.transform, false);
        Image fill = fillObject.AddComponent<Image>();
        fill.color = new Color(0.49f, 0.61f, 0.27f, 0.95f);
        RectTransform fillRect = fill.rectTransform;
        Stretch(fillRect, 0f, 0f, 0f, 0f);

        GameObject handleAreaObject = new GameObject("Handle Slide Area");
        handleAreaObject.transform.SetParent(sliderObject.transform, false);
        RectTransform handleAreaRect = handleAreaObject.AddComponent<RectTransform>();
        Stretch(handleAreaRect, 12f, 0f, 12f, 0f);

        GameObject handleObject = new GameObject("Handle");
        handleObject.transform.SetParent(handleAreaObject.transform, false);
        Image handle = handleObject.AddComponent<Image>();
        handle.color = new Color(0.9f, 0.72f, 0.42f, 1f);
        RectTransform handleRect = handle.rectTransform;
        handleRect.sizeDelta = new Vector2(34f, 44f);

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle;

        return slider;
    }

    private Button CreateSmallButton(string objectName, Transform parent, string text, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        SetRect(rect, anchor, anchor, position, size, new Vector2(1f, 1f));

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.55f, 0.12f, 0.08f, 0.94f);

        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.24f, 0.05f, 0.03f, 0.95f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        Text label = CreateText("Label", buttonObject.transform, text, 28, FontStyle.Bold, Color.white);
        label.alignment = TextAnchor.MiddleCenter;
        Stretch(label.rectTransform, 0f, 0f, 0f, 0f);

        return button;
    }

    private Button CreatePanelButton(string objectName, Transform parent, string text, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        SetRect(rect, anchor, anchor, position, size, new Vector2(0.5f, 0f));

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.5f, 0.31f, 0.15f, 0.92f);

        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.22f, 0.12f, 0.05f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        Text label = CreateText("Label", buttonObject.transform, text, 24, FontStyle.Bold, new Color(1f, 0.91f, 0.68f, 1f));
        label.alignment = TextAnchor.MiddleCenter;
        Stretch(label.rectTransform, 0f, 0f, 0f, 0f);

        return button;
    }

    private Text CreateText(string objectName, Transform parent, string text, int fontSize, FontStyle style, Color color)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        Text label = textObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, Vector2 pivot)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private void TogglePopup()
    {
        if (_popup == null)
            return;

        if (_popup.activeSelf)
            ClosePopup();
        else
            OpenPopup();
    }

    private void OpenPopup()
    {
        if (_popup == null)
            return;

        SyncFromAudioSettings();
        _popup.SetActive(true);

        if (!_pausedByPopup)
        {
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            _pausedByPopup = true;
        }
    }

    private void ClosePopup()
    {
        if (_popup != null)
            _popup.SetActive(false);

        if (_pausedByPopup)
        {
            Time.timeScale = _previousTimeScale;
            _pausedByPopup = false;
        }
    }

    private void ApplySceneVisibility(string sceneName)
    {
        bool hidden = IsExcludedScene(sceneName);
        if (hidden)
        {
            ClosePopup();
            DestroyFallbackEventSystem();
        }

        if (_canvas != null)
            _canvas.gameObject.SetActive(!hidden);

        if (!hidden)
            EnsureEventSystem();
    }

    private void EnsureEventSystem()
    {
        EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        bool sceneEventSystemExists = false;

        for (int i = 0; i < eventSystems.Length; i++)
        {
            EventSystem system = eventSystems[i];
            if (system == null)
                continue;

            if (system.gameObject != _eventSystemObject)
            {
                sceneEventSystemExists = true;
                break;
            }
        }

        if (sceneEventSystemExists)
        {
            DestroyFallbackEventSystem();
            return;
        }

        EventSystem existing = EventSystem.current != null ? EventSystem.current : FindFirstObjectByType<EventSystem>();
        if (existing != null)
            return;

        if (_eventSystemObject == null)
        {
            _eventSystemObject = new GameObject("GlobalAudioSettingsEventSystem");
            DontDestroyOnLoad(_eventSystemObject);
        }

        if (_eventSystemObject.GetComponent<EventSystem>() == null)
            _eventSystemObject.AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
        if (_eventSystemObject.GetComponent<InputSystemUIInputModule>() == null)
            _eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
        if (_eventSystemObject.GetComponent<StandaloneInputModule>() == null)
            _eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
    }

    private void DestroyFallbackEventSystem()
    {
        if (_eventSystemObject == null)
            return;

        Destroy(_eventSystemObject);
        _eventSystemObject = null;
    }

    private static bool IsExcludedScene(string sceneName)
    {
        return string.Equals(sceneName, "Intro", StringComparison.OrdinalIgnoreCase)
            || string.Equals(sceneName, "MAIN MENU", StringComparison.OrdinalIgnoreCase)
            || string.Equals(sceneName, "MainMenu", StringComparison.OrdinalIgnoreCase);
    }

    private void SyncFromAudioSettings()
    {
        AudioSettingsManager audioSettings = AudioSettingsManager.Instance;
        SetSliderValue(0, audioSettings.MasterVolumeNormalized * 100f);
        SetSliderValue(1, audioSettings.MusicVolumeNormalized * 100f);
        SetSliderValue(2, audioSettings.SfxVolumeNormalized * 100f);
    }

    private void SetSliderValue(int index, float value)
    {
        int rounded = Mathf.RoundToInt(value);

        if (_sliders[index] != null)
            _sliders[index].SetValueWithoutNotify(rounded);

        if (_valueLabels[index] != null)
            _valueLabels[index].text = rounded.ToString();
    }

    private void OnMasterChanged(float value)
    {
        SetSliderValue(0, value);
        AudioSettingsManager.Instance.SetMasterVolumeNormalized(value / 100f);
    }

    private void OnMusicChanged(float value)
    {
        SetSliderValue(1, value);
        AudioSettingsManager.Instance.SetMusicVolumeNormalized(value / 100f);
    }

    private void OnSfxChanged(float value)
    {
        SetSliderValue(2, value);
        AudioSettingsManager.Instance.SetSfxVolumeNormalized(value / 100f);
    }

    private void ReturnToMainMenu()
    {
        if (_pausedByPopup)
        {
            Time.timeScale = _previousTimeScale;
            _pausedByPopup = false;
        }

        Time.timeScale = 1f;
        ClosePopup();
        CleanupGameplaySystemsForMainMenu();
        SceneManager.LoadScene("MAIN MENU", LoadSceneMode.Single);
    }

    private void CleanupGameplaySystemsForMainMenu()
    {
        DestroyPersistentObjects<InventoryController>();
        DestroyPersistentObjects<HotBarController>();
        DestroyPersistentObjects<HotBarHUDController>();
        DestroyPersistentObjects<MoneyManager>();
        DestroyPersistentObjects<GameManager>();
        DestroyPersistentObjects<DayNightCycleNice2D>();
        DestroyPersistentObjects<GlobalMoneyHUD>();
        DestroyPersistentObjects<GlobalClockHUD>();
        DestroyPersistentObjects<GlobalNextDayButtonHUD>();
        DestroyPersistentObjects<ClockHUDController>();
        DestroyPersistentObjects<OrderListHUD>();
        DestroyPersistentObjects<LanternController>();
        DestroyPersistentObjects<ImprovedLanternController>();
        DestroyPersistentObjects<RestaurantCookingUIController>();
        DestroyPersistentObjects<CameraFollowFix>();
        DestroyFallbackEventSystem();
    }

    private static void DestroyPersistentObjects<T>() where T : MonoBehaviour
    {
        T[] instances = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (instances == null)
            return;

        for (int i = 0; i < instances.Length; i++)
        {
            T instance = instances[i];
            if (instance != null)
                Destroy(instance.gameObject);
        }
    }
}
