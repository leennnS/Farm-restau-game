using UnityEngine;
using UnityEngine.UIElements;

public class FreezerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private bool showPromptOnEnterRange = true;
    [SerializeField] private string interactionPrompt = "Press E to make ice";

    [Header("Conversion")]
    [SerializeField] private ItemDefinition waterItemDefinition;
    [SerializeField] private ItemDefinition iceItemDefinition;
    [SerializeField] private int waterPerBatch = 1;
    [SerializeField] private int icePerBatch = 10;
    [SerializeField] private float makeIceDurationSeconds = 2.5f;

    [Header("Feedback")]
    [SerializeField] private string notEnoughWaterMessage = "Not enough water";
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
    private bool isMakingIce;
    private float makeIceTimer;

    [Header("Panel UI")]
    [SerializeField] private UIDocument hostUiDocument;

    private VisualElement overlayRoot;
    private VisualElement panelRoot;
    private VisualElement progressFill;
    private Image iceIcon;
    private Label statusText;
    private Label recipeText;
    private Label titleText;
    private Button makeIceButton;
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

    private void TryMakeIce()
    {
        if (waterItemDefinition == null || iceItemDefinition == null)
        {
            Debug.LogWarning("[FreezerInteraction] Water/Ice item references are missing.");
            ShowFeedback("Freezer is not configured");
            return;
        }

        if (waterPerBatch <= 0 || icePerBatch <= 0)
        {
            Debug.LogWarning("[FreezerInteraction] Conversion amounts must be greater than zero.");
            ShowFeedback("Freezer amount config is invalid");
            return;
        }

        int availableWater = playerInventory.CountItemInInventory(waterItemDefinition);
        if (availableWater < waterPerBatch)
        {
            ShowFeedback(notEnoughWaterMessage);
            return;
        }

        bool removedWater = playerInventory.TryRemoveItem(waterItemDefinition, waterPerBatch);
        if (!removedWater)
        {
            ShowFeedback(notEnoughWaterMessage);
            return;
        }

        bool addedIce = playerInventory.TryAdd(iceItemDefinition, icePerBatch);
        if (!addedIce)
        {
            // Roll back water if ice could not be added.
            playerInventory.TryAdd(waterItemDefinition, waterPerBatch);
            ShowFeedback(inventoryFullMessage);
            return;
        }

        string itemName = string.IsNullOrEmpty(iceItemDefinition.displayName)
            ? iceItemDefinition.name
            : iceItemDefinition.displayName;

        ShowFeedback($"+{icePerBatch} {itemName}");
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

        Debug.Log($"[FreezerInteraction] {message}");
    }

    private void OpenPanel()
    {
        BuildPanelIfNeeded();
        if (overlayRoot == null)
        {
            ShowFeedback("Could not open freezer panel");
            return;
        }

        overlayRoot.style.display = DisplayStyle.Flex;
        isPanelOpen = true;
        isMakingIce = false;
        makeIceTimer = 0f;
        RefreshPanelStaticData();
    }

    private void ClosePanel()
    {
        isMakingIce = false;
        makeIceTimer = 0f;
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

        if (!isMakingIce)
            return;

        makeIceTimer += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(makeIceTimer / Mathf.Max(0.1f, makeIceDurationSeconds));

        if (progressFill != null)
            progressFill.style.width = Length.Percent(t * 100f);

        if (statusText != null)
            statusText.text = $"Pending... {(int)(t * 100f)}%";

        if (t >= 1f)
        {
            isMakingIce = false;
            TryMakeIce();
            RefreshPanelStaticData();
        }
    }

    private void StartMakeIceProcess()
    {
        if (isMakingIce)
            return;

        if (waterItemDefinition == null || iceItemDefinition == null)
        {
            ShowFeedback("Freezer is not configured");
            return;
        }

        int availableWater = playerInventory != null ? playerInventory.CountItemInInventory(waterItemDefinition) : 0;
        if (availableWater < waterPerBatch)
        {
            ShowFeedback(notEnoughWaterMessage);
            RefreshPanelStaticData();
            return;
        }

        isMakingIce = true;
        makeIceTimer = 0f;

        if (statusText != null)
            statusText.text = "Pending... 0%";

        if (progressFill != null)
            progressFill.style.width = Length.Percent(0f);

        if (makeIceButton != null)
            makeIceButton.SetEnabled(false);
    }

    private void RefreshPanelStaticData()
    {
        if (!isPanelOpen)
            return;

        if (titleText != null)
            titleText.text = "Freezer";

        if (recipeText != null)
        {
            int waterCount = (playerInventory != null && waterItemDefinition != null)
                ? playerInventory.CountItemInInventory(waterItemDefinition)
                : 0;
            recipeText.text = $"{waterPerBatch} Water -> {icePerBatch} Ice  |  You have: {waterCount} Water";
        }

        if (statusText != null && !isMakingIce)
            statusText.text = "Ready to make ice";

        if (progressFill != null && !isMakingIce)
            progressFill.style.width = Length.Percent(0f);

        if (iceIcon != null)
        {
            Sprite icon = iceItemDefinition != null ? iceItemDefinition.icon : null;
            iceIcon.sprite = icon;
            iceIcon.tintColor = icon != null ? Color.white : new Color(1f, 1f, 1f, 0.25f);
        }

        if (makeIceButton != null)
        {
            int waterCount = (playerInventory != null && waterItemDefinition != null)
                ? playerInventory.CountItemInInventory(waterItemDefinition)
                : 0;
            makeIceButton.SetEnabled(!isMakingIce && waterCount >= waterPerBatch);
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
            name = "FreezerOverlay"
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
            name = "FreezerPanel"
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

        titleText = CreateLabel("FreezerTitle", "Ice Freezer", 44, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.8f, 1f));
        titleText.style.marginBottom = 8;

        Label subtitle = CreateLabel("FreezerSubtitle", "Convert water into ice blocks.", 18, TextAnchor.MiddleCenter, new Color(0.84f, 0.84f, 0.84f, 1f));
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

        iceIcon = new Image { name = "IceIcon" };
        iceIcon.style.width = 132;
        iceIcon.style.height = 132;
        iceIcon.style.marginLeft = 24;
        iceIcon.style.marginRight = 20;
        iceIcon.style.backgroundColor = new Color(0f, 0f, 0f, 0.15f);
        iceIcon.scaleMode = ScaleMode.ScaleToFit;

        recipeText = CreateLabel("FreezerRecipe", string.Empty, 28, TextAnchor.MiddleLeft, new Color(0.92f, 0.96f, 1f, 1f));
        recipeText.style.flexGrow = 1;
        recipeText.style.marginRight = 24;

        recipeRow.Add(iceIcon);
        recipeRow.Add(recipeText);

        statusText = CreateLabel("FreezerStatus", "Ready", 24, TextAnchor.MiddleCenter, new Color(0.92f, 0.95f, 1f, 1f));
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
        progressFill.style.backgroundColor = new Color(0.63f, 0.87f, 1f, 1f);
        progressFill.style.borderTopLeftRadius = 16;
        progressFill.style.borderBottomLeftRadius = 16;

        progressTrack.Add(progressFill);

        makeIceButton = CreateButton("MakeIceButton", "Make Ice", 58, 360);
        makeIceButton.clicked += StartMakeIceProcess;

        closeButton = CreateButton("CloseIceButton", "Close", 50, 280);
        closeButton.clicked += ClosePanel;

        panelRoot.Add(titleText);
        panelRoot.Add(subtitle);
        panelRoot.Add(recipeRow);
        panelRoot.Add(statusText);
        panelRoot.Add(progressTrack);
        panelRoot.Add(makeIceButton);
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
        button.style.backgroundColor = new Color(0.2f, 0.46f, 0.62f, 1f);
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
        if (makeIceButton != null)
            makeIceButton.clicked -= StartMakeIceProcess;

        if (closeButton != null)
            closeButton.clicked -= ClosePanel;

        if (overlayRoot != null)
            overlayRoot.RemoveFromHierarchy();
    }
}
