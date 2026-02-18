using System;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class InventoryController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.I;

    [Header("Start State")]
    [SerializeField] private bool startOpen = false;

    [Header("Debug")]
    [SerializeField] private bool debugSlotClicks = false;

    // ---------- NEW: Inventory ----------
    [Header("Inventory")]
    [SerializeField] private int inventorySize = 36;

    [Header("Quick Test (optional)")]
    [SerializeField] private ItemDefinition testItem;
    [SerializeField] private KeyCode testAddKey = KeyCode.K;
    [SerializeField] private int testAddAmount = 1;

    private UIDocument _uiDocument;
    private VisualElement _root;
    private bool _isOpen;

    // Hotbar HUD reference
    private HotBarHUDController _hotbarHUD;

    // Cached UI references
    private Button _closeButton;

    private Button _tabToolsButton;
    private Button _tabCropsButton;
    private Button _tabCraftingButton;

    private VisualElement _toolsPage;
    private VisualElement _cropsPage;
    private VisualElement _craftingPage;

    // ---------- NEW: slot UI refs ----------
    private VisualElement[] _itemSlots; // itemSlot01..itemSlot36
    private VisualElement[] _hotbarSlots; // hotbarSlot01..hotbarSlot12

    [Serializable]
    private struct ItemStack
    {
        public ItemDefinition item;
        public int amount;
    }

    private ItemStack[] _slotsData;
    private ItemStack[] _hotbarData; // NEW: separate hotbar data

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
        _hotbarHUD = FindFirstObjectByType<HotBarHUDController>();

        CacheUI();
        CacheInventorySlots();     // NEW
        BindUI();

        _slotsData = new ItemStack[inventorySize]; // NEW
        _hotbarData = new ItemStack[12]; // NEW: hotbar data array
        RefreshAllSlots();                         // NEW

        // Apply initial state
        SetOpen(startOpen);

        // Optional: start on Tools tab if open
        ShowTools();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            Toggle();

        // quick test: press K to add testItem
        if (testItem != null && Input.GetKeyDown(testAddKey))
            TryAdd(testItem, testAddAmount);
    }

    public void Toggle() => SetOpen(!_isOpen);
    public void Open() => SetOpen(true);
    public void Close() => SetOpen(false);

    private void SetOpen(bool open)
    {
        _isOpen = open;

        // Hide hotbar when inventory opens, show when it closes
        if (_hotbarHUD != null)
            _hotbarHUD.SetVisible(!open);

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
        _tabCropsButton = _root.Q<Button>("tabCropsButton");
        _tabCraftingButton = _root.Q<Button>("tabCraftingButton");

        // Pages
        _toolsPage = _root.Q<VisualElement>("toolsPage");
        _cropsPage = _root.Q<VisualElement>("cropsPage");
        _craftingPage = _root.Q<VisualElement>("craftingPage");
    }

    // NEW: cache inventory slots itemSlot01..itemSlot36 AND hotbarSlot01..hotbarSlot12
    private void CacheInventorySlots()
    {
        _itemSlots = new VisualElement[inventorySize];
        _hotbarSlots = new VisualElement[12];

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
        for (int i = 0; i < 12; i++)
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

        // OPTIONAL: slot click debugging
        if (debugSlotClicks)
        {
            HookSlotClicks("itemSlot", inventorySize);
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

        // Swap within inventory or swap with hotbar
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
            // From hotbar to inventory
            SwapInventoryAndHotbar(_draggedSlotIndex, targetSlotIndex, false);
        }

        ResetDragState();
        evt.StopPropagation();
    }

    private void OnHotbarSlotMouseDown(int slotIndex, MouseDownEvent evt)
    {
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
            SwapInventoryAndHotbar(_draggedSlotIndex, targetSlotIndex, true);
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

        RefreshInventorySlot(source);
        RefreshInventorySlot(target);
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
        }
        else
        {
            temp = _hotbarData[slotIndex];
            _hotbarData[slotIndex] = _slotsData[otherIndex];
            _slotsData[otherIndex] = temp;

            RefreshHotbarSlot(slotIndex);
            RefreshInventorySlot(otherIndex);
        }

        if (debugSlotClicks)
            Debug.Log($"Swapped inventory and hotbar");
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
        if (_tabCropsButton != null) _tabCropsButton.clicked -= ShowCrops;
        if (_tabCraftingButton != null) _tabCraftingButton.clicked -= ShowCrafting;
    }
}
