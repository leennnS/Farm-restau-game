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
    }

    private void ShowTools() => ShowPage(_toolsPage, _mapPage, _craftingPage, _settingsPage);
    private void ShowMap() => ShowPage(_mapPage, _toolsPage, _craftingPage, _settingsPage);
    private void ShowCrafting() => ShowPage(_craftingPage, _toolsPage, _mapPage, _settingsPage);
    private void ShowSettings() => ShowPage(_settingsPage, _toolsPage, _mapPage, _craftingPage);

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


