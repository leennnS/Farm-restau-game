using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class InventoryController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.I;

    [Header("Start State")]
    [SerializeField] private bool startOpen = false;

    // Optional: enable to log clicks on slots
    [Header("Debug")]
    [SerializeField] private bool debugSlotClicks = false;

    private UIDocument _uiDocument;
    private VisualElement _root;
    private bool _isOpen;

    // Cached UI references
    private Button _closeButton;

    private Button _tabToolsButton;
    private Button _tabCropsButton;
    private Button _tabCraftingButton;

    private VisualElement _toolsPage;
    private VisualElement _cropsPage;
    private VisualElement _craftingPage;

    private void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        _root = _uiDocument.rootVisualElement;

        CacheUI();
        BindUI();

        // Apply initial state
        SetOpen(startOpen);

        // Optional: start on Tools tab if open
        ShowTools();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            Toggle();
    }

    public void Toggle() => SetOpen(!_isOpen);
    public void Open() => SetOpen(true);
    public void Close() => SetOpen(false);

    private void SetOpen(bool open)
    {
        _isOpen = open;

        if (_root != null)
            _root.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // ----------------------------
    // NEW: UI Toolkit wiring
    // ----------------------------

    private void CacheUI()
    {
        // Header
        _closeButton = _root.Q<Button>("closeButton");

        // Tabs
        _tabToolsButton = _root.Q<Button>("tabToolsButton");
        _tabCropsButton = _root.Q<Button>("tabCropsButton");
        _tabCraftingButton = _root.Q<Button>("tabCraftingButton");

        // Pages
        _toolsPage = _root.Q<VisualElement>("toolsPage");
        _cropsPage = _root.Q<VisualElement>("cropsPage");
        _craftingPage = _root.Q<VisualElement>("craftingPage");
    }

    private void BindUI()
    {
        // Close button
        if (_closeButton != null)
            _closeButton.clicked += Close;

        // Tab switching
        if (_tabToolsButton != null)
            _tabToolsButton.clicked += ShowTools;

        if (_tabCropsButton != null)
            _tabCropsButton.clicked += ShowCrops;

        if (_tabCraftingButton != null)
            _tabCraftingButton.clicked += ShowCrafting;

        // OPTIONAL: make your VisualElement "slots" clickable (for debugging)
        if (debugSlotClicks)
        {
            HookSlotClicks("toolSlot", 12);
            HookSlotClicks("itemSlot", 36);
            HookSlotClicks("cropSlot", 12);
            HookSlotClicks("craftSlot", 6);
            HookSlotClicks("playerItem", 4);

            MakeClickable(_root.Q<VisualElement>("trashSlot"), "trashSlot");
        }
    }

    private void ShowTools() => ShowPage(_toolsPage, _cropsPage, _craftingPage);
    private void ShowCrops() => ShowPage(_cropsPage, _toolsPage, _craftingPage);
    private void ShowCrafting() => ShowPage(_craftingPage, _toolsPage, _cropsPage);

    private void ShowPage(VisualElement show, VisualElement hide1, VisualElement hide2)
    {
        if (show != null) show.style.display = DisplayStyle.Flex;
        if (hide1 != null) hide1.style.display = DisplayStyle.None;
        if (hide2 != null) hide2.style.display = DisplayStyle.None;
    }

    // ----------------------------
    // OPTIONAL: Slot click debugging
    // ----------------------------

    private void HookSlotClicks(string prefix, int count)
    {
        for (int i = 1; i <= count; i++)
        {
            string name = $"{prefix}{i:00}"; // itemSlot01, itemSlot02, etc.
            MakeClickable(_root.Q<VisualElement>(name), name);
        }
    }

    private void MakeClickable(VisualElement ve, string debugName)
    {
        if (ve == null) return;

        ve.pickingMode = PickingMode.Position;
        ve.RegisterCallback<ClickEvent>(_ =>
        {
            Debug.Log($"Clicked: {debugName}");
        });
    }

    private void OnDestroy()
    {
        // Clean unbinds (good practice)
        if (_closeButton != null) _closeButton.clicked -= Close;

        if (_tabToolsButton != null) _tabToolsButton.clicked -= ShowTools;
        if (_tabCropsButton != null) _tabCropsButton.clicked -= ShowCrops;
        if (_tabCraftingButton != null) _tabCraftingButton.clicked -= ShowCrafting;
    }
}
