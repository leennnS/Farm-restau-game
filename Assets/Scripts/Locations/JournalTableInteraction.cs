using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// House journal interaction.
/// Attach to a table or desk with a trigger collider.
/// Shows a proximity prompt, opens a writable journal panel, and persists the text via PlayerPrefs.
/// </summary>
public class JournalTableInteraction : MonoBehaviour
{
    private const string DefaultSaveKey = "JournalText";

    private static JournalTableInteraction _instance;

    [Header("Interaction")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string interactionPrompt = "Press E to open journal";
    [SerializeField] private PickupToastUIToolkit pickupToast;

    [Header("UI")]
    [SerializeField] private UIDocument hostUiDocument;
    [SerializeField] private string journalTitle = "Journal";
    [SerializeField] private string journalSubtitle = "Write your thoughts and notes. Saved automatically.";
    [SerializeField] private string saveKey = DefaultSaveKey;

    private bool playerInRange;
    private bool panelOpen;
    private bool isLoadingJournal;

    private VisualElement overlayRoot;
    private VisualElement panelRoot;
    private TextField journalField;
    private Label saveStatusLabel;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private void Start()
    {
        ResolveReferences();
    }

    private void Update()
    {
        ResolveReferences();

        if (!panelOpen)
        {
            if (playerInRange && Input.GetKeyDown(interactionKey))
                OpenJournal();

            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
            CloseJournal();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerInRange = true;
        ShowPrompt();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerInRange = false;
        HidePrompt();

        if (panelOpen)
            CloseJournal();
    }

    private void OnDisable()
    {
        if (panelOpen)
            SaveJournalText();

        HidePrompt();
        SetPanelVisible(false);
    }

    private void OnApplicationQuit()
    {
        if (panelOpen)
            SaveJournalText();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && panelOpen)
            SaveJournalText();
    }

    private void ResolveReferences()
    {
        if (pickupToast == null)
        {
            pickupToast = FindFirstObjectByType<PickupToastUIToolkit>();
            if (pickupToast == null)
                TryBootstrapPickupToast();
        }
    }

    private void TryBootstrapPickupToast()
    {
        UIDocument sourceDoc = hostUiDocument;
        if (sourceDoc == null)
        {
            UIDocument[] docs = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
            for (int i = 0; i < docs.Length; i++)
            {
                if (docs[i] != null && docs[i].panelSettings != null)
                {
                    sourceDoc = docs[i];
                    break;
                }
            }
        }

        if (sourceDoc == null || sourceDoc.panelSettings == null)
            return;

        GameObject toastGo = new GameObject("RuntimeJournalToastUI");
        UIDocument doc = toastGo.AddComponent<UIDocument>();
        doc.panelSettings = sourceDoc.panelSettings;
        doc.sortingOrder = sourceDoc.sortingOrder + 10;

        pickupToast = toastGo.AddComponent<PickupToastUIToolkit>();
    }

    private void ShowPrompt()
    {
        if (pickupToast != null)
            pickupToast.ShowPersistent(interactionPrompt, 24);
    }

    private void HidePrompt()
    {
        if (pickupToast != null)
            pickupToast.Hide();
    }

    private void OpenJournal()
    {
        BuildPanelIfNeeded();

        if (overlayRoot == null)
        {
            if (pickupToast != null)
                pickupToast.Show("Could not open journal");

            return;
        }

        panelOpen = true;
        SetPanelVisible(true);

        if (journalField != null)
        {
            isLoadingJournal = true;
            journalField.value = LoadJournalText();
            isLoadingJournal = false;
            journalField.Focus();
        }

        if (saveStatusLabel != null)
            saveStatusLabel.text = "Loaded";

        HidePrompt();
    }

    private void CloseJournal()
    {
        if (!panelOpen)
            return;

        SaveJournalText();
        panelOpen = false;
        SetPanelVisible(false);

        if (playerInRange)
            ShowPrompt();
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

        overlayRoot = new VisualElement { name = "JournalOverlay" };
        overlayRoot.style.position = Position.Absolute;
        overlayRoot.style.left = 0;
        overlayRoot.style.top = 0;
        overlayRoot.style.right = 0;
        overlayRoot.style.bottom = 0;
        overlayRoot.style.backgroundColor = new Color(0.05f, 0.04f, 0.03f, 0.48f);
        overlayRoot.style.justifyContent = Justify.Center;
        overlayRoot.style.alignItems = Align.Center;
        overlayRoot.style.display = DisplayStyle.None;
        overlayRoot.pickingMode = PickingMode.Position;

        panelRoot = new VisualElement { name = "JournalPanel" };
        panelRoot.style.width = 760;
        panelRoot.style.maxWidth = Length.Percent(92);
        panelRoot.style.maxHeight = Length.Percent(84);
        panelRoot.style.flexDirection = FlexDirection.Column;
        panelRoot.style.paddingLeft = 28;
        panelRoot.style.paddingRight = 28;
        panelRoot.style.paddingTop = 24;
        panelRoot.style.paddingBottom = 22;
        panelRoot.style.backgroundColor = new Color(0.96f, 0.92f, 0.83f, 0.98f);
        panelRoot.style.borderTopLeftRadius = 12;
        panelRoot.style.borderTopRightRadius = 10;
        panelRoot.style.borderBottomLeftRadius = 14;
        panelRoot.style.borderBottomRightRadius = 12;
        panelRoot.style.borderLeftWidth = 2;
        panelRoot.style.borderRightWidth = 2;
        panelRoot.style.borderTopWidth = 2;
        panelRoot.style.borderBottomWidth = 2;
        panelRoot.style.borderLeftColor = new Color(0.55f, 0.43f, 0.29f, 0.65f);
        panelRoot.style.borderRightColor = new Color(0.55f, 0.43f, 0.29f, 0.65f);
        panelRoot.style.borderTopColor = new Color(0.62f, 0.50f, 0.35f, 0.55f);
        panelRoot.style.borderBottomColor = new Color(0.45f, 0.35f, 0.23f, 0.75f);

        VisualElement topRow = new VisualElement();
        topRow.style.flexDirection = FlexDirection.Row;
        topRow.style.justifyContent = Justify.SpaceBetween;
        topRow.style.alignItems = Align.Center;
        topRow.style.marginBottom = 8;
        panelRoot.Add(topRow);

        VisualElement titleGroup = new VisualElement();
        titleGroup.style.flexDirection = FlexDirection.Column;
        titleGroup.style.flexGrow = 1;
        topRow.Add(titleGroup);

        Label titleLabel = new Label(journalTitle);
        titleLabel.style.fontSize = 30;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.color = new Color(0.28f, 0.18f, 0.09f, 1f);
        titleLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
        titleGroup.Add(titleLabel);

        Label subtitleLabel = new Label(journalSubtitle);
        subtitleLabel.style.fontSize = 14;
        subtitleLabel.style.color = new Color(0.43f, 0.31f, 0.19f, 0.9f);
        subtitleLabel.style.marginTop = 4;
        titleGroup.Add(subtitleLabel);

        Button closeButton = new Button(CloseJournal);
        closeButton.text = "Close";
        closeButton.style.width = 96;
        closeButton.style.height = 34;
        closeButton.style.marginLeft = 12;
        closeButton.style.backgroundColor = new Color(0.56f, 0.42f, 0.28f, 0.95f);
        closeButton.style.color = new Color(0.98f, 0.95f, 0.89f, 1f);
        closeButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        closeButton.style.borderTopLeftRadius = 8;
        closeButton.style.borderTopRightRadius = 8;
        closeButton.style.borderBottomLeftRadius = 8;
        closeButton.style.borderBottomRightRadius = 8;
        topRow.Add(closeButton);

        VisualElement divider = new VisualElement();
        divider.style.height = 2;
        divider.style.marginTop = 10;
        divider.style.marginBottom = 14;
        divider.style.backgroundColor = new Color(0.52f, 0.39f, 0.24f, 0.20f);
        panelRoot.Add(divider);

        VisualElement paperFrame = new VisualElement();
        paperFrame.style.flexGrow = 1;
        paperFrame.style.paddingLeft = 18;
        paperFrame.style.paddingRight = 18;
        paperFrame.style.paddingTop = 18;
        paperFrame.style.paddingBottom = 18;
        paperFrame.style.backgroundColor = new Color(1f, 0.98f, 0.94f, 0.85f);
        paperFrame.style.borderTopLeftRadius = 10;
        paperFrame.style.borderTopRightRadius = 10;
        paperFrame.style.borderBottomLeftRadius = 10;
        paperFrame.style.borderBottomRightRadius = 10;
        paperFrame.style.borderLeftWidth = 1;
        paperFrame.style.borderRightWidth = 1;
        paperFrame.style.borderTopWidth = 1;
        paperFrame.style.borderBottomWidth = 1;
        paperFrame.style.borderLeftColor = new Color(0.66f, 0.55f, 0.41f, 0.35f);
        paperFrame.style.borderRightColor = new Color(0.66f, 0.55f, 0.41f, 0.35f);
        paperFrame.style.borderTopColor = new Color(0.72f, 0.61f, 0.47f, 0.30f);
        paperFrame.style.borderBottomColor = new Color(0.58f, 0.47f, 0.33f, 0.38f);
        panelRoot.Add(paperFrame);

        Label fieldLabel = new Label("Entry");
        fieldLabel.style.fontSize = 15;
        fieldLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        fieldLabel.style.color = new Color(0.36f, 0.24f, 0.14f, 0.96f);
        fieldLabel.style.marginBottom = 10;
        paperFrame.Add(fieldLabel);

        journalField = new TextField();
        journalField.multiline = true;
        journalField.style.flexGrow = 1;
        journalField.style.minHeight = 320;
        journalField.style.unityTextAlign = TextAnchor.UpperLeft;
        journalField.style.whiteSpace = WhiteSpace.Normal;
        journalField.style.fontSize = 18;
        journalField.style.color = new Color(0.22f, 0.16f, 0.10f, 1f);
        journalField.style.backgroundColor = new Color(1f, 0.99f, 0.95f, 0.98f);
        journalField.style.borderLeftWidth = 1;
        journalField.style.borderRightWidth = 1;
        journalField.style.borderTopWidth = 1;
        journalField.style.borderBottomWidth = 1;
        journalField.style.borderLeftColor = new Color(0.67f, 0.58f, 0.47f, 0.46f);
        journalField.style.borderRightColor = new Color(0.67f, 0.58f, 0.47f, 0.46f);
        journalField.style.borderTopColor = new Color(0.72f, 0.64f, 0.52f, 0.36f);
        journalField.style.borderBottomColor = new Color(0.56f, 0.47f, 0.36f, 0.48f);
        journalField.style.borderTopLeftRadius = 8;
        journalField.style.borderTopRightRadius = 8;
        journalField.style.borderBottomLeftRadius = 8;
        journalField.style.borderBottomRightRadius = 8;
        journalField.style.paddingLeft = 12;
        journalField.style.paddingRight = 12;
        journalField.style.paddingTop = 10;
        journalField.style.paddingBottom = 10;
        journalField.style.marginBottom = 14;
        journalField.style.unityFontStyleAndWeight = FontStyle.Normal;
        journalField.RegisterValueChangedCallback(_ =>
        {
            if (!isLoadingJournal)
                UpdateSaveStatus("Unsaved changes");
        });
        paperFrame.Add(journalField);

        VisualElement footerRow = new VisualElement();
        footerRow.style.flexDirection = FlexDirection.Row;
        footerRow.style.justifyContent = Justify.SpaceBetween;
        footerRow.style.alignItems = Align.Center;
        paperFrame.Add(footerRow);

        Label hintLabel = new Label("Esc to close");
        hintLabel.style.fontSize = 13;
        hintLabel.style.color = new Color(0.47f, 0.34f, 0.22f, 0.9f);
        footerRow.Add(hintLabel);

        saveStatusLabel = new Label("Ready");
        saveStatusLabel.style.fontSize = 13;
        saveStatusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        saveStatusLabel.style.color = new Color(0.38f, 0.27f, 0.17f, 0.95f);
        footerRow.Add(saveStatusLabel);

        overlayRoot.Add(panelRoot);
        hostRoot.Add(overlayRoot);
    }

    private void SetPanelVisible(bool visible)
    {
        if (overlayRoot != null)
            overlayRoot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void SaveJournalText()
    {
        if (journalField == null)
            return;

        string text = journalField.value ?? string.Empty;
        PlayerPrefs.SetString(saveKey, text);
        PlayerPrefs.Save();

        UpdateSaveStatus("Saved");
    }

    private string LoadJournalText()
    {
        return PlayerPrefs.GetString(saveKey, string.Empty);
    }

    private void UpdateSaveStatus(string status)
    {
        if (saveStatusLabel != null)
            saveStatusLabel.text = status;
    }

    private void ResolveHostDocument()
    {
        if (hostUiDocument != null)
            return;

        UIDocument[] docs = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
        for (int i = 0; i < docs.Length; i++)
        {
            UIDocument doc = docs[i];
            if (doc == null || !doc.isActiveAndEnabled || doc.panelSettings == null)
                continue;

            hostUiDocument = doc;
            return;
        }
    }
}