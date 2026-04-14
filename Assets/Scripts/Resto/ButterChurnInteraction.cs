using UnityEngine;
using UnityEngine.UIElements;

public class ButterChurnInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private bool showPromptOnEnterRange = true;
    [SerializeField] private string interactionPrompt = "Press E to churn butter";

    [Header("Conversion")]
    [SerializeField] private ItemDefinition milkItemDefinition;
    [SerializeField] private ItemDefinition butterItemDefinition;
    [SerializeField] private int milkPerBatch = 2;
    [SerializeField] private int butterPerBatch = 5;
    [SerializeField] private float churnDurationSeconds = 2.5f;

    [Header("Feedback")]
    [SerializeField] private string notEnoughMilkMessage = "Not enough milk";
    [SerializeField] private string inventoryFullMessage = "Not enough inventory space";

    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private InventoryController playerInventory;
    [SerializeField] private PickupToastUIToolkit pickupToast;
    [SerializeField] private Collider2D interactionCollider;

    private bool wasInRangeLastFrame;
    private string fallbackMessage;
    private float fallbackMessageUntil;

    private bool isPanelOpen;
    private bool isChurning;
    private float churnTimer;

    [Header("Panel UI")]
    [SerializeField] private UIDocument hostUiDocument;

    private VisualElement overlayRoot;
    private VisualElement panelRoot;
    private VisualElement progressFill;
    private Image butterIcon;
    private Label statusText;
    private Label recipeText;
    private Label titleText;
    private Button churnButton;
    private Button closeButton;

    private void Start()
    {
        ResolveReferences();
    }

    private void Update()
    {
        ResolveReferences();

        if (playerTransform == null || playerInventory == null)
            return;

        float distance = GetPlayerDistance();
        bool inRange = distance <= interactionDistance;

        if (showPromptOnEnterRange && inRange && !wasInRangeLastFrame)
            ShowFeedback(interactionPrompt);

        wasInRangeLastFrame = inRange;

        if (!inRange && isPanelOpen)
            ClosePanel();

        if (isPanelOpen)
        {
            UpdatePanelRuntime();
            return;
        }

        if (!inRange)
            return;

        if (Input.GetKeyDown(interactionKey))
            OpenPanel();
    }

    private float GetPlayerDistance()
    {
        if (playerTransform == null)
            return float.MaxValue;

        Vector2 playerPos = playerTransform.position;

        if (interactionCollider != null)
        {
            Vector2 closest = interactionCollider.ClosestPoint(playerPos);
            return Vector2.Distance(playerPos, closest);
        }

        return Vector2.Distance(transform.position, playerPos);
    }

    private void TryMakeButter()
    {
        if (milkItemDefinition == null || butterItemDefinition == null)
        {
            Debug.LogWarning("[ButterChurnInteraction] Milk/Butter item references are missing.");
            ShowFeedback("Butter churn is not configured");
            return;
        }

        if (milkPerBatch <= 0 || butterPerBatch <= 0)
        {
            Debug.LogWarning("[ButterChurnInteraction] Conversion amounts must be greater than zero.");
            ShowFeedback("Butter churn config is invalid");
            return;
        }

        int availableMilk = playerInventory.CountItemInInventory(milkItemDefinition);
        if (availableMilk < milkPerBatch)
        {
            ShowFeedback(notEnoughMilkMessage);
            return;
        }

        bool removedMilk = playerInventory.TryRemoveItem(milkItemDefinition, milkPerBatch);
        if (!removedMilk)
        {
            ShowFeedback(notEnoughMilkMessage);
            return;
        }

        bool addedButter = playerInventory.TryAdd(butterItemDefinition, butterPerBatch);
        if (!addedButter)
        {
            playerInventory.TryAdd(milkItemDefinition, milkPerBatch);
            ShowFeedback(inventoryFullMessage);
            return;
        }

        string itemName = string.IsNullOrEmpty(butterItemDefinition.displayName)
            ? butterItemDefinition.name
            : butterItemDefinition.displayName;

        ShowFeedback($"+{butterPerBatch} {itemName}");
        RefreshPanelStaticData();
    }

    private void ResolveReferences()
    {
        if (pickupToast == null)
            pickupToast = FindFirstObjectByType<PickupToastUIToolkit>();

        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<InventoryController>();

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }

        if (interactionCollider == null)
            interactionCollider = GetComponent<Collider2D>();
    }

    private void ShowFeedback(string message)
    {
        if (pickupToast != null)
            pickupToast.Show(message);
        else
            ShowFallbackMessage(message);

        Debug.Log($"[ButterChurnInteraction] {message}");
    }

    private void OpenPanel()
    {
        BuildPanelIfNeeded();
        if (overlayRoot == null)
        {
            ShowFeedback("Could not open churn panel");
            return;
        }

        overlayRoot.style.display = DisplayStyle.Flex;
        isPanelOpen = true;
        isChurning = false;
        churnTimer = 0f;
        RefreshPanelStaticData();
    }

    private void ClosePanel()
    {
        isChurning = false;
        churnTimer = 0f;
        isPanelOpen = false;

        if (overlayRoot != null)
            overlayRoot.style.display = DisplayStyle.None;
    }

    private void UpdatePanelRuntime()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(interactionKey))
        {
            ClosePanel();
            return;
        }

        if (!isChurning)
            return;

        churnTimer += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(churnTimer / Mathf.Max(0.1f, churnDurationSeconds));

        if (progressFill != null)
            progressFill.style.width = Length.Percent(t * 100f);

        if (statusText != null)
            statusText.text = $"Pending... {(int)(t * 100f)}%";

        if (t >= 1f)
        {
            isChurning = false;
            TryMakeButter();
            RefreshPanelStaticData();
        }
    }

    private void StartChurnProcess()
    {
        if (isChurning)
            return;

        if (milkItemDefinition == null || butterItemDefinition == null)
        {
            ShowFeedback("Butter churn is not configured");
            return;
        }

        int availableMilk = playerInventory != null ? playerInventory.CountItemInInventory(milkItemDefinition) : 0;
        if (availableMilk < milkPerBatch)
        {
            ShowFeedback(notEnoughMilkMessage);
            RefreshPanelStaticData();
            return;
        }

        isChurning = true;
        churnTimer = 0f;

        if (statusText != null)
            statusText.text = "Pending... 0%";

        if (progressFill != null)
            progressFill.style.width = Length.Percent(0f);

        if (churnButton != null)
            churnButton.SetEnabled(false);
    }

    private void RefreshPanelStaticData()
    {
        if (!isPanelOpen)
            return;

        if (titleText != null)
            titleText.text = "Butter Churn";

        if (recipeText != null)
        {
            int milkCount = (playerInventory != null && milkItemDefinition != null)
                ? playerInventory.CountItemInInventory(milkItemDefinition)
                : 0;
            recipeText.text = $"{milkPerBatch} Milk -> {butterPerBatch} Butter  |  You have: {milkCount} Milk";
        }

        if (statusText != null && !isChurning)
            statusText.text = "Ready to churn";

        if (progressFill != null && !isChurning)
            progressFill.style.width = Length.Percent(0f);

        if (butterIcon != null)
        {
            Sprite icon = butterItemDefinition != null ? butterItemDefinition.icon : null;
            butterIcon.sprite = icon;
            butterIcon.tintColor = icon != null ? Color.white : new Color(1f, 1f, 1f, 0.25f);
        }

        if (churnButton != null)
        {
            int milkCount = (playerInventory != null && milkItemDefinition != null)
                ? playerInventory.CountItemInInventory(milkItemDefinition)
                : 0;
            churnButton.SetEnabled(!isChurning && milkCount >= milkPerBatch);
        }
    }

    private void BuildPanelIfNeeded()
    {
        if (overlayRoot != null)
            return;

        ResolveHostDocument();
        if (hostUiDocument == null)
            return;

        VisualElement hostRoot = hostUiDocument.rootVisualElement;
        if (hostRoot == null)
            return;

        overlayRoot = new VisualElement
        {
            name = "ButterChurnOverlay"
        };
        overlayRoot.style.position = Position.Absolute;
        overlayRoot.style.left = 0;
        overlayRoot.style.top = 0;
        overlayRoot.style.right = 0;
        overlayRoot.style.bottom = 0;
        overlayRoot.style.backgroundColor = new Color(0f, 0f, 0f, 0.6f);
        overlayRoot.style.justifyContent = Justify.Center;
        overlayRoot.style.alignItems = Align.Center;
        overlayRoot.style.display = DisplayStyle.None;
        overlayRoot.pickingMode = PickingMode.Position;

        panelRoot = new VisualElement
        {
            name = "ButterChurnPanel"
        };
        panelRoot.style.width = 920;
        panelRoot.style.minHeight = 700;
        panelRoot.style.paddingTop = 30;
        panelRoot.style.paddingBottom = 30;
        panelRoot.style.paddingLeft = 40;
        panelRoot.style.paddingRight = 40;
        panelRoot.style.backgroundColor = new Color(0.12f, 0.09f, 0.07f, 0.97f);
        panelRoot.style.borderTopLeftRadius = 24;
        panelRoot.style.borderTopRightRadius = 24;
        panelRoot.style.borderBottomLeftRadius = 24;
        panelRoot.style.borderBottomRightRadius = 24;
        panelRoot.style.borderLeftWidth = 3;
        panelRoot.style.borderRightWidth = 3;
        panelRoot.style.borderTopWidth = 3;
        panelRoot.style.borderBottomWidth = 3;
        panelRoot.style.borderLeftColor = new Color(0.47f, 0.35f, 0.2f, 1f);
        panelRoot.style.borderRightColor = new Color(0.47f, 0.35f, 0.2f, 1f);
        panelRoot.style.borderTopColor = new Color(0.47f, 0.35f, 0.2f, 1f);
        panelRoot.style.borderBottomColor = new Color(0.47f, 0.35f, 0.2f, 1f);

        titleText = CreateLabel("ButterChurnTitle", "Dairy Churn", 44, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.8f, 1f));
        titleText.style.marginBottom = 8;

        Label subtitle = CreateLabel("ButterChurnSubtitle", "Convert milk into butter blocks.", 18, TextAnchor.MiddleCenter, new Color(0.84f, 0.84f, 0.84f, 1f));
        subtitle.style.marginBottom = 24;

        VisualElement recipeRow = new VisualElement();
        recipeRow.style.flexDirection = FlexDirection.Row;
        recipeRow.style.alignItems = Align.Center;
        recipeRow.style.height = 180;
        recipeRow.style.marginBottom = 22;
        recipeRow.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.18f);
        recipeRow.style.borderTopLeftRadius = 20;
        recipeRow.style.borderTopRightRadius = 20;
        recipeRow.style.borderBottomLeftRadius = 20;
        recipeRow.style.borderBottomRightRadius = 20;

        butterIcon = new Image { name = "ButterIcon" };
        butterIcon.style.width = 132;
        butterIcon.style.height = 132;
        butterIcon.style.marginLeft = 24;
        butterIcon.style.marginRight = 20;
        butterIcon.style.backgroundColor = new Color(0f, 0f, 0f, 0.15f);
        butterIcon.scaleMode = ScaleMode.ScaleToFit;

        recipeText = CreateLabel("ButterChurnRecipe", string.Empty, 28, TextAnchor.MiddleLeft, new Color(0.98f, 0.95f, 0.83f, 1f));
        recipeText.style.flexGrow = 1;
        recipeText.style.marginRight = 24;

        recipeRow.Add(butterIcon);
        recipeRow.Add(recipeText);

        statusText = CreateLabel("ButterChurnStatus", "Ready", 24, TextAnchor.MiddleCenter, new Color(0.98f, 0.95f, 0.83f, 1f));
        statusText.style.marginBottom = 14;

        VisualElement progressTrack = new VisualElement();
        progressTrack.style.width = Length.Percent(100f);
        progressTrack.style.height = 42;
        progressTrack.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        progressTrack.style.borderTopLeftRadius = 16;
        progressTrack.style.borderTopRightRadius = 16;
        progressTrack.style.borderBottomLeftRadius = 16;
        progressTrack.style.borderBottomRightRadius = 16;
        progressTrack.style.marginBottom = 28;

        progressFill = new VisualElement();
        progressFill.style.height = Length.Percent(100f);
        progressFill.style.width = Length.Percent(0f);
        progressFill.style.backgroundColor = new Color(0.95f, 0.8f, 0.3f, 1f);
        progressFill.style.borderTopLeftRadius = 16;
        progressFill.style.borderBottomLeftRadius = 16;

        progressTrack.Add(progressFill);

        churnButton = CreateButton("ChurnButterButton", "Churn Butter", 58, 360);
        churnButton.clicked += StartChurnProcess;

        closeButton = CreateButton("CloseButterChurnButton", "Close", 50, 280);
        closeButton.clicked += ClosePanel;

        panelRoot.Add(titleText);
        panelRoot.Add(subtitle);
        panelRoot.Add(recipeRow);
        panelRoot.Add(statusText);
        panelRoot.Add(progressTrack);
        panelRoot.Add(churnButton);
        panelRoot.Add(closeButton);

        overlayRoot.Add(panelRoot);
        hostRoot.Add(overlayRoot);
    }

    private void ResolveHostDocument()
    {
        if (hostUiDocument != null)
            return;

        UIDocument[] docs = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
        for (int i = 0; i < docs.Length; i++)
        {
            UIDocument doc = docs[i];
            if (doc == null || !doc.isActiveAndEnabled || doc.rootVisualElement == null)
                continue;

            hostUiDocument = doc;
            break;
        }
    }

    private Label CreateLabel(string name, string text, int fontSize, TextAnchor alignment, Color color)
    {
        Label label = new Label(text) { name = name };
        label.style.fontSize = fontSize;
        label.style.unityTextAlign = alignment;
        label.style.color = color;
        return label;
    }

    private Button CreateButton(string name, string text, int height, int width)
    {
        Button button = new Button { name = name, text = text };
        button.style.height = height;
        button.style.width = width;
        button.style.alignSelf = Align.Center;
        button.style.fontSize = 24;
        button.style.marginBottom = 14;
        button.style.backgroundColor = new Color(0.65f, 0.47f, 0.16f, 1f);
        button.style.color = Color.white;
        return button;
    }

    private void ShowFallbackMessage(string message)
    {
        fallbackMessage = message;
        fallbackMessageUntil = Time.time + 1.8f;
    }

    private void OnGUI()
    {
        if (string.IsNullOrEmpty(fallbackMessage) || Time.time > fallbackMessageUntil)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };

        Rect rect = new Rect((Screen.width * 0.5f) - 220f, Screen.height - 120f, 440f, 40f);
        GUI.Box(rect, fallbackMessage, style);
    }

    private void OnDestroy()
    {
        if (churnButton != null)
            churnButton.clicked -= StartChurnProcess;

        if (closeButton != null)
            closeButton.clicked -= ClosePanel;

        if (overlayRoot != null)
            overlayRoot.RemoveFromHierarchy();
    }
}