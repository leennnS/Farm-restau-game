using System;
using UnityEngine;
using UnityEngine.UIElements;


[RequireComponent(typeof(UIDocument))]


public class InventoryController : MonoBehaviour
{
    private const int HotbarSize = 12;

    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.I;

    [Header("Start State")]
    [SerializeField] private bool startOpen = false;

    [Header("Debug")]
    [SerializeField] private bool debugSlotClicks = false;

    // ---------- NEW: Inventory ----------
    [Header("Inventory")]
    [SerializeField] private int inventorySize = 36;

    [Header("Crafting")]
    [SerializeField] private RecipeDefinition[] recipes;

    [Header("Map")]
    [SerializeField] private Texture2D gameMapImage;

    [Header("Quick Test (optional)")]
    [SerializeField] private ItemDefinition testItem;
    [SerializeField] private KeyCode testAddKey = KeyCode.K;
    [SerializeField] private int testAddAmount = 1;

    private ItemStack[] _cookingRecipeSlotData;
    private VisualElement[] _cookingIngredientSlotElements;

    private int _draggedCookingInventoryIndex = -1;
    private bool _isDraggingFromCookingInventory = false;

    private VisualElement _inventoryFooter;
    private VisualElement _playerCard;
    private UIDocument _uiDocument;
    private VisualElement _root;
    private bool _isOpen;

    // Hotbar HUD reference (supports either controller script)
    private HotBarHUDController _hotbarHUD;
    private HotBarController _hotbarController;

    // Cached UI references
    private Button _closeButton;

    private Button _tabToolsButton;
    private Button _tabMapButton;
    private Button _tabCraftingButton;
    private Button _tabSettingsButton;

    private VisualElement _toolsPage;
    private VisualElement _mapPage;
    private VisualElement _craftingPage;
    private VisualElement _settingsPage;

    // Cooking tab refs
    private Button _tabBreakfastButton;
    private Button _tabMainDishButton;
    private Button _tabDrinksButton;
    private Button _backToRecipesButton;
    private Button _cookRecipeButton;
    private VisualElement _cookingLoadingContainer;
    private VisualElement _cookingLoadingBarFill;
    private Label _cookingLoadingLabel;
    private Label _cookingProgressText;
    private bool _isCooking;

    private VisualElement _recipeBrowserView;
    private VisualElement _recipeDetailView;
    private VisualElement _recipeGrid;
    private VisualElement _recipeTooltip;
    private Label _recipeTooltipName;
    private Label _recipeTooltipIngredients;

    private Label _selectedRecipeName;
    private VisualElement _selectedRecipeIcon;
    private VisualElement _requiredIngredientSlots;
    private VisualElement _craftingInventoryGrid;

    private RecipeDefinition _selectedRecipe;
    private RecipeCategory _currentRecipeCategory = RecipeCategory.BreakfastBakery;

    // Settings controls
    private Slider _masterVolumeSlider;
    private Slider _musicVolumeSlider;
    private Slider _sfxVolumeSlider;
    private Label _masterVolumeLabel;
    private Label _musicVolumeLabel;
    private Label _sfxVolumeLabel;
    private Button _exitButton;
    private Button _quitButton;

    // ---------- NEW: slot UI refs ----------
    private VisualElement[] _itemSlots; // itemSlot01..itemSlot36
    private VisualElement[] _hotbarSlots; // hotbarSlot01..hotbarSlot12
    private VisualElement _trashSlot;

    private ItemStack[] _slotsData;
    private ItemStack[] _hotbarData;

    // ------- Drag and Drop State -------
    private int _draggedSlotIndex = -1;
    private VisualElement _draggedSlotElement;
    private bool _isDragging = false;
    private bool _isDraggingFromHotbar = false;

    private void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        _root = _uiDocument.rootVisualElement;
        var cookingStyleSheet = Resources.Load<StyleSheet>("CookingStyles");
        if (cookingStyleSheet != null && !_root.styleSheets.Contains(cookingStyleSheet))
            _root.styleSheets.Add(cookingStyleSheet);
        else if (cookingStyleSheet == null)
            Debug.LogWarning("CookingStyles.uss not found in a Resources folder.");

        // Find the HotBar HUD controller
        TryResolveHotbarHUD();

        CacheUI();
        CacheInventorySlots();     // NEW
        BindUI();

        _slotsData = new ItemStack[inventorySize]; // NEW
        _hotbarData = new ItemStack[HotbarSize];
        RefreshAllSlots();                         // NEW

        // Apply initial state
        SetOpen(startOpen);

        // Optional: start on Tools tab if open
        ShowTools();

        // Ensure outside HUD mirrors first 12 inventory slots from startup.
        SyncExternalHotbarAll();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            Toggle();

        // quick test: press K to add testItem
        if (testItem != null && Input.GetKeyDown(testAddKey))
            TryAdd(testItem, testAddAmount);

        // Keep external HUD mirrored to inventory slots 1..12.
        SyncExternalHotbarAll();
    }

    public void Toggle() => SetOpen(!_isOpen);
    public void Open() => SetOpen(true);
    public void Close() => SetOpen(false);

    private void SetOpen(bool open)
    {
        _isOpen = open;

        TryResolveHotbarHUD();

        // Hide hotbar when inventory opens, show when it closes
        SetExternalHotbarVisible(!open);

        // Refresh mirrored content when visibility changes.
        SyncExternalHotbarAll();

        if (_root != null)
            _root.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // ----------------------------
    // UI Toolkit wiring
    // ----------------------------

    private void CacheUI()
    {
        // Header
        _closeButton = _root.Q<Button>("closeButton");

        // Tabs
        _tabToolsButton = _root.Q<Button>("tabToolsButton");
        _tabMapButton = _root.Q<Button>("tabMapButton");
        _tabCraftingButton = _root.Q<Button>("tabCraftingButton");
        _tabSettingsButton = _root.Q<Button>("tabSettingsButton");

        // Pages
        _toolsPage = _root.Q<VisualElement>("toolsPage");
        _mapPage = _root.Q<VisualElement>("mapPage");
        _craftingPage = _root.Q<VisualElement>("craftingPage");
        _settingsPage = _root.Q<VisualElement>("settingsPage");

        // Settings controls
        _masterVolumeSlider = _root.Q<Slider>("masterVolumeSlider");
        _musicVolumeSlider = _root.Q<Slider>("musicVolumeSlider");
        _sfxVolumeSlider = _root.Q<Slider>("sfxVolumeSlider");
        _masterVolumeLabel = _root.Q<Label>("masterVolumeLabel");
        _musicVolumeLabel = _root.Q<Label>("musicVolumeLabel");
        _sfxVolumeLabel = _root.Q<Label>("sfxVolumeLabel");
        _exitButton = _root.Q<Button>("exitButton");
        _quitButton = _root.Q<Button>("quitButton");
        // Cooking tab
        _tabBreakfastButton = _root.Q<Button>("tabBreakfastButton");
        _tabMainDishButton = _root.Q<Button>("tabMainDishButton");
        _tabDrinksButton = _root.Q<Button>("tabDrinksButton");
        _backToRecipesButton = _root.Q<Button>("backToRecipesButton");
        _cookRecipeButton = _root.Q<Button>("cookRecipeButton");

        _recipeBrowserView = _root.Q<VisualElement>("recipeBrowserView");
        _recipeDetailView = _root.Q<VisualElement>("recipeDetailView");
        _recipeGrid = _root.Q<VisualElement>("recipeGrid");
        _recipeTooltip = _root.Q<VisualElement>("recipeTooltip");
        _recipeTooltipName = _root.Q<Label>("recipeTooltipName");
        _recipeTooltipIngredients = _root.Q<Label>("recipeTooltipIngredients");

        _selectedRecipeName = _root.Q<Label>("selectedRecipeName");
        _selectedRecipeIcon = _root.Q<VisualElement>("selectedRecipeIcon");
        _requiredIngredientSlots = _root.Q<VisualElement>("requiredIngredientSlots");
        _craftingInventoryGrid = _root.Q<VisualElement>("craftingInventoryGrid");

        _inventoryFooter = _root.Q<VisualElement>("inventoryFooter");
        _playerCard = _root.Q<VisualElement>("playerCard");
        _cookingLoadingContainer = _root.Q<VisualElement>("cookingLoadingContainer");
        _cookingLoadingBarFill = _root.Q<VisualElement>("cookingLoadingBarFill");
        _cookingLoadingLabel = _root.Q<Label>("cookingLoadingLabel");
        _cookingProgressText = _root.Q<Label>("cookingProgressText");
    }

    // NEW: cache inventory slots itemSlot01..itemSlot36 AND hotbarSlot01..hotbarSlot12
    private void CacheInventorySlots()
    {
        _itemSlots = new VisualElement[inventorySize];
        _hotbarSlots = new VisualElement[HotbarSize];

        // Cache inventory grid slots
        for (int i = 0; i < inventorySize; i++)
        {
            string name = $"itemSlot{(i + 1):00}";
            _itemSlots[i] = _root.Q<VisualElement>(name);

            if (_itemSlots[i] != null)
            {
                _itemSlots[i].style.position = Position.Relative;

                // Register drag and drop handlers
                int slotIndex = i;
                _itemSlots[i].pickingMode = PickingMode.Position;
                _itemSlots[i].RegisterCallback<MouseDownEvent>(evt => OnInventorySlotMouseDown(slotIndex, evt));
                _itemSlots[i].RegisterCallback<MouseUpEvent>(evt => OnInventorySlotMouseUp(slotIndex, evt));
            }
            else
            {
                Debug.LogWarning($"Missing slot in UXML: {name}");
            }
        }

        // Cache hotbar slots separately
        for (int i = 0; i < HotbarSize; i++)
        {
            string name = $"hotbarSlot{(i + 1):00}";
            _hotbarSlots[i] = _root.Q<VisualElement>(name);

            if (_hotbarSlots[i] != null)
            {
                _hotbarSlots[i].style.position = Position.Relative;

                // Register drag and drop handlers
                int slotIndex = i;
                _hotbarSlots[i].pickingMode = PickingMode.Position;
                _hotbarSlots[i].RegisterCallback<MouseDownEvent>(evt => OnHotbarSlotMouseDown(slotIndex, evt));
                _hotbarSlots[i].RegisterCallback<MouseUpEvent>(evt => OnHotbarSlotMouseUp(slotIndex, evt));
            }
            else
            {
                Debug.LogWarning($"Missing slot in UXML: {name}");
            }
        }

        // Cache trash slot and register mouseup handler
        _trashSlot = _root.Q<VisualElement>("trashSlot");
        if (_trashSlot != null)
        {
            _trashSlot.pickingMode = PickingMode.Position;
            _trashSlot.RegisterCallback<MouseUpEvent>(evt => OnTrashSlotMouseUp(evt));
        }
        else
        {
            Debug.LogWarning("Missing slot in UXML: trashSlot");
        }
    }

    private void BindUI()
    {
        // Close button
        if (_closeButton != null)
            _closeButton.clicked += Close;

        // Tab switching
        if (_tabToolsButton != null)
            _tabToolsButton.clicked += ShowTools;

        if (_tabMapButton != null)
            _tabMapButton.clicked += ShowMap;

        if (_tabCraftingButton != null)
            _tabCraftingButton.clicked += ShowCrafting;

        if (_tabSettingsButton != null)
            _tabSettingsButton.clicked += ShowSettings;

        // Settings controls
        if (_masterVolumeSlider != null)
            _masterVolumeSlider.RegisterValueChangedCallback(evt => OnMasterVolumeChanged(evt.newValue));

        if (_musicVolumeSlider != null)
            _musicVolumeSlider.RegisterValueChangedCallback(evt => OnMusicVolumeChanged(evt.newValue));

        if (_sfxVolumeSlider != null)
            _sfxVolumeSlider.RegisterValueChangedCallback(evt => OnSFXVolumeChanged(evt.newValue));

        if (_exitButton != null)
            _exitButton.clicked += ExitToMenu;

        if (_quitButton != null)
            _quitButton.clicked += QuitGame;

        // OPTIONAL: slot click debugging
        if (debugSlotClicks)
        {
            HookSlotClicks("itemSlot", inventorySize);
            MakeClickable(_root.Q<VisualElement>("trashSlot"), "trashSlot");
        }

        // Populate crafting recipes
        PopulateCraftingRecipes();

        // Populate map display
        PopulateMapDisplay();
        if (_tabBreakfastButton != null)
            _tabBreakfastButton.clicked += () => ShowRecipeCategory(RecipeCategory.BreakfastBakery);

        if (_tabMainDishButton != null)
            _tabMainDishButton.clicked += () => ShowRecipeCategory(RecipeCategory.MainDish);

        if (_tabDrinksButton != null)
            _tabDrinksButton.clicked += () => ShowRecipeCategory(RecipeCategory.SoupsDrinks);

        if (_backToRecipesButton != null)
            _backToRecipesButton.clicked += ShowRecipeBrowser;

        if (_cookRecipeButton != null)
            _cookRecipeButton.clicked += CookSelectedRecipe;
    }

    private void ShowTools()
    {
        ShowPage(_toolsPage, _mapPage, _craftingPage, _settingsPage);
        SetFooterVisible(true);
    }

    private void ShowMap()
    {
        ShowPage(_mapPage, _toolsPage, _craftingPage, _settingsPage);
        SetFooterVisible(true);
    }
    private void ShowCrafting()
    {
        ShowPage(_craftingPage, _toolsPage, _mapPage, _settingsPage);
        SetFooterVisible(false);
        ShowRecipeCategory(_currentRecipeCategory);
    }
    private void ShowSettings()
    {
        ShowPage(_settingsPage, _toolsPage, _mapPage, _craftingPage);
        SetFooterVisible(true);
    }

    private void ShowPage(VisualElement show, VisualElement hide1, VisualElement hide2, VisualElement hide3)
    {
        if (show != null) show.style.display = DisplayStyle.Flex;
        if (hide1 != null) hide1.style.display = DisplayStyle.None;
        if (hide2 != null) hide2.style.display = DisplayStyle.None;
        if (hide3 != null) hide3.style.display = DisplayStyle.None;
    }

    // Settings callbacks
    private void OnMasterVolumeChanged(float value)
    {
        if (_masterVolumeLabel != null)
            _masterVolumeLabel.text = ((int)value).ToString();
        // TODO: Apply master volume to AudioListener
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (_musicVolumeLabel != null)
            _musicVolumeLabel.text = ((int)value).ToString();
        // TODO: Apply music volume to music source
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (_sfxVolumeLabel != null)
            _sfxVolumeLabel.text = ((int)value).ToString();
        // TODO: Apply SFX volume to SFX source
    }

    private void ExitToMenu()
    {
        Debug.Log("Exiting to menu...");
        // TODO: Load main menu scene
        // SceneManager.LoadScene("MainMenu");
    }

    private void QuitGame()
    {
        Debug.Log("Quitting game...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ----------------------------
    // Inventory functionality (NEW)
    // ----------------------------

    // Call this from pickups/crafting/etc.
    public bool TryAdd(ItemDefinition item, int amount)
    {
        if (item == null || amount <= 0) return false;

        // 1) stack into existing
        for (int i = 0; i < _slotsData.Length && amount > 0; i++)
        {
            if (_slotsData[i].item == item && _slotsData[i].amount < item.maxStack)
            {
                int canAdd = item.maxStack - _slotsData[i].amount;
                int addNow = Mathf.Min(canAdd, amount);

                _slotsData[i].amount += addNow;
                amount -= addNow;

                RefreshSlot(i);
            }
        }

        // 2) fill empty slots
        for (int i = 0; i < _slotsData.Length && amount > 0; i++)
        {
            if (_slotsData[i].item == null || _slotsData[i].amount <= 0)
            {
                int addNow = Mathf.Min(item.maxStack, amount);

                _slotsData[i] = new ItemStack { item = item, amount = addNow };
                amount -= addNow;

                RefreshSlot(i);
            }
        }

        return amount == 0;
    }

    private void RefreshAllSlots()
    {
        for (int i = 0; i < _slotsData.Length; i++)
            RefreshInventorySlot(i);

        for (int i = 0; i < _hotbarData.Length; i++)
            RefreshHotbarSlot(i);

        SyncExternalHotbarAll();
    }

    private void RefreshInventorySlot(int index)
    {
        if (_itemSlots == null || index < 0 || index >= _itemSlots.Length) return;

        var slotVE = _itemSlots[index];
        if (slotVE == null) return;

        var stack = _slotsData[index];

        // empty
        if (stack.item == null || stack.amount <= 0)
        {
            slotVE.style.backgroundImage = StyleKeyword.None;
            SetSlotCount(slotVE, "");
            return;
        }

        // icon
        slotVE.style.backgroundImage = new StyleBackground(stack.item.icon);

        // count
        SetSlotCount(slotVE, stack.amount > 1 ? stack.amount.ToString() : "");
    }

    private void RefreshHotbarSlot(int index)
    {
        if (_hotbarSlots == null || index < 0 || index >= _hotbarSlots.Length) return;
        if (_hotbarData == null || index >= _hotbarData.Length) return;

        var slotVE = _hotbarSlots[index];
        if (slotVE == null) return;

        var stack = _hotbarData[index];

        // empty
        if (stack.item == null || stack.amount <= 0)
        {
            slotVE.style.backgroundImage = StyleKeyword.None;
            SetSlotCount(slotVE, "");
            return;
        }

        // icon
        slotVE.style.backgroundImage = new StyleBackground(stack.item.icon);

        // count
        SetSlotCount(slotVE, stack.amount > 1 ? stack.amount.ToString() : "");
    }

    private void RefreshSlot(int index)
    {
        RefreshInventorySlot(index);
    }

    private void SyncExternalHotbarAll()
    {
        TryResolveHotbarHUD();
        if (!HasExternalHotbar() || _hotbarData == null) return;

        for (int i = 0; i < HotbarSize; i++)
            SyncExternalHotbarSlot(i);
    }

    private void TryResolveHotbarHUD()
    {
        if (_hotbarHUD == null)
            _hotbarHUD = FindFirstObjectByType<HotBarHUDController>();

        if (_hotbarController == null)
            _hotbarController = FindFirstObjectByType<HotBarController>();
    }

    private bool HasExternalHotbar()
    {
        return _hotbarHUD != null || _hotbarController != null;
    }

    private void SetExternalHotbarVisible(bool visible)
    {
        if (_hotbarHUD != null)
            _hotbarHUD.SetVisible(visible);

        if (_hotbarController != null)
            _hotbarController.SetVisible(visible);
    }

    private void SetExternalHotbarSlot(int index, Sprite icon, int amount)
    {
        if (_hotbarHUD != null)
            _hotbarHUD.SetSlot(index, icon, amount);

        if (_hotbarController != null)
            _hotbarController.SetSlot(index, icon, amount);
    }

    private void SyncExternalHotbarSlot(int index)
    {
        if (!HasExternalHotbar()) return;
        if (index < 0 || index >= HotbarSize) return;

        if (_hotbarData == null || index >= _hotbarData.Length)
        {
            SetExternalHotbarSlot(index, null, 0);
            return;
        }

        var stack = _hotbarData[index];
        var icon = stack.item != null ? stack.item.icon : null;
        var amount = stack.item != null ? stack.amount : 0;

        SetExternalHotbarSlot(index, icon, amount);
    }

    private void SetSlotCount(VisualElement slotVE, string text)
    {
        var countLabel = slotVE.Q<Label>("countLabel");
        if (countLabel == null)
        {
            countLabel = new Label { name = "countLabel" };
            countLabel.style.position = Position.Absolute;
            countLabel.style.right = 2;
            countLabel.style.bottom = 0;
            countLabel.style.fontSize = 11;
            countLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            countLabel.style.color = new Color(0.23f, 0.16f, 0.09f);
            slotVE.Add(countLabel);
        }

        countLabel.text = text;
    }

    // ----------------------------
    // OPTIONAL: Slot click debugging (kept)
    // ----------------------------

    private void HookSlotClicks(string prefix, int count)
    {
        for (int i = 1; i <= count; i++)
        {
            string name = $"{prefix}{i:00}";
            var ve = _root.Q<VisualElement>(name);
            MakeClickable(ve, name);
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

    // ----------------------------
    // Drag and Drop Handlers (NEW)
    // ----------------------------

    private void OnInventorySlotMouseDown(int slotIndex, MouseDownEvent evt)
    {
        if (_slotsData[slotIndex].item == null || _slotsData[slotIndex].amount <= 0)
            return;

        _draggedSlotIndex = slotIndex;
        _draggedSlotElement = _itemSlots[slotIndex];
        _isDragging = true;
        _isDraggingFromHotbar = false;

        _draggedSlotElement.style.opacity = 0.5f;

        if (debugSlotClicks)
            Debug.Log($"Started dragging inventory slot {slotIndex}");

        evt.StopPropagation();
    }

    private void OnInventorySlotMouseUp(int targetSlotIndex, MouseUpEvent evt)
    {
        if (!_isDragging || _draggedSlotIndex < 0)
        {
            ResetDragState();
            return;
        }

        // Restore opacity
        if (_draggedSlotElement != null)
            _draggedSlotElement.style.opacity = 1f;

        // Swap within inventory or move from hotbar to inventory
        if (!_isDraggingFromHotbar)
        {
            // Both in inventory
            if (_draggedSlotIndex != targetSlotIndex)
            {
                SwapInventorySlots(_draggedSlotIndex, targetSlotIndex);
                if (debugSlotClicks)
                    Debug.Log($"Swapped inventory slots {_draggedSlotIndex} <-> {targetSlotIndex}");
            }
        }
        else
        {
            // From hotbar to inventory - MOVE operation (not swap)
            MoveHotbarToInventory(_draggedSlotIndex, targetSlotIndex);
        }

        ResetDragState();
        evt.StopPropagation();
    }

    private void OnHotbarSlotMouseDown(int slotIndex, MouseDownEvent evt)
    {
        if (_hotbarData == null || slotIndex < 0 || slotIndex >= _hotbarData.Length)
            return;

        if (_hotbarData[slotIndex].item == null || _hotbarData[slotIndex].amount <= 0)
            return;

        _draggedSlotIndex = slotIndex;
        _draggedSlotElement = _hotbarSlots[slotIndex];
        _isDragging = true;
        _isDraggingFromHotbar = true;

        _draggedSlotElement.style.opacity = 0.5f;

        if (debugSlotClicks)
            Debug.Log($"Started dragging hotbar slot {slotIndex}");

        evt.StopPropagation();
    }

    private void OnHotbarSlotMouseUp(int targetSlotIndex, MouseUpEvent evt)
    {
        if (!_isDragging || _draggedSlotIndex < 0)
        {
            ResetDragState();
            return;
        }

        // Restore opacity
        if (_draggedSlotElement != null)
            _draggedSlotElement.style.opacity = 1f;

        // Swap within hotbar or swap with inventory
        if (_isDraggingFromHotbar)
        {
            // Both in hotbar
            if (_draggedSlotIndex != targetSlotIndex)
            {
                SwapHotbarSlots(_draggedSlotIndex, targetSlotIndex);
                if (debugSlotClicks)
                    Debug.Log($"Swapped hotbar slots {_draggedSlotIndex} <-> {targetSlotIndex}");
            }
        }
        else
        {
            // From inventory to hotbar
            CopyInventoryToHotbar(_draggedSlotIndex, targetSlotIndex);
        }

        ResetDragState();
        evt.StopPropagation();
    }

    private void OnTrashSlotMouseUp(MouseUpEvent evt)
    {
        if (!_isDragging || _draggedSlotIndex < 0)
        {
            ResetDragState();
            return;
        }

        // Restore opacity
        if (_draggedSlotElement != null)
            _draggedSlotElement.style.opacity = 1f;

        // Delete the dragged item
        if (_isDraggingFromHotbar)
        {
            // Clear the hotbar slot
            _hotbarData[_draggedSlotIndex] = new ItemStack { item = null, amount = 0 };
            RefreshHotbarSlot(_draggedSlotIndex);
            SyncExternalHotbarSlot(_draggedSlotIndex);
            if (debugSlotClicks)
                Debug.Log($"Deleted item from hotbar slot {_draggedSlotIndex}");
        }
        else
        {
            // Clear the inventory slot
            var deletedItem = _slotsData[_draggedSlotIndex].item;
            _slotsData[_draggedSlotIndex] = new ItemStack { item = null, amount = 0 };
            RefreshInventorySlot(_draggedSlotIndex);

            // Also remove from hotbar if it exists there
            if (deletedItem != null)
            {
                for (int i = 0; i < _hotbarData.Length; i++)
                {
                    if (_hotbarData[i].item == deletedItem)
                    {
                        _hotbarData[i] = new ItemStack { item = null, amount = 0 };
                        RefreshHotbarSlot(i);
                        SyncExternalHotbarSlot(i);
                    }
                }
            }

            if (debugSlotClicks)
                Debug.Log($"Deleted item from inventory slot {_draggedSlotIndex}");
        }

        ResetDragState();
        evt.StopPropagation();
    }

    private void SwapInventorySlots(int source, int target)
    {
        if (source < 0 || source >= _slotsData.Length || target < 0 || target >= _slotsData.Length)
            return;

        ItemStack temp = _slotsData[source];
        _slotsData[source] = _slotsData[target];
        _slotsData[target] = temp;

        RefreshSlot(source);
        RefreshSlot(target);
    }

    private void SwapHotbarSlots(int source, int target)
    {
        if (source < 0 || source >= _hotbarData.Length || target < 0 || target >= _hotbarData.Length)
            return;

        ItemStack temp = _hotbarData[source];
        _hotbarData[source] = _hotbarData[target];
        _hotbarData[target] = temp;

        RefreshHotbarSlot(source);
        RefreshHotbarSlot(target);
        SyncExternalHotbarSlot(source);
        SyncExternalHotbarSlot(target);
    }

    private void SwapInventoryAndHotbar(int slotIndex, int otherIndex, bool draggedFromInventory)
    {
        ItemStack temp;

        if (draggedFromInventory)
        {
            temp = _slotsData[slotIndex];
            _slotsData[slotIndex] = _hotbarData[otherIndex];
            _hotbarData[otherIndex] = temp;

            RefreshInventorySlot(slotIndex);
            RefreshHotbarSlot(otherIndex);
            SyncExternalHotbarSlot(otherIndex);
        }
        else
        {
            temp = _hotbarData[slotIndex];
            _hotbarData[slotIndex] = _slotsData[otherIndex];
            _slotsData[otherIndex] = temp;

            RefreshHotbarSlot(slotIndex);
            RefreshInventorySlot(otherIndex);
            SyncExternalHotbarSlot(slotIndex);
        }

        if (debugSlotClicks)
            Debug.Log($"Swapped inventory and hotbar");
    }

    private void CopyInventoryToHotbar(int inventoryIndex, int hotbarIndex)
    {
        if (inventoryIndex < 0 || inventoryIndex >= _slotsData.Length) return;
        if (hotbarIndex < 0 || hotbarIndex >= _hotbarData.Length) return;

        var source = _slotsData[inventoryIndex];
        if (source.item == null || source.amount <= 0) return;

        // Check if this item already exists in the hotbar
        for (int i = 0; i < _hotbarData.Length; i++)
        {
            if (_hotbarData[i].item == source.item)
            {
                if (debugSlotClicks)
                    Debug.Log($"Item already exists in hotbar at slot {i}. Cannot add duplicates.");
                return; // Item already in hotbar, don't add again
            }
        }

        // Only add if target slot is empty
        if (_hotbarData[hotbarIndex].item != null)
        {
            if (debugSlotClicks)
                Debug.Log($"Hotbar slot {hotbarIndex} is not empty. Swap functionality would apply here.");
            return; // Target slot is occupied, don't overwrite
        }

        _hotbarData[hotbarIndex] = new ItemStack
        {
            item = source.item,
            amount = source.amount // Transfer full stack amount
        };

        RefreshHotbarSlot(hotbarIndex);
        SyncExternalHotbarSlot(hotbarIndex);

        if (debugSlotClicks)
            Debug.Log($"Copied inventory slot {inventoryIndex} to hotbar slot {hotbarIndex} ({source.amount} items)");
    }

    private void MoveHotbarToInventory(int hotbarIndex, int inventoryIndex)
    {
        if (hotbarIndex < 0 || hotbarIndex >= _hotbarData.Length) return;
        if (inventoryIndex < 0 || inventoryIndex >= _slotsData.Length) return;

        var source = _hotbarData[hotbarIndex];
        if (source.item == null || source.amount <= 0) return;

        // Check if this item already exists in inventory
        for (int i = 0; i < _slotsData.Length; i++)
        {
            if (_slotsData[i].item == source.item)
            {
                if (debugSlotClicks)
                    Debug.Log($"Item already exists in inventory at slot {i}. Cannot add duplicates.");
                return; // Item already in inventory, don't add again
            }
        }

        // Only add if target slot is empty
        if (_slotsData[inventoryIndex].item != null)
        {
            if (debugSlotClicks)
                Debug.Log($"Inventory slot {inventoryIndex} is not empty.");
            return; // Target slot is occupied, don't overwrite
        }

        // Move the item from hotbar to inventory
        _slotsData[inventoryIndex] = new ItemStack
        {
            item = source.item,
            amount = source.amount
        };

        // Clear the hotbar slot
        _hotbarData[hotbarIndex] = new ItemStack { item = null, amount = 0 };

        RefreshInventorySlot(inventoryIndex);
        RefreshHotbarSlot(hotbarIndex);
        SyncExternalHotbarSlot(hotbarIndex);

        if (debugSlotClicks)
            Debug.Log($"Moved hotbar slot {hotbarIndex} to inventory slot {inventoryIndex}");
    }

    private void ResetDragState()
    {
        _draggedSlotIndex = -1;
        _draggedSlotElement = null;
        _isDragging = false;
        _isDraggingFromHotbar = false;
    }

    private void OnSlotMouseDown(int slotIndex, MouseDownEvent evt)
    {
        OnInventorySlotMouseDown(slotIndex, evt);
    }

    private void OnSlotMouseUp(int targetSlotIndex, MouseUpEvent evt)
    {
        OnInventorySlotMouseUp(targetSlotIndex, evt);
    }

    private void SwapOrMoveItems(int sourceSlotIndex, int targetSlotIndex)
    {
        SwapInventorySlots(sourceSlotIndex, targetSlotIndex);
    }

    private void OnDestroy()
    {
        if (_closeButton != null) _closeButton.clicked -= Close;

        if (_tabToolsButton != null) _tabToolsButton.clicked -= ShowTools;
        if (_tabMapButton != null) _tabMapButton.clicked -= ShowMap;
        if (_tabCraftingButton != null) _tabCraftingButton.clicked -= ShowCrafting;
        if (_tabSettingsButton != null) _tabSettingsButton.clicked -= ShowSettings;

        if (_masterVolumeSlider != null)
            _masterVolumeSlider.UnregisterValueChangedCallback(evt => OnMasterVolumeChanged(evt.newValue));
        if (_musicVolumeSlider != null)
            _musicVolumeSlider.UnregisterValueChangedCallback(evt => OnMusicVolumeChanged(evt.newValue));
        if (_sfxVolumeSlider != null)
            _sfxVolumeSlider.UnregisterValueChangedCallback(evt => OnSFXVolumeChanged(evt.newValue));

        if (_exitButton != null)
            _exitButton.clicked -= ExitToMenu;
        if (_quitButton != null)
            _quitButton.clicked -= QuitGame;
    }

    // ==================== CRAFTING SYSTEM ====================
    private void ShowRecipeCategory(RecipeCategory category)
    {
        _currentRecipeCategory = category;
        Debug.Log($"Switching to recipe category: {category}");
        ShowRecipeBrowser();
        PopulateRecipeGrid();
    }

    private void ShowRecipeBrowser()
    {
        ReturnPlacedCookingIngredientsToInventory();
        _selectedRecipe = null;

        if (_recipeBrowserView != null)
            _recipeBrowserView.style.display = DisplayStyle.Flex;

        if (_recipeDetailView != null)
            _recipeDetailView.style.display = DisplayStyle.None;
        if (_cookingLoadingContainer != null)
            _cookingLoadingContainer.style.display = DisplayStyle.None;

        if (_cookRecipeButton != null)
            _cookRecipeButton.SetEnabled(true);

        if (_backToRecipesButton != null)
            _backToRecipesButton.SetEnabled(true);

        _isCooking = false;
    }
    private void PopulateRecipeGrid()
    {
        if (_recipeGrid == null)
        {
            Debug.Log("recipeGrid is NULL");
            return;
        }

        _recipeGrid.Clear();

        if (recipes == null || recipes.Length == 0)
        {
            Debug.Log("No recipes assigned.");
            return;
        }

        int recipeCount = 0;
        int matchingCategoryCount = 0;
        int withIngredientsCount = 0;

        foreach (var recipe in recipes)
        {
            if (recipe == null)
                continue;

            recipeCount++;
            Debug.Log($"Recipe: {recipe.recipeName}, Category: {recipe.category}, Current: {_currentRecipeCategory}");

            // Only show recipes in the current category
            if (recipe.category != _currentRecipeCategory)
                continue;

            matchingCategoryCount++;
            Debug.Log($"  → Matched category for: {recipe.recipeName}");

            int ingredientCount = recipe.ingredients != null ? recipe.ingredients.Length : 0;
            if (ingredientCount <= 0)
            {
                Debug.Log($"  → Skipped {recipe.recipeName}: no ingredients ({ingredientCount})");
                continue;
            }

            withIngredientsCount++;

            VisualElement dishBox = new VisualElement();
            dishBox.style.width = 70;
            dishBox.style.height = 70;
            dishBox.style.backgroundColor = new Color(0.96f, 0.91f, 0.82f, 1f);
            dishBox.style.marginRight = 8;
            dishBox.style.marginBottom = 8;
            dishBox.style.position = Position.Relative;
            dishBox.style.borderLeftWidth = 2;
            dishBox.style.borderRightWidth = 2;
            dishBox.style.borderTopWidth = 2;
            dishBox.style.borderBottomWidth = 2;
            dishBox.style.borderLeftColor = new Color(0.78f, 0.65f, 0.48f, 1f);
            dishBox.style.borderRightColor = new Color(0.78f, 0.65f, 0.48f, 1f);
            dishBox.style.borderTopColor = new Color(0.90f, 0.81f, 0.63f, 1f);
            dishBox.style.borderBottomColor = new Color(0.61f, 0.42f, 0.23f, 1f);

            if (recipe.result != null && recipe.result.icon != null)
                dishBox.style.backgroundImage = new StyleBackground(recipe.result.icon);

            Label nameLabel = new Label(recipe.recipeName);
            nameLabel.style.fontSize = 9;
            nameLabel.style.color = new Color(0.23f, 0.16f, 0.09f, 1f);
            nameLabel.style.backgroundColor = new Color(1f, 1f, 1f, 0.55f);
            nameLabel.style.position = Position.Absolute;
            nameLabel.style.left = 0;
            nameLabel.style.right = 0;
            nameLabel.style.bottom = 0;
            dishBox.Add(nameLabel);

            dishBox.RegisterCallback<ClickEvent>(_ =>
            {
                ShowRecipeDetail(recipe);
            });

            _recipeGrid.Add(dishBox);
        }

        Debug.Log($"Recipe Grid Summary: Total recipes: {recipeCount}, Matching category: {matchingCategoryCount}, With ingredients: {withIngredientsCount}");
    }

    private void ShowRecipeDetail(RecipeDefinition recipe)
    {
        if (recipe == null)
            return;

        _selectedRecipe = recipe;

        if (_recipeBrowserView != null)
            _recipeBrowserView.style.display = DisplayStyle.None;

        if (_recipeDetailView != null)
            _recipeDetailView.style.display = DisplayStyle.Flex;

        if (_selectedRecipeName != null)
            _selectedRecipeName.text = recipe.recipeName;

        if (_selectedRecipeIcon != null)
        {
            if (recipe.recipeIcon != null)
                _selectedRecipeIcon.style.backgroundImage = new StyleBackground(recipe.recipeIcon);
            else if (recipe.result != null && recipe.result.icon != null)
                _selectedRecipeIcon.style.backgroundImage = new StyleBackground(recipe.result.icon);
            else
                _selectedRecipeIcon.style.backgroundImage = StyleKeyword.None;
        }

        int ingredientCount = recipe.ingredients != null ? recipe.ingredients.Length : 0;

        _cookingRecipeSlotData = new ItemStack[ingredientCount];
        _cookingIngredientSlotElements = new VisualElement[ingredientCount];

        if (_requiredIngredientSlots != null)
        {
            _requiredIngredientSlots.Clear();

            for (int i = 0; i < ingredientCount; i++)
            {
                int slotIndex = i;

                VisualElement slot = new VisualElement();
                slot.style.width = 48;
                slot.style.height = 48;
                slot.style.position = Position.Relative;

                slot.RegisterCallback<MouseUpEvent>(evt => OnCookingIngredientSlotMouseUp(slotIndex, evt));

                _cookingIngredientSlotElements[i] = slot;
                _cookingRecipeSlotData[i] = new ItemStack
                {
                    item = recipe.ingredients[i].item,
                    amount = 0
                };

                _requiredIngredientSlots.Add(slot);
                RefreshCookingIngredientSlotVisual(i);
            }
        }

        PopulateCraftingInventoryFromRealData();
    }

    private void ShowFakeRecipeDetail(string dishName)
    {
        if (_recipeBrowserView != null)
            _recipeBrowserView.style.display = DisplayStyle.None;

        if (_recipeDetailView != null)
            _recipeDetailView.style.display = DisplayStyle.Flex;

        if (_selectedRecipeName != null)
            _selectedRecipeName.text = dishName;

        if (_selectedRecipeIcon != null)
            _selectedRecipeIcon.style.backgroundImage = StyleKeyword.None;

        _cookingRecipeSlotData = new ItemStack[4];
        _cookingIngredientSlotElements = new VisualElement[4];

        if (_requiredIngredientSlots != null)
        {
            _requiredIngredientSlots.Clear();

            for (int i = 0; i < 4; i++)
            {
                int slotIndex = i;

                VisualElement slot = new VisualElement();
                slot.style.width = 40;
                slot.style.height = 40;
                slot.style.backgroundColor = new Color(0.95f, 0.88f, 0.72f, 1f);
                slot.style.borderLeftWidth = 2;
                slot.style.borderRightWidth = 2;
                slot.style.borderTopWidth = 2;
                slot.style.borderBottomWidth = 2;
                slot.style.borderLeftColor = new Color(0.78f, 0.65f, 0.48f, 1f);
                slot.style.borderRightColor = new Color(0.78f, 0.65f, 0.48f, 1f);
                slot.style.borderTopColor = new Color(0.90f, 0.81f, 0.63f, 1f);
                slot.style.borderBottomColor = new Color(0.61f, 0.42f, 0.23f, 1f);
                slot.style.position = Position.Relative;

                slot.RegisterCallback<MouseUpEvent>(evt => OnCookingIngredientSlotMouseUp(slotIndex, evt));

                _cookingIngredientSlotElements[i] = slot;
                _requiredIngredientSlots.Add(slot);
            }
        }

        PopulateCraftingInventoryFromRealData();
    }

    private void PopulateCraftingInventoryFromRealData()
    {
        if (_craftingInventoryGrid == null || _slotsData == null)
            return;

        _craftingInventoryGrid.Clear();

        for (int i = 0; i < _slotsData.Length; i++)
        {
            int inventoryIndex = i;

            VisualElement slot = new VisualElement();
            slot.style.width = 36;
            slot.style.height = 36;
            slot.style.backgroundColor = new Color(0.96f, 0.91f, 0.82f, 1f);
            slot.style.borderLeftWidth = 2;
            slot.style.borderRightWidth = 2;
            slot.style.borderTopWidth = 2;
            slot.style.borderBottomWidth = 2;
            slot.style.borderLeftColor = new Color(0.78f, 0.65f, 0.48f, 1f);
            slot.style.borderRightColor = new Color(0.78f, 0.65f, 0.48f, 1f);
            slot.style.borderTopColor = new Color(0.90f, 0.81f, 0.63f, 1f);
            slot.style.borderBottomColor = new Color(0.61f, 0.42f, 0.23f, 1f);
            slot.style.marginRight = 6;
            slot.style.marginBottom = 6;
            slot.style.position = Position.Relative;
            slot.pickingMode = PickingMode.Position;

            var stack = _slotsData[i];

            if (stack.item != null && stack.amount > 0)
            {
                if (stack.item.icon != null)
                    slot.style.backgroundImage = new StyleBackground(stack.item.icon);

                Label countLabel = new Label(stack.amount > 1 ? stack.amount.ToString() : "");
                countLabel.style.position = Position.Absolute;
                countLabel.style.right = 2;
                countLabel.style.bottom = 0;
                countLabel.style.fontSize = 10;
                countLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                countLabel.style.color = new Color(0.23f, 0.16f, 0.09f);
                slot.Add(countLabel);

                slot.RegisterCallback<MouseDownEvent>(evt => OnCookingInventorySlotMouseDown(inventoryIndex, slot, evt));
            }

            _craftingInventoryGrid.Add(slot);
        }
    }

    private void OnCookingInventorySlotMouseDown(int inventoryIndex, VisualElement slotElement, MouseDownEvent evt)
    {
        if (_slotsData == null || inventoryIndex < 0 || inventoryIndex >= _slotsData.Length)
            return;

        if (_slotsData[inventoryIndex].item == null || _slotsData[inventoryIndex].amount <= 0)
            return;

        _draggedCookingInventoryIndex = inventoryIndex;
        _isDraggingFromCookingInventory = true;

        if (slotElement != null)
            slotElement.style.opacity = 0.5f;

        evt.StopPropagation();
    }

    private void OnCookingIngredientSlotMouseUp(int ingredientSlotIndex, MouseUpEvent evt)
    {
        if (!_isDraggingFromCookingInventory || _draggedCookingInventoryIndex < 0)
            return;

        if (_slotsData == null || _cookingRecipeSlotData == null || _selectedRecipe == null)
            return;

        if (_selectedRecipe.ingredients == null)
            return;

        if (ingredientSlotIndex < 0 || ingredientSlotIndex >= _selectedRecipe.ingredients.Length)
            return;

        var draggedStack = _slotsData[_draggedCookingInventoryIndex];
        if (draggedStack.item == null || draggedStack.amount <= 0)
        {
            ResetCookingDragState();
            return;
        }

        var requiredIngredient = _selectedRecipe.ingredients[ingredientSlotIndex];
        var placedStack = _cookingRecipeSlotData[ingredientSlotIndex];

        // Wrong item
        if (requiredIngredient.item == null || draggedStack.item != requiredIngredient.item)
        {
            Debug.Log("Wrong ingredient for this slot.");
            ResetCookingDragState();
            evt.StopPropagation();
            return;
        }

        // Already full
        if (placedStack.amount >= requiredIngredient.amount)
        {
            Debug.Log("This ingredient slot is already full.");
            ResetCookingDragState();
            evt.StopPropagation();
            return;
        }

        // Remove 1 from inventory
        _slotsData[_draggedCookingInventoryIndex].amount -= 1;
        if (_slotsData[_draggedCookingInventoryIndex].amount <= 0)
            _slotsData[_draggedCookingInventoryIndex] = new ItemStack { item = null, amount = 0 };

        // Add 1 to recipe slot
        _cookingRecipeSlotData[ingredientSlotIndex].item = requiredIngredient.item;
        _cookingRecipeSlotData[ingredientSlotIndex].amount += 1;

        RefreshInventorySlot(_draggedCookingInventoryIndex);
        PopulateCraftingInventoryFromRealData();
        RefreshCookingIngredientSlotVisual(ingredientSlotIndex);

        ResetCookingDragState();
        evt.StopPropagation();
    }
    private void RefreshCookingIngredientSlotVisual(int index)
    {
        if (_selectedRecipe == null || _selectedRecipe.ingredients == null)
            return;

        if (_cookingIngredientSlotElements == null || _cookingRecipeSlotData == null)
            return;

        if (index < 0 || index >= _selectedRecipe.ingredients.Length)
            return;

        var slot = _cookingIngredientSlotElements[index];
        if (slot == null)
            return;

        var required = _selectedRecipe.ingredients[index];
        var placed = _cookingRecipeSlotData[index];

        slot.Clear();
        slot.style.backgroundImage = StyleKeyword.None;
        slot.style.position = Position.Relative;

        bool isFilled = placed.amount >= required.amount;

        // muted when not full, normal when full
        slot.style.backgroundColor = isFilled
            ? new Color(0.95f, 0.88f, 0.72f, 1f)
            : new Color(0.72f, 0.72f, 0.72f, 0.85f);

        slot.style.borderLeftWidth = 2;
        slot.style.borderRightWidth = 2;
        slot.style.borderTopWidth = 2;
        slot.style.borderBottomWidth = 2;
        slot.style.borderLeftColor = new Color(0.78f, 0.65f, 0.48f, 1f);
        slot.style.borderRightColor = new Color(0.78f, 0.65f, 0.48f, 1f);
        slot.style.borderTopColor = new Color(0.90f, 0.81f, 0.63f, 1f);
        slot.style.borderBottomColor = new Color(0.61f, 0.42f, 0.23f, 1f);

        if (required.item != null && required.item.icon != null)
            slot.style.backgroundImage = new StyleBackground(required.item.icon);

        Label countLabel = new Label($"{placed.amount}/{required.amount}");
        countLabel.style.position = Position.Absolute;
        countLabel.style.right = 2;
        countLabel.style.bottom = 0;
        countLabel.style.fontSize = 10;
        countLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        countLabel.style.color = new Color(0.23f, 0.16f, 0.09f);
        slot.Add(countLabel);
    }

    private void RefreshCookingIngredientSlotAsFilled(int index)
    {
        if (_cookingIngredientSlotElements == null || _cookingRecipeSlotData == null || _selectedRecipe == null)
            return;

        if (index < 0 || index >= _cookingIngredientSlotElements.Length)
            return;

        var slot = _cookingIngredientSlotElements[index];
        if (slot == null)
            return;

        slot.Clear();
        slot.style.backgroundImage = StyleKeyword.None;
        slot.style.backgroundColor = new Color(0.78f, 0.92f, 0.72f, 1f);

        var stack = _cookingRecipeSlotData[index];
        var required = _selectedRecipe.ingredients[index];

        if (stack.item != null && stack.item.icon != null)
            slot.style.backgroundImage = new StyleBackground(stack.item.icon);

        Label amountLabel = new Label(required.amount.ToString());
        amountLabel.style.position = Position.Absolute;
        amountLabel.style.right = 2;
        amountLabel.style.bottom = 0;
        amountLabel.style.fontSize = 10;
        amountLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        amountLabel.style.color = new Color(0.23f, 0.16f, 0.09f);
        slot.Add(amountLabel);
    }
    private void RefreshCookingIngredientSlot(int index)
    {
        if (_cookingIngredientSlotElements == null || _cookingRecipeSlotData == null)
            return;

        if (index < 0 || index >= _cookingIngredientSlotElements.Length)
            return;

        VisualElement slot = _cookingIngredientSlotElements[index];
        if (slot == null)
            return;

        slot.Clear();
        slot.style.backgroundImage = StyleKeyword.None;

        var stack = _cookingRecipeSlotData[index];

        if (stack.item == null || stack.amount <= 0)
            return;

        if (stack.item.icon != null)
            slot.style.backgroundImage = new StyleBackground(stack.item.icon);

        Label countLabel = new Label(stack.amount.ToString());
        countLabel.style.position = Position.Absolute;
        countLabel.style.right = 2;
        countLabel.style.bottom = 0;
        countLabel.style.fontSize = 10;
        countLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        countLabel.style.color = new Color(0.23f, 0.16f, 0.09f);
        slot.Add(countLabel);
    }
    private void ResetCookingDragState()
    {
        _draggedCookingInventoryIndex = -1;
        _isDraggingFromCookingInventory = false;

        PopulateCraftingInventoryFromRealData();
    }
    private void PopulateFakeCraftingInventory()
    {
        if (_craftingInventoryGrid == null)
            return;

        _craftingInventoryGrid.Clear();

        for (int i = 0; i < 18; i++)
        {
            VisualElement slot = new VisualElement();
            slot.style.width = 36;
            slot.style.height = 36;
            slot.style.backgroundColor = new Color(0.96f, 0.91f, 0.82f, 1f);
            slot.style.borderLeftWidth = 2;
            slot.style.borderRightWidth = 2;
            slot.style.borderTopWidth = 2;
            slot.style.borderBottomWidth = 2;
            slot.style.borderLeftColor = new Color(0.78f, 0.65f, 0.48f, 1f);
            slot.style.borderRightColor = new Color(0.78f, 0.65f, 0.48f, 1f);
            slot.style.borderTopColor = new Color(0.90f, 0.81f, 0.63f, 1f);
            slot.style.borderBottomColor = new Color(0.61f, 0.42f, 0.23f, 1f);
            slot.style.marginRight = 6;
            slot.style.marginBottom = 6;

            _craftingInventoryGrid.Add(slot);
        }
    }

    private void CookSelectedRecipe()
    {
        if (_isCooking)
            return;

        if (_selectedRecipe == null || _selectedRecipe.ingredients == null || _cookingRecipeSlotData == null)
            return;

        for (int i = 0; i < _selectedRecipe.ingredients.Length; i++)
        {
            if (_cookingRecipeSlotData[i].item != _selectedRecipe.ingredients[i].item)
            {
                Debug.Log("Wrong ingredient in slot.");
                return;
            }

            if (_cookingRecipeSlotData[i].amount < _selectedRecipe.ingredients[i].amount)
            {
                Debug.Log("Not all ingredients are placed.");
                return;
            }
        }

        StartCoroutine(AnimateCookingProcess());
    }
    private System.Collections.IEnumerator AnimateCookingProcess()
    {
        _isCooking = true;

        if (_cookRecipeButton != null)
            _cookRecipeButton.SetEnabled(false);

        if (_backToRecipesButton != null)
            _backToRecipesButton.SetEnabled(false);

        if (_cookingLoadingContainer != null)
            _cookingLoadingContainer.style.display = DisplayStyle.Flex;

        if (_cookingLoadingLabel != null)
            _cookingLoadingLabel.text = "Preparing meal...";

        if (_cookingLoadingBarFill != null)
            _cookingLoadingBarFill.style.width = new Length(0, LengthUnit.Percent);

        if (_cookingProgressText != null)
            _cookingProgressText.text = "0%";

        float duration = 1.8f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            int percent = Mathf.RoundToInt(t * 100f);

            if (_cookingLoadingBarFill != null)
                _cookingLoadingBarFill.style.width = new Length(percent, LengthUnit.Percent);

            if (_cookingProgressText != null)
                _cookingProgressText.text = percent + "%";

            if (_cookingLoadingLabel != null)
            {
                if (t < 0.33f)
                    _cookingLoadingLabel.text = "Preparing meal...";
                else if (t < 0.66f)
                    _cookingLoadingLabel.text = "Cooking...";
                else
                    _cookingLoadingLabel.text = "Plating...";
            }

            yield return null;
        }

        if (_selectedRecipe.result != null)
            TryAdd(_selectedRecipe.result, _selectedRecipe.resultAmount);

        for (int i = 0; i < _cookingRecipeSlotData.Length; i++)
        {
            _cookingRecipeSlotData[i] = new ItemStack { item = null, amount = 0 };
            RefreshCookingIngredientSlotVisual(i);
        }

        PopulateCraftingInventoryFromRealData();
        RefreshAllSlots();

        if (_cookingLoadingLabel != null)
            _cookingLoadingLabel.text = "Done!";

        if (_cookingProgressText != null)
            _cookingProgressText.text = "100%";

        yield return new WaitForSeconds(0.35f);

        if (_cookingLoadingContainer != null)
            _cookingLoadingContainer.style.display = DisplayStyle.None;

        if (_cookRecipeButton != null)
            _cookRecipeButton.SetEnabled(true);

        if (_backToRecipesButton != null)
            _backToRecipesButton.SetEnabled(true);

        _isCooking = false;

        Debug.Log($"Cooked {_selectedRecipe.recipeName}!");
    }
    private void ReturnPlacedCookingIngredientsToInventory()
    {
        if (_cookingRecipeSlotData == null)
            return;

        for (int i = 0; i < _cookingRecipeSlotData.Length; i++)
        {
            var placed = _cookingRecipeSlotData[i];

            if (placed.item != null && placed.amount > 0)
                TryAdd(placed.item, placed.amount);

            _cookingRecipeSlotData[i] = new ItemStack { item = null, amount = 0 };

            if (_cookingIngredientSlotElements != null && i < _cookingIngredientSlotElements.Length)
                RefreshCookingIngredientSlotVisual(i);
        }

        PopulateCraftingInventoryFromRealData();
        RefreshAllSlots();
    }
    private void PopulateCraftingRecipes()
    {
        if (_craftingPage == null || recipes == null || recipes.Length == 0)
            return;

        ScrollView recipeList = _craftingPage.Q<ScrollView>("recipeList");
        if (recipeList == null)
            return;

        recipeList.Clear();

        foreach (var recipe in recipes)
        {
            if (recipe == null)
                continue;

            // Create recipe item
            VisualElement recipeItem = new VisualElement();
            recipeItem.style.flexDirection = FlexDirection.Row;
            recipeItem.style.marginLeft = 6;
            recipeItem.style.marginRight = 6;
            recipeItem.style.marginTop = 6;
            recipeItem.style.marginBottom = 6;
            recipeItem.style.borderBottomWidth = 1;
            recipeItem.style.borderBottomColor = new Color(0.5f, 0.5f, 0.5f, 1);

            // Recipe icon and name
            VisualElement recipeContent = new VisualElement();
            recipeContent.style.flexGrow = 1;
            recipeContent.style.flexDirection = FlexDirection.Column;

            Label recipeName = new Label(recipe.recipeName);
            recipeName.style.fontSize = 12;
            recipeName.style.unityFontStyleAndWeight = FontStyle.Bold;
            recipeName.style.marginBottom = 4;
            recipeContent.Add(recipeName);

            // Ingredients list
            VisualElement ingredientsList = new VisualElement();
            ingredientsList.style.flexDirection = FlexDirection.Row;
            ingredientsList.style.flexWrap = Wrap.Wrap;

            if (recipe.ingredients != null)
            {
                foreach (var ingredient in recipe.ingredients)
                {
                    if (ingredient.item == null)
                        continue;

                    VisualElement ingredientItem = new VisualElement();
                    ingredientItem.style.width = 32;
                    ingredientItem.style.height = 32;
                    ingredientItem.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1);
                    ingredientItem.style.marginRight = 4;
                    ingredientItem.style.marginBottom = 4;

                    if (ingredient.item.icon != null)
                        ingredientItem.style.backgroundImage = ingredient.item.icon.texture;

                    Label ingredientAmount = new Label(ingredient.amount.ToString());
                    ingredientAmount.style.position = Position.Absolute;
                    ingredientAmount.style.right = 2;
                    ingredientAmount.style.bottom = 2;
                    ingredientAmount.style.fontSize = 10;
                    ingredientAmount.style.color = Color.white;
                    ingredientItem.Add(ingredientAmount);

                    ingredientsList.Add(ingredientItem);
                }
            }

            recipeContent.Add(ingredientsList);

            // Result
            Label result = new Label($"→ {recipe.resultAmount}x {recipe.result.name}");
            result.style.fontSize = 11;
            result.style.marginBottom = 4;
            recipeContent.Add(result);

            // Craft button
            Button craftBtn = new Button(() => AttemptCraft(recipe));
            craftBtn.text = "Craft";
            craftBtn.style.width = 70;
            craftBtn.style.height = 32;
            craftBtn.style.marginLeft = 8;

            recipeItem.Add(recipeContent);
            recipeItem.Add(craftBtn);

            recipeList.Add(recipeItem);
        }
    }


    private void AttemptCraft(RecipeDefinition recipe)
    {
        if (recipe == null || !recipe.CanCraft(_slotsData))
        {
            Debug.Log("Cannot craft recipe: missing ingredients");
            return;
        }

        recipe.Craft(ref _slotsData);

        // Refresh all inventory slots
        for (int i = 0; i < _itemSlots.Length; i++)
            RefreshInventorySlot(i);

        Debug.Log($"Crafted {recipe.recipeName}!");
    }

    // ==================== MAP DISPLAY ====================

    private void PopulateMapDisplay()
    {
        if (_mapPage == null)
            return;

        VisualElement mapContainer = _mapPage.Q<VisualElement>("mapContainer");
        if (mapContainer == null)
            return;

        mapContainer.Clear();

        if (gameMapImage != null)
        {
            // Create an image element to display the map
            VisualElement mapImage = new VisualElement();
            mapImage.style.width = new Length(100, LengthUnit.Percent);
            mapImage.style.height = new Length(100, LengthUnit.Percent);
            mapImage.style.backgroundImage = gameMapImage;
            mapContainer.Add(mapImage);
        }
        else
        {
            Label placeholder = new Label("Map image not assigned\nAssign a map texture in the inspector");
            placeholder.style.unityTextAlign = TextAnchor.MiddleCenter;
            placeholder.style.color = new Color(0.7f, 0.7f, 0.7f, 1);
            mapContainer.Add(placeholder);
        }
    }

    // ==================== FARMING SYSTEM INTEGRATION ====================

    /// <summary>
    /// Count total amount of an item in inventory (for farming system)
    /// </summary>
    public int CountItemInInventory(ItemDefinition item)
    {
        if (item == null || _slotsData == null) return 0;

        int total = 0;
        foreach (var slot in _slotsData)
        {
            if (slot.item == item)
                total += slot.amount;
        }
        return total;
    }

    /// <summary>
    /// Remove an item from inventory (for farming system - consume seeds)
    /// </summary>
    public bool TryRemoveItem(ItemDefinition item, int amount)
    {
        if (item == null || amount <= 0 || _slotsData == null)
            return false;

        int toRemove = amount;

        // Remove from slots in order
        for (int i = 0; i < _slotsData.Length && toRemove > 0; i++)
        {
            if (_slotsData[i].item == item && _slotsData[i].amount > 0)
            {
                int removed = Mathf.Min(toRemove, _slotsData[i].amount);
                _slotsData[i].amount -= removed;
                toRemove -= removed;

                if (_slotsData[i].amount <= 0)
                    _slotsData[i] = new ItemStack { item = null, amount = 0 };

                RefreshInventorySlot(i);
            }
        }

        // Check if item still exists in inventory
        int itemCountLeft = CountItemInInventory(item);

        // If item is completely gone from inventory, remove from hotbar too
        if (itemCountLeft <= 0)
        {
            for (int i = 0; i < _hotbarData.Length; i++)
            {
                if (_hotbarData[i].item == item)
                {
                    _hotbarData[i] = new ItemStack { item = null, amount = 0 };
                    RefreshHotbarSlot(i);
                    SyncExternalHotbarSlot(i);
                }
            }
        }

        // Sync hotbar display
        SyncExternalHotbarAll();

        return toRemove == 0;
    }

    private void SetFooterVisible(bool visible)
    {
        if (_playerCard != null)
            _playerCard.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        if (_trashSlot != null)
            _trashSlot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
    /// <summary>
    /// Get the item at a specific hotbar slot (for farming system)
    /// </summary>
    public ItemDefinition GetHotbarItem(int slotIndex)
    {
        if (_hotbarData == null || slotIndex < 0 || slotIndex >= _hotbarData.Length)
            return null;

        return _hotbarData[slotIndex].item;
    }
}


