using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Styled top-right money HUD that stays visible across scenes.
/// Created once and updated from MoneyManager.
/// </summary>
public class GlobalMoneyHUD : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private Vector2 topRightOffset = new Vector2(-24f, -24f);
    [SerializeField] private Vector2 panelSize = new Vector2(300f, 64f);

    [Header("Style")]
    [SerializeField] private int fontSize = 28;
    [SerializeField] private Color textColor = new Color(0.12f, 0.42f, 0.2f, 1f);
    [SerializeField] private Color panelColor = new Color(0.72f, 0.9f, 1f, 0.9f);
    [SerializeField] private Color iconBgColor = new Color(0.35f, 0.78f, 0.42f, 1f);
    [SerializeField] private Color iconTextColor = new Color(0.22f, 0.12f, 0.02f, 1f);
    [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.45f);

    private Text _moneyText;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        BuildHudIfNeeded();
    }

    private void OnEnable()
    {
        MoneyManager.Instance.OnMoneyChanged += HandleMoneyChanged;
        Refresh();
    }

    private void OnDisable()
    {
        if (MoneyManager.HasInstance)
            MoneyManager.Instance.OnMoneyChanged -= HandleMoneyChanged;
    }

    private void HandleMoneyChanged(int _)
    {
        Refresh();
    }

    private void BuildHudIfNeeded()
    {
        if (_moneyText != null)
            return;

        Canvas canvas = GetComponentInChildren<Canvas>(true);
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("MoneyHUDCanvas");
            canvasGo.transform.SetParent(transform, false);

            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            CanvasGroup group = canvasGo.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        GameObject panelGo = new GameObject("MoneyPanel");
        panelGo.transform.SetParent(canvas.transform, false);

        RectTransform panelRt = panelGo.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(1f, 1f);
        panelRt.anchorMax = new Vector2(1f, 1f);
        panelRt.pivot = new Vector2(1f, 1f);
        panelRt.anchoredPosition = topRightOffset;
        panelRt.sizeDelta = panelSize;

        Image panelImage = panelGo.AddComponent<Image>();
        panelImage.color = panelColor;

        Outline panelOutline = panelGo.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0f, 0f, 0f, 0.5f);
        panelOutline.effectDistance = new Vector2(1.5f, -1.5f);

        HorizontalLayoutGroup layout = panelGo.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 8, 8);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        GameObject iconBgGo = new GameObject("WalletIcon");
        iconBgGo.transform.SetParent(panelGo.transform, false);
        RectTransform iconRt = iconBgGo.AddComponent<RectTransform>();
        iconRt.sizeDelta = new Vector2(42f, 42f);
        LayoutElement iconLayout = iconBgGo.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 42f;
        iconLayout.preferredHeight = 42f;

        Image iconBgImage = iconBgGo.AddComponent<Image>();
        iconBgImage.color = iconBgColor;

        GameObject iconTextGo = new GameObject("WalletIconText");
        iconTextGo.transform.SetParent(iconBgGo.transform, false);
        RectTransform iconTextRt = iconTextGo.AddComponent<RectTransform>();
        iconTextRt.anchorMin = Vector2.zero;
        iconTextRt.anchorMax = Vector2.one;
        iconTextRt.offsetMin = Vector2.zero;
        iconTextRt.offsetMax = Vector2.zero;

        Text iconText = iconTextGo.AddComponent<Text>();
        iconText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        iconText.fontSize = 24;
        iconText.fontStyle = FontStyle.Bold;
        iconText.alignment = TextAnchor.MiddleCenter;
        iconText.color = iconTextColor;
        iconText.text = "G";
        iconText.raycastTarget = false;

        GameObject textGo = new GameObject("MoneyText");
        textGo.transform.SetParent(panelGo.transform, false);

        RectTransform rt = textGo.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(panelSize.x - 84f, 42f);
        LayoutElement textLayout = textGo.AddComponent<LayoutElement>();
        textLayout.preferredWidth = panelSize.x - 84f;
        textLayout.preferredHeight = 42f;

        _moneyText = textGo.AddComponent<Text>();
        _moneyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _moneyText.fontSize = fontSize;
        _moneyText.fontStyle = FontStyle.Bold;
        _moneyText.alignment = TextAnchor.MiddleLeft;
        _moneyText.color = textColor;
        _moneyText.raycastTarget = false;

        Shadow shadow = textGo.AddComponent<Shadow>();
        shadow.effectColor = shadowColor;
        shadow.effectDistance = new Vector2(2f, -2f);
    }

    public void Refresh()
    {
        if (_moneyText == null)
            return;

        _moneyText.text = $"{MoneyManager.Instance.CurrentMoney}";
    }
}