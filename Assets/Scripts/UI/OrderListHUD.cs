using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class OrderListHUD : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private Vector2 topLeftOffset = new Vector2(24f, -24f);
    [SerializeField] private Vector2 panelSize = new Vector2(380f, 220f);
    [SerializeField] private float spacingBelowClock = 8f;
    [SerializeField] private float fallbackClockHeight = 54f;

    [Header("Style")]
    [SerializeField] private int headerFontSize = 24;
    [SerializeField] private int bodyFontSize = 18;
    [SerializeField] private Color panelColor = new Color(0.08f, 0.13f, 0.18f, 0.8f);
    [SerializeField] private Color textColor = new Color(0.95f, 0.98f, 1f, 1f);

    private Text _ordersText;
    private RectTransform _panelRect;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        BuildHudIfNeeded();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureManagerExists();
        UpdateAnchorPosition();

        if (OrderManager.Instance != null)
        {
            OrderManager.Instance.OnOrdersChanged += HandleOrdersChanged;
            Refresh(OrderManager.Instance.ActiveOrders);
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (OrderManager.Instance != null)
            OrderManager.Instance.OnOrdersChanged -= HandleOrdersChanged;
    }

    private void LateUpdate()
    {
        UpdateAnchorPosition();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateAnchorPosition();
    }

    private void EnsureManagerExists()
    {
        if (OrderManager.Instance != null)
            return;

        OrderManager existing = FindFirstObjectByType<OrderManager>();
        if (existing != null)
            return;

        GameObject managerGo = new GameObject("OrderManager");
        managerGo.AddComponent<OrderManager>();
    }

    private void HandleOrdersChanged(System.Collections.Generic.IReadOnlyList<Order> orders)
    {
        Refresh(orders);
    }

    private void BuildHudIfNeeded()
    {
        if (_ordersText != null)
            return;

        Canvas canvas = GetComponentInChildren<Canvas>(true);
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("OrdersHUDCanvas");
            canvasGo.transform.SetParent(transform, false);

            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1002;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            CanvasGroup group = canvasGo.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        GameObject panelGo = new GameObject("OrdersPanel");
        panelGo.transform.SetParent(canvas.transform, false);

        RectTransform panelRt = panelGo.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0f, 1f);
        panelRt.anchorMax = new Vector2(0f, 1f);
        panelRt.pivot = new Vector2(0f, 1f);
        panelRt.sizeDelta = panelSize;
        _panelRect = panelRt;

        UpdateAnchorPosition();

        Image panelImage = panelGo.AddComponent<Image>();
        panelImage.color = panelColor;

        VerticalLayoutGroup layout = panelGo.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 10, 10);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;

        GameObject textGo = new GameObject("OrdersText");
        textGo.transform.SetParent(panelGo.transform, false);

        RectTransform textRt = textGo.AddComponent<RectTransform>();
        textRt.sizeDelta = panelSize - new Vector2(24f, 20f);

        _ordersText = textGo.AddComponent<Text>();
        _ordersText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _ordersText.fontSize = bodyFontSize;
        _ordersText.alignment = TextAnchor.UpperLeft;
        _ordersText.color = textColor;
        _ordersText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _ordersText.verticalOverflow = VerticalWrapMode.Overflow;
        _ordersText.raycastTarget = false;
    }

    private void UpdateAnchorPosition()
    {
        if (_panelRect == null)
            return;

        Vector2 offset = topLeftOffset;
        float clockHeight = fallbackClockHeight;

        GlobalClockHUD clock = FindFirstObjectByType<GlobalClockHUD>();
        if (clock != null)
        {
            Transform clockPanel = clock.transform.Find("GlobalClockCanvas/ClockPanel");
            if (clockPanel != null)
            {
                RectTransform clockRect = clockPanel as RectTransform;
                if (clockRect != null)
                {
                    offset = clockRect.anchoredPosition;
                    clockHeight = clockRect.rect.height > 0f ? clockRect.rect.height : clockRect.sizeDelta.y;
                }
            }
        }

        _panelRect.anchoredPosition = new Vector2(offset.x, offset.y - clockHeight - spacingBelowClock);
    }

    private void Refresh(System.Collections.Generic.IReadOnlyList<Order> orders)
    {
        if (_ordersText == null)
            return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Orders");

        if (orders == null || orders.Count == 0)
        {
            sb.Append("No active orders");
            _ordersText.fontSize = bodyFontSize;
            _ordersText.text = sb.ToString();
            return;
        }

        for (int i = 0; i < orders.Count; i++)
        {
            Order order = orders[i];
            string name = order.recipe != null ? order.recipe.recipeName : "Unknown";
            sb.Append(i + 1)
              .Append(". ")
              .Append(name)
              .Append(" | ")
              .Append(Mathf.CeilToInt(order.remainingTime))
              .Append("s | +")
              .Append(order.rewardMoney)
              .AppendLine();
        }

        _ordersText.fontSize = bodyFontSize;
        _ordersText.text = sb.ToString();
    }
}
