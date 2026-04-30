using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;


[RequireComponent(typeof(UIDocument))]


public class InventoryController : MonoBehaviour
{
    private const string InventorySaveKey = "GlobalInventory";

    private static InventoryController _instance;

    public static InventoryController Instance => _instance;
    public static bool HasInstance => _instance != null;

    public const int HotbarSize = 12;

    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.I;

    [Header("Start State")]
    [SerializeField] private bool startOpen = false;

    [Header("Audio")]
    [SerializeField] private AudioClip openInventorySound;
    [SerializeField] private AudioClip closeInventorySound;
    [SerializeField] private AudioClip moveItemSound;

    [Header("Cooking Audio")]
    [SerializeField] private AudioClip openCookingSound;
    [SerializeField] private AudioClip closeCookingSound;
    [SerializeField] private AudioClip serveSound;
    [SerializeField] private AudioClip cookingLoopSound;

    [Header("Debug")]
    [SerializeField] private bool debugSlotClicks = false;
    private VisualElement _inventoryRootElement;

    // ---------- NEW: Inventory ----------
    [Header("Inventory")]
    [SerializeField] private int inventorySize = 36;
    [SerializeField, Range(0.8f, 2.0f)] private float inventoryUiScale = 1.1f;
    [SerializeField] private bool autoScaleByDevice = true;
    [SerializeField] private float referenceDpi = 96f;
    [SerializeField, Range(1.0f, 1.8f)] private float maxAutoScaleMultiplier = 1.45f;
    [SerializeField] private ItemDefinition[] knownItemDefinitions;

    [Header("Crafting")]
    [SerializeField] private bool enableInventoryCookingTab = false;
    [SerializeField] private RecipeDefinition[] recipes;

    [Header("Map")]
    [SerializeField] private Texture2D gameMapImage;

    [Header("UI")]
    [SerializeField] private StyleSheet cookingStyleSheet;

    [Header("Quick Test (optional)")]
    [SerializeField] private ItemDefinition testItem;
    [SerializeField] private KeyCode testAddKey = KeyCode.K;
    [SerializeField] private int testAddAmount = 1;

    private ItemStack[] _cookingRecipeSlotData;
    private VisualElement[] _cookingIngredientSlotElements;

    private struct CookingSourceSlot
    {
        public bool fromHotbar;
        public int slotIndex;
    }

    private readonly List<CookingSourceSlot> _cookingSourceSlots = new List<CookingSourceSlot>(HotbarSize + 36);
    private int _draggedCookingInventoryIndex = -1;
    private bool _isDraggingFromCookingInventory = false;

    private VisualElement _inventoryFooter;
    private VisualElement _playerCard;
    private UIDocument _uiDocument;
    private VisualElement _root;
    private VisualElement _boundRootForCallbacks;
    private bool _isOpen;
    private bool _isCookingOnlyMode;
    private AudioSource _audioSource;
    private bool _isCookingLoopPlaying;

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
    private Button _tabDessertButton;
    private Button _tabServeButton;
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
    private RecipeCategory _currentRecipeCategory = RecipeCategory.Breakfast;
    private bool _restaurantRecipeAutoLoadAttempted;
    private bool _isServeMode;
    private int _selectedServeQueueIndex = -1;
    private RecipeDefinition _selectedServeRecipe;
    private Label _serveStatusLabel;
    private RestaurantNpcQueueManager _restaurantQueueManager;

    public event Action<RecipeDefinition> OnRecipeCooked;

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
    private readonly Dictionary<string, ItemDefinition> _itemLookupByKey = new Dictionary<string, ItemDefinition>(StringComparer.Ordinal);
    private bool _inventorySaveDirty;

    [Serializable]
    private struct SavedStackData
    {
        public string itemKey;
        public int amount;
    }

    [Serializable]
    private class InventorySaveData
    {
        public SavedStackData[] slots;
        public SavedStackData[] hotbar;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        _uiDocument = GetComponent<UIDocument>();
        _root = _uiDocument.rootVisualElement;

        // Keep this inventory object across scene loads
        DontDestroyOnLoad(gameObject);

        EnsureAudioSource();

        var loadedStyleSheet = cookingStyleSheet;

        // Backward-compatible fallback in case the inspector field is not assigned yet.
        if (loadedStyleSheet == null)
            loadedStyleSheet = Resources.Load<StyleSheet>("UI/UXML/CookingStyles");

        if (loadedStyleSheet != null)
        {
            _root.styleSheets.Add(loadedStyleSheet);
        }

        var loadingContainer = _root.Q<VisualElement>("cookingLoadingContainer");

        TryResolveHotbarHUD();
        RebindInventoryUIIfNeeded(forceRebindCallbacks: true);

        _slotsData = new ItemStack[inventorySize];
        _hotbarData = new ItemStack[HotbarSize];

        RebuildItemLookupFromKnownItems();
        LoadInventoryData();
        RefreshAllSlots();

        SetOpen(startOpen, playSound: false);
        ShowTools();
        SyncExternalHotbarAll();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        AudioSettingsManager.Instance.SettingsChanged += HandleAudioSettingsChanged;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (AudioSettingsManager.HasInstance)
            AudioSettingsManager.Instance.SettingsChanged -= HandleAudioSettingsChanged;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Retry recipe coverage when entering Restaurant scene from a persistent inventory singleton.
        _restaurantRecipeAutoLoadAttempted = false;
        StartCoroutine(RebindAfterSceneLoad());
    }

    private System.Collections.IEnumerator RebindAfterSceneLoad()
    {
        // Let scene UI documents initialize first.
        yield return null;

        // Rebind inventory UI references if UI Toolkit rebuilt the runtime panel.
        RebindInventoryUIIfNeeded(forceRebindCallbacks: false);

        // Force re-resolve to scene-local HUD instances.
        _hotbarHUD = null;
        _hotbarController = null;
        TryResolveHotbarHUD();

        // Re-apply visibility and contents after transitions.
        SetExternalHotbarVisible(!_isOpen);
        SyncExternalHotbarAll();
        RefreshAllSlots();

        // Re-apply interaction state so hidden inventory cannot intercept clicks.
        SetOpen(_isOpen, playSound: false);
        SyncAudioSettingsFromSliders();
    }

    private void RebindInventoryUIIfNeeded(bool forceRebindCallbacks)
    {
        if (_uiDocument == null)
            _uiDocument = GetComponent<UIDocument>();

        if (_uiDocument == null)
            return;

        VisualElement newRoot = _uiDocument.rootVisualElement;
        if (newRoot == null)
            return;

        bool rootChanged = !ReferenceEquals(_root, newRoot);
        _root = newRoot;

        CacheUI();
        ApplyInventoryUiScale();
        CacheInventorySlots();

        if (forceRebindCallbacks || rootChanged || !ReferenceEquals(_boundRootForCallbacks, _root))
        {
            BindUI();
            _boundRootForCallbacks = _root;
        }
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

        if (_inventorySaveDirty)
            SaveInventoryData();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SaveInventoryData();
    }

    private void OnApplicationQuit()
    {
        SaveInventoryData();
    }

    private void MarkInventoryDirty()
    {
        _inventorySaveDirty = true;
    }

    public void SaveInventoryData()
    {
        if (_slotsData == null || _hotbarData == null)
            return;

        InventorySaveData data = new InventorySaveData
        {
            slots = BuildSavedStacks(_slotsData),
            hotbar = BuildSavedStacks(_hotbarData)
        };

        PlayerPrefs.SetString(InventorySaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
        _inventorySaveDirty = false;
    }

    private SavedStackData[] BuildSavedStacks(ItemStack[] source)
    {
        SavedStackData[] result = new SavedStackData[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            ItemStack stack = source[i];
            result[i] = new SavedStackData
            {
                itemKey = stack.item != null ? GetItemKey(stack.item) : string.Empty,
                amount = stack.item != null ? Mathf.Max(0, stack.amount) : 0
            };
        }

        return result;
    }

    public void LoadInventoryData()
    {
        if (!PlayerPrefs.HasKey(InventorySaveKey))
            return;

        string json = PlayerPrefs.GetString(InventorySaveKey, string.Empty);
        if (string.IsNullOrEmpty(json))
            return;

        InventorySaveData data = JsonUtility.FromJson<InventorySaveData>(json);
        if (data == null)
            return;

        ApplyLoadedStacks(data.slots, _slotsData);
        ApplyLoadedStacks(data.hotbar, _hotbarData);
    }

    private void ApplyLoadedStacks(SavedStackData[] source, ItemStack[] target)
    {
        if (target == null)
            return;

        for (int i = 0; i < target.Length; i++)
            target[i] = new ItemStack { item = null, amount = 0 };

        if (source == null)
            return;

        int max = Mathf.Min(source.Length, target.Length);
        for (int i = 0; i < max; i++)
        {
            SavedStackData saved = source[i];
            if (string.IsNullOrEmpty(saved.itemKey) || saved.amount <= 0)
                continue;

            ItemDefinition item = TryResolveItem(saved.itemKey);
            if (item == null)
                continue;

            target[i] = new ItemStack { item = item, amount = Mathf.Max(1, saved.amount) };
        }
    }

    private void RebuildItemLookupFromKnownItems()
    {
        _itemLookupByKey.Clear();

        if (knownItemDefinitions != null)
        {
            for (int i = 0; i < knownItemDefinitions.Length; i++)
                RegisterItemForLookup(knownItemDefinitions[i]);
        }

        if (recipes != null)
        {
            for (int i = 0; i < recipes.Length; i++)
            {
                RecipeDefinition recipe = recipes[i];
                if (recipe == null)
                    continue;

                RegisterItemForLookup(recipe.result);

                if (recipe.ingredients == null)
                    continue;

                for (int j = 0; j < recipe.ingredients.Length; j++)
                    RegisterItemForLookup(recipe.ingredients[j].item);
            }
        }

        RegisterItemForLookup(testItem);
    }

    private void RegisterItemForLookup(ItemDefinition item)
    {
        if (item == null)
            return;

        string key = GetItemKey(item);
        if (string.IsNullOrEmpty(key))
            return;

        if (!_itemLookupByKey.ContainsKey(key))
            _itemLookupByKey.Add(key, item);
    }

    private ItemDefinition TryResolveItem(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        if (_itemLookupByKey.TryGetValue(key, out ItemDefinition item))
            return item;

        return null;
    }

    private static string GetItemKey(ItemDefinition item)
    {
        if (item == null)
            return string.Empty;

        if (!string.IsNullOrEmpty(item.displayName))
            return item.displayName;

        return item.name;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Keep known item list populated so runtime save-load can resolve items by key.
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemDefinition", new[] { "Assets/Items/itemDefinition", "Assets/Resources/Prefabs" });
        if (guids == null || guids.Length == 0)
            return;

        List<ItemDefinition> found = new List<ItemDefinition>(guids.Length);
        for (int i = 0; i < guids.Length; i++)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            ItemDefinition item = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (item != null)
                found.Add(item);
        }

        knownItemDefinitions = found.ToArray();
    }
#endif

    public bool IsCookingOnlyModeOpen => _isOpen && _isCookingOnlyMode;

    public void Toggle() => SetOpen(!_isOpen);
    public void Open() => SetOpen(true);
    public void Close()
    {
        _isCookingOnlyMode = false;
        RestoreDefaultLayout();
        SetOpen(false);
    }

    public void OpenCookingOnlyMode()
    {
        OpenCookingOnlyMode(false);
    }

    public void OpenCookingOnlyMode(bool playSound)
    {
        _isCookingOnlyMode = true;
        ApplyCookingOnlyLayout();
        ApplyInventoryUiScale();
        SetOpen(true, playSound: false);

        if (playSound)
            PlayInventorySound(openCookingSound);
    }

    public void CloseCookingOnlyMode()
    {
        CloseCookingOnlyMode(false);
    }

    public void CloseCookingOnlyMode(bool playSound)
    {
        _isCookingOnlyMode = false;
        RestoreDefaultLayout();
        ApplyInventoryUiScale();
        SetOpen(false, playSound: false);

        if (playSound)
            PlayInventorySound(closeCookingSound);
    }

    private void ApplyCookingOnlyLayout()
    {
        if (_tabToolsButton != null)
            _tabToolsButton.style.display = DisplayStyle.None;

        if (_tabMapButton != null)
            _tabMapButton.style.display = DisplayStyle.None;

        if (_tabSettingsButton != null)
            _tabSettingsButton.style.display = DisplayStyle.None;

        if (_tabCraftingButton != null)
            _tabCraftingButton.style.display = DisplayStyle.Flex;

        ShowPage(_craftingPage, _toolsPage, _mapPage, _settingsPage);
        SetFooterVisible(false);
        ShowRecipeCategory(_currentRecipeCategory);
        UpdateActiveTopTab(_tabCraftingButton);
    }

    private void RestoreDefaultLayout()
    {
        if (_tabToolsButton != null)
            _tabToolsButton.style.display = DisplayStyle.Flex;

        if (_tabMapButton != null)
            _tabMapButton.style.display = DisplayStyle.None;

        if (_tabSettingsButton != null)
            _tabSettingsButton.style.display = DisplayStyle.Flex;

        if (_tabCraftingButton != null)
            _tabCraftingButton.style.display = enableInventoryCookingTab ? DisplayStyle.Flex : DisplayStyle.None;

        if (_isOpen)
            ShowTools();
    }

    private void SetOpen(bool open, bool playSound = true)
    {
        bool stateChanged = _isOpen != open;
        _isOpen = open;

        TryResolveHotbarHUD();

        SetExternalHotbarVisible(!open);

        SyncExternalHotbarAll();

        // Re-resolve in case UI was rebuilt across scene transitions.
        if (_inventoryRootElement == null && _root != null)
            _inventoryRootElement = _root.Q<VisualElement>("inventoryRoot");

        // Prefer inventoryRoot, but fall back to root so hidden inventory cannot steal clicks.
        VisualElement interactionRoot = _inventoryRootElement != null ? _inventoryRootElement : _root;
        if (interactionRoot != null)
        {
            interactionRoot.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            interactionRoot.pickingMode = open ? PickingMode.Position : PickingMode.Ignore;
        }

        // Extra safety: when closed, root should not eat pointer events.
        if (_root != null && !open)
            _root.pickingMode = PickingMode.Ignore;
        else if (_root != null)
            _root.pickingMode = PickingMode.Position;

        if (!stateChanged || !playSound)
            return;

        if (open)
            PlayInventorySound(openInventorySound);
        else
            PlayInventorySound(closeInventorySound);
    }

    private void EnsureAudioSource()
    {
        if (_audioSource == null)
            _audioSource = GetComponent<AudioSource>();

        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        _audioSource.spatialBlend = 0f;
    }

    private void PlayInventorySound(AudioClip clip)
    {
        if (_audioSource == null || clip == null)
            return;

        _audioSource.PlayOneShot(clip);
    }

    private void PlayMoveItemSound()
    {
        PlayInventorySound(moveItemSound);
    }

    private void PlayServeSound()
    {
        PlayInventorySound(serveSound);
    }

    private void StartCookingLoopSound()
    {
        if (_isCookingLoopPlaying || _audioSource == null || cookingLoopSound == null)
            return;

        _audioSource.clip = cookingLoopSound;
        _audioSource.loop = true;
        _audioSource.Play();
        _isCookingLoopPlaying = true;
    }

    private void StopCookingLoopSound()
    {
        if (!_isCookingLoopPlaying || _audioSource == null)
            return;

        if (_audioSource.clip == cookingLoopSound)
            _audioSource.Stop();

        _audioSource.loop = false;

        if (_audioSource.clip == cookingLoopSound)
            _audioSource.clip = null;

        _isCookingLoopPlaying = false;
    }


    private void UpdateActiveTopTab(Button activeButton)
    {
        _tabToolsButton?.RemoveFromClassList("active-top-tab");
        _tabMapButton?.RemoveFromClassList("active-top-tab");
        _tabCraftingButton?.RemoveFromClassList("active-top-tab");
        _tabSettingsButton?.RemoveFromClassList("active-top-tab");

        activeButton?.AddToClassList("active-top-tab");
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

        if (_tabMapButton != null)
            _tabMapButton.style.display = DisplayStyle.None;

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
        _tabDessertButton = _root.Q<Button>("tabDessertButton");
        EnsureServeTabButton();
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
        _inventoryRootElement = _root.Q<VisualElement>("inventoryRoot");
        EnsureServeStatusLabel();

        if (!enableInventoryCookingTab)
        {
            if (_tabCraftingButton != null)
                _tabCraftingButton.style.display = DisplayStyle.None;

            if (_craftingPage != null)
                _craftingPage.style.display = DisplayStyle.None;
        }
    }

    private void EnsureServeTabButton()
    {
        if (_tabServeButton != null)
            return;

        VisualElement tabRow = _tabDessertButton != null ? _tabDessertButton.parent : null;
        if (tabRow == null)
            return;

        _tabServeButton = new Button { name = "tabServeButton", text = "Serve" };
        _tabServeButton.AddToClassList("category-tab");
        tabRow.Add(_tabServeButton);
    }

    private void EnsureServeStatusLabel()
    {
        if (_serveStatusLabel != null || _recipeBrowserView == null)
            return;

        _serveStatusLabel = new Label();
        _serveStatusLabel.name = "serveStatusLabel";
        _serveStatusLabel.style.fontSize = 13;
        _serveStatusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        _serveStatusLabel.style.color = new Color(0.23f, 0.16f, 0.09f, 1f);
        _serveStatusLabel.style.marginBottom = 8;
        _serveStatusLabel.style.display = DisplayStyle.None;
        _recipeBrowserView.Insert(0, _serveStatusLabel);
    }

    private void ApplyInventoryUiScale()
    {
        if (_root == null)
            return;

        VisualElement inventoryShell = _root.Q<VisualElement>("inventoryShell");
        if (inventoryShell == null)
            return;

        float targetScale = Mathf.Clamp(inventoryUiScale, 0.8f, 2.0f);

        if (autoScaleByDevice)
        {
            // Screen.dpi is the best indicator for high-density laptop displays.
            // If unavailable (0), we simply keep the manual scale value.
            float dpi = Screen.dpi;
            if (dpi > 0f)
            {
                float safeReferenceDpi = Mathf.Max(1f, referenceDpi);
                float dpiMultiplier = Mathf.Clamp(dpi / safeReferenceDpi, 1f, maxAutoScaleMultiplier);
                targetScale *= dpiMultiplier;
            }
        }

        float clampedScale = Mathf.Clamp(targetScale, 0.8f, 2.0f);
        inventoryShell.style.scale = new Scale(new Vector2(clampedScale, clampedScale));
    }

    // NEW: cache inventory slots itemSlot01..itemSlot36 AND hotbarSlot01..hotbarSlot12
    private void CacheInventorySlots()
    {
        if (_root == null)
        {
            return;
        }

        _itemSlots = new VisualElement[inventorySize];
        _hotbarSlots = new VisualElement[HotbarSize];

        int foundInventorySlots = 0;
        // Cache inventory grid slots
        for (int i = 0; i < inventorySize; i++)
        {
            string name = $"itemSlot{(i + 1):00}";
            _itemSlots[i] = _root.Q<VisualElement>(name);

            if (_itemSlots[i] != null)
            {
                foundInventorySlots++;
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


        int foundHotbarSlots = 0;
        // Cache hotbar slots separately
        for (int i = 0; i < HotbarSize; i++)
        {
            string name = $"hotbarSlot{(i + 1):00}";
            _hotbarSlots[i] = _root.Q<VisualElement>(name);

            if (_hotbarSlots[i] != null)
            {
                foundHotbarSlots++;
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

        if (enableInventoryCookingTab && _tabCraftingButton != null)
            _tabCraftingButton.clicked += ShowCrafting;

        if (_tabSettingsButton != null)
            _tabSettingsButton.clicked += ShowSettings;

        // Settings controls
        if (_masterVolumeSlider != null)
            _masterVolumeSlider.RegisterValueChangedCallback(OnMasterVolumeSliderChanged);

        if (_musicVolumeSlider != null)
            _musicVolumeSlider.RegisterValueChangedCallback(OnMusicVolumeSliderChanged);

        if (_sfxVolumeSlider != null)
            _sfxVolumeSlider.RegisterValueChangedCallback(OnSFXVolumeSliderChanged);

        SyncAudioSettingsFromSliders();

        if (_exitButton != null)
            _exitButton.clicked += ExitToMenu;

        if (_quitButton != null)
            _quitButton.clicked += QuitGame;

        // Populate crafting recipes
        if (enableInventoryCookingTab)
            PopulateCraftingRecipes();

        // Populate map display
        PopulateMapDisplay();
        if (_tabBreakfastButton != null)
            _tabBreakfastButton.clicked += () => ShowRecipeCategory(RecipeCategory.Breakfast);

        if (_tabMainDishButton != null)
            _tabMainDishButton.clicked += () => ShowRecipeCategory(RecipeCategory.MainDishes);

        if (_tabDrinksButton != null)
            _tabDrinksButton.clicked += () => ShowRecipeCategory(RecipeCategory.DrinksSmoothies);

        if (_tabDessertButton != null)
            _tabDessertButton.clicked += () => ShowRecipeCategory(RecipeCategory.BakeryDesserts);

        if (_tabServeButton != null)
            _tabServeButton.clicked += ShowServeTab;

        if (_backToRecipesButton != null)
            _backToRecipesButton.clicked += ShowRecipeBrowser;

        if (_cookRecipeButton != null)
            _cookRecipeButton.clicked += OnCookOrServePressed;
    }

    private void ShowTools()
    {
        ShowPage(_toolsPage, _mapPage, _craftingPage, _settingsPage);
        SetFooterVisible(true);
        UpdateActiveTopTab(_tabToolsButton);
    }

    private void ShowMap()
    {
        ShowPage(_mapPage, _toolsPage, _craftingPage, _settingsPage);
        SetFooterVisible(true);
        UpdateActiveTopTab(_tabMapButton);
    }

    private void ShowCrafting()
    {
        if (!enableInventoryCookingTab)
        {
            ShowTools();
            return;
        }

        ShowPage(_craftingPage, _toolsPage, _mapPage, _settingsPage);
        SetFooterVisible(false);
        if (_isServeMode)
            ShowServeTab();
        else
            ShowRecipeCategory(_currentRecipeCategory);
        UpdateActiveTopTab(_tabCraftingButton);
    }

    private void OnCookOrServePressed()
    {
        if (_isServeMode)
        {
            PlayServeSound();
            TryServeSelectedOrder();
        }
        else
            CookSelectedRecipe();
    }

    private void ShowSettings()
    {
        ShowPage(_settingsPage, _toolsPage, _mapPage, _craftingPage);
        SetFooterVisible(true);
        UpdateActiveTopTab(_tabSettingsButton);
        SyncAudioSettingsFromSliders();
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

        AudioSettingsManager.Instance.SetMasterVolumeNormalized(value / 100f);
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (_musicVolumeLabel != null)
            _musicVolumeLabel.text = ((int)value).ToString();

        AudioSettingsManager.Instance.SetMusicVolumeNormalized(value / 100f);
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (_sfxVolumeLabel != null)
            _sfxVolumeLabel.text = ((int)value).ToString();

        AudioSettingsManager.Instance.SetSfxVolumeNormalized(value / 100f);
    }

    private void SyncAudioSettingsFromSliders()
    {
        AudioSettingsManager audioSettings = AudioSettingsManager.Instance;

        if (_masterVolumeSlider != null)
            _masterVolumeSlider.SetValueWithoutNotify(audioSettings.MasterVolumeNormalized * 100f);

        if (_musicVolumeSlider != null)
            _musicVolumeSlider.SetValueWithoutNotify(audioSettings.MusicVolumeNormalized * 100f);

        if (_sfxVolumeSlider != null)
            _sfxVolumeSlider.SetValueWithoutNotify(audioSettings.SfxVolumeNormalized * 100f);

        if (_masterVolumeLabel != null)
            _masterVolumeLabel.text = Mathf.RoundToInt(audioSettings.MasterVolumeNormalized * 100f).ToString();

        if (_musicVolumeLabel != null)
            _musicVolumeLabel.text = Mathf.RoundToInt(audioSettings.MusicVolumeNormalized * 100f).ToString();

        if (_sfxVolumeLabel != null)
            _sfxVolumeLabel.text = Mathf.RoundToInt(audioSettings.SfxVolumeNormalized * 100f).ToString();
    }

    private void HandleAudioSettingsChanged()
    {
        SyncAudioSettingsFromSliders();
    }

    private void ExitToMenu()
    {
        StopCookingLoopSound();
        SaveInventoryData();
        SetOpen(false, playSound: false);

        CleanupGameplaySystemsForMainMenu();
        SceneManager.LoadScene("MAIN MENU", LoadSceneMode.Single);
    }

    private void CleanupGameplaySystemsForMainMenu()
    {
        Time.timeScale = 1f;

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
    }

    private static void DestroyPersistentObjects<T>() where T : MonoBehaviour
    {
        T[] instances = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (instances == null)
            return;

        for (int i = 0; i < instances.Length; i++)
        {
            T instance = instances[i];
            if (instance == null)
                continue;

            GameObject target = instance.gameObject;
            if (target == null)
                continue;

            target.SetActive(false);
            Destroy(target);
        }
    }

    private void QuitGame()
    {

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

        bool changed = false;

        // 1) stack into existing
        for (int i = 0; i < _slotsData.Length && amount > 0; i++)
        {
            if (_slotsData[i].item == item && _slotsData[i].amount < item.maxStack)
            {
                int canAdd = item.maxStack - _slotsData[i].amount;
                int addNow = Mathf.Min(canAdd, amount);

                _slotsData[i].amount += addNow;
                amount -= addNow;
                changed = true;

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
                changed = true;

                RefreshSlot(i);
            }
        }

        if (changed)
            MarkInventoryDirty();

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
            countLabel.pickingMode = PickingMode.Ignore;  // Allow mouse events to pass through to parent slot
            slotVE.Add(countLabel);
        }

        countLabel.text = text;
    }

    // ----------------------------
    // Drag and Drop Handlers (NEW)
    // ----------------------------

    private void OnInventorySlotMouseDown(int slotIndex, MouseDownEvent evt)
    {


        if (_slotsData[slotIndex].item == null || _slotsData[slotIndex].amount <= 0)
        {

            return;
        }

        _draggedSlotIndex = slotIndex;
        _draggedSlotElement = _itemSlots[slotIndex];
        _isDragging = true;
        _isDraggingFromHotbar = false;

        _draggedSlotElement.style.opacity = 0.5f;

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
            }
        }
        else
        {
            // From inventory to hotbar
            MoveInventoryToHotbar(_draggedSlotIndex, targetSlotIndex);
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
            MarkInventoryDirty();
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

            MarkInventoryDirty();
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
        MarkInventoryDirty();
        PlayMoveItemSound();
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
        MarkInventoryDirty();
        PlayMoveItemSound();
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

        MarkInventoryDirty();
        PlayMoveItemSound();
    }

    private void MoveInventoryToHotbar(int inventoryIndex, int hotbarIndex)
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
                return; // Item already in hotbar, don't add again
            }
        }

        // Only add if target slot is empty
        if (_hotbarData[hotbarIndex].item != null)
        {
            return; // Target slot is occupied, don't overwrite
        }

        _hotbarData[hotbarIndex] = new ItemStack
        {
            item = source.item,
            amount = source.amount // Transfer full stack amount
        };

        // MOVE operation: clear source inventory slot.
        _slotsData[inventoryIndex] = new ItemStack { item = null, amount = 0 };

        RefreshInventorySlot(inventoryIndex);
        RefreshHotbarSlot(hotbarIndex);
        SyncExternalHotbarSlot(hotbarIndex);
        MarkInventoryDirty();
        PlayMoveItemSound();
    }

    private void MoveHotbarToInventory(int hotbarIndex, int inventoryIndex)
    {
        if (hotbarIndex < 0 || hotbarIndex >= _hotbarData.Length) return;
        if (inventoryIndex < 0 || inventoryIndex >= _slotsData.Length) return;

        var source = _hotbarData[hotbarIndex];
        if (source.item == null || source.amount <= 0) return;

        // Prefer merging into existing stack if present.
        for (int i = 0; i < _slotsData.Length; i++)
        {
            if (_slotsData[i].item == source.item)
            {
                _slotsData[i].amount += source.amount;
                _hotbarData[hotbarIndex] = new ItemStack { item = null, amount = 0 };

                RefreshInventorySlot(i);
                RefreshHotbarSlot(hotbarIndex);
                SyncExternalHotbarSlot(hotbarIndex);
                MarkInventoryDirty();
                PlayMoveItemSound();
                return;
            }
        }

        // Only add if target slot is empty
        if (_slotsData[inventoryIndex].item != null)
        {
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
        MarkInventoryDirty();
        PlayMoveItemSound();
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
        StopCookingLoopSound();

        if (AudioSettingsManager.HasInstance)
            AudioSettingsManager.Instance.SettingsChanged -= HandleAudioSettingsChanged;

        if (_closeButton != null) _closeButton.clicked -= Close;

        if (_tabToolsButton != null) _tabToolsButton.clicked -= ShowTools;
        if (_tabMapButton != null) _tabMapButton.clicked -= ShowMap;
        if (_tabCraftingButton != null) _tabCraftingButton.clicked -= ShowCrafting;
        if (_tabSettingsButton != null) _tabSettingsButton.clicked -= ShowSettings;
        if (_tabServeButton != null) _tabServeButton.clicked -= ShowServeTab;

        if (_masterVolumeSlider != null)
            _masterVolumeSlider.UnregisterValueChangedCallback(OnMasterVolumeSliderChanged);
        if (_musicVolumeSlider != null)
            _musicVolumeSlider.UnregisterValueChangedCallback(OnMusicVolumeSliderChanged);
        if (_sfxVolumeSlider != null)
            _sfxVolumeSlider.UnregisterValueChangedCallback(OnSFXVolumeSliderChanged);

        if (_exitButton != null)
            _exitButton.clicked -= ExitToMenu;
        if (_quitButton != null)
            _quitButton.clicked -= QuitGame;

        if (_cookRecipeButton != null)
            _cookRecipeButton.clicked -= OnCookOrServePressed;
    }

    private void OnMasterVolumeSliderChanged(ChangeEvent<float> evt)
    {
        OnMasterVolumeChanged(evt.newValue);
    }

    private void OnMusicVolumeSliderChanged(ChangeEvent<float> evt)
    {
        OnMusicVolumeChanged(evt.newValue);
    }

    private void OnSFXVolumeSliderChanged(ChangeEvent<float> evt)
    {
        OnSFXVolumeChanged(evt.newValue);
    }

    // ==================== CRAFTING SYSTEM ====================
    private void ShowRecipeCategory(RecipeCategory category)
    {
        EnsureRestaurantRecipeCoverageIfNeeded();
        _isServeMode = false;
        _currentRecipeCategory = category;
        Debug.Log($"Switching to recipe category: {category}");
        SetServeStatus(string.Empty);
        UpdateActiveCookingSubTab();
        ShowRecipeBrowser();
        PopulateRecipeGrid();
    }

    private void ShowServeTab()
    {
        _isServeMode = true;
        _selectedServeQueueIndex = -1;
        _selectedServeRecipe = null;

        ShowRecipeBrowser();
        PopulateServeGrid();
        UpdateActiveCookingSubTab();
    }

    private void UpdateActiveCookingSubTab()
    {
        _tabBreakfastButton?.RemoveFromClassList("active-category-tab");
        _tabMainDishButton?.RemoveFromClassList("active-category-tab");
        _tabDrinksButton?.RemoveFromClassList("active-category-tab");
        _tabDessertButton?.RemoveFromClassList("active-category-tab");
        _tabServeButton?.RemoveFromClassList("active-category-tab");

        if (_isServeMode)
        {
            _tabServeButton?.AddToClassList("active-category-tab");
            return;
        }

        switch (_currentRecipeCategory)
        {
            case RecipeCategory.Breakfast:
                _tabBreakfastButton?.AddToClassList("active-category-tab");
                break;
            case RecipeCategory.MainDishes:
                _tabMainDishButton?.AddToClassList("active-category-tab");
                break;
            case RecipeCategory.DrinksSmoothies:
                _tabDrinksButton?.AddToClassList("active-category-tab");
                break;
            case RecipeCategory.BakeryDesserts:
                _tabDessertButton?.AddToClassList("active-category-tab");
                break;
        }
    }

    private void ShowRecipeBrowser()
    {
        if (!_isServeMode)
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

        if (_cookRecipeButton != null)
            _cookRecipeButton.text = _isServeMode ? "Serve" : "Cook";

        _isCooking = false;

        if (_isServeMode)
            PopulateServeGrid();
    }
    private void PopulateRecipeGrid()
    {
        EnsureRestaurantRecipeCoverageIfNeeded();

        if (_isServeMode)
        {
            PopulateServeGrid();
            return;
        }

        if (_recipeGrid == null)
        {
            return;
        }

        _recipeGrid.Clear();

        if (recipes == null || recipes.Length == 0)
        {
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


            // Only show recipes in the current category
            if (recipe.category != _currentRecipeCategory)
                continue;

            matchingCategoryCount++;


            int ingredientCount = recipe.ingredients != null ? recipe.ingredients.Length : 0;
            if (ingredientCount <= 0)
            {

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


    }

    private void PopulateServeGrid()
    {
        if (_recipeGrid == null)
            return;

        _recipeGrid.Clear();
        TryResolveRestaurantQueueManager();

        if (_restaurantQueueManager == null)
        {
            SetServeStatus("No queue manager found in this scene.");
            return;
        }

        IReadOnlyList<RestaurantNpcQueueManager.QueueOrderView> orders = _restaurantQueueManager.GetQueueOrders();
        if (orders == null || orders.Count == 0)
        {
            SetServeStatus("No customer orders right now.");
            return;
        }

        SetServeStatus("Select an order. Only Q0 (front) can be served.");

        for (int i = 0; i < orders.Count; i++)
        {
            RestaurantNpcQueueManager.QueueOrderView order = orders[i];
            int queueIndex = order.queueIndex;

            VisualElement orderBox = new VisualElement();
            orderBox.style.width = 120;
            orderBox.style.height = 76;
            orderBox.style.backgroundColor = queueIndex == 0
                ? new Color(0.84f, 0.95f, 0.84f, 1f)
                : new Color(0.96f, 0.91f, 0.82f, 1f);
            orderBox.style.marginRight = 8;
            orderBox.style.marginBottom = 8;
            orderBox.style.paddingLeft = 6;
            orderBox.style.paddingTop = 6;
            orderBox.style.borderLeftWidth = 2;
            orderBox.style.borderRightWidth = 2;
            orderBox.style.borderTopWidth = 2;
            orderBox.style.borderBottomWidth = 2;
            orderBox.style.borderLeftColor = new Color(0.78f, 0.65f, 0.48f, 1f);
            orderBox.style.borderRightColor = new Color(0.78f, 0.65f, 0.48f, 1f);
            orderBox.style.borderTopColor = new Color(0.90f, 0.81f, 0.63f, 1f);
            orderBox.style.borderBottomColor = new Color(0.61f, 0.42f, 0.23f, 1f);

            Label title = new Label($"Q{queueIndex}: {order.recipeName}");
            title.style.fontSize = 11;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            orderBox.Add(title);

            Label meta = new Label($"{Mathf.CeilToInt(order.remainingTime)}s | +{order.rewardMoney}");
            meta.style.fontSize = 10;
            orderBox.Add(meta);

            orderBox.RegisterCallback<ClickEvent>(_ => ShowServeDetail(queueIndex));
            _recipeGrid.Add(orderBox);
        }
    }

    private void ShowServeDetail(int queueIndex)
    {
        TryResolveRestaurantQueueManager();
        if (_restaurantQueueManager == null)
        {
            SetServeStatus("No queue manager found.");
            return;
        }

        if (!_restaurantQueueManager.TryGetOrderAtQueueIndex(queueIndex, out RecipeDefinition recipe, out float remainingTime) || recipe == null)
        {
            SetServeStatus("Order no longer available.");
            PopulateServeGrid();
            return;
        }

        _selectedServeQueueIndex = queueIndex;
        _selectedServeRecipe = recipe;

        if (_recipeBrowserView != null)
            _recipeBrowserView.style.display = DisplayStyle.None;

        if (_recipeDetailView != null)
            _recipeDetailView.style.display = DisplayStyle.Flex;

        if (_selectedRecipeName != null)
            _selectedRecipeName.text = $"Serve Q{queueIndex}: {recipe.recipeName} ({Mathf.CeilToInt(remainingTime)}s)";

        if (_selectedRecipeIcon != null)
        {
            if (recipe.result != null && recipe.result.icon != null)
                _selectedRecipeIcon.style.backgroundImage = new StyleBackground(recipe.result.icon);
            else
                _selectedRecipeIcon.style.backgroundImage = StyleKeyword.None;
        }

        if (_requiredIngredientSlots != null)
        {
            _requiredIngredientSlots.Clear();

            if (recipe.result != null)
            {
                VisualElement slot = new VisualElement();
                slot.style.width = 48;
                slot.style.height = 48;
                slot.style.position = Position.Relative;
                slot.style.backgroundColor = new Color(0.95f, 0.88f, 0.72f, 1f);

                if (recipe.result.icon != null)
                    slot.style.backgroundImage = new StyleBackground(recipe.result.icon);

                int haveCount = CountItemInInventory(recipe.result);
                Label count = new Label($"{haveCount}/1");
                count.style.position = Position.Absolute;
                count.style.right = 2;
                count.style.bottom = 0;
                count.style.fontSize = 10;
                count.style.unityFontStyleAndWeight = FontStyle.Bold;
                count.style.color = new Color(0.23f, 0.16f, 0.09f);
                slot.Add(count);

                _requiredIngredientSlots.Add(slot);
            }
        }

        if (_cookRecipeButton != null)
        {
            _cookRecipeButton.text = "Serve";
            bool hasDishReady = recipe.result != null && CountItemInInventory(recipe.result) > 0;
            _cookRecipeButton.SetEnabled(queueIndex == 0 && hasDishReady);
        }

        if (queueIndex != 0)
            SetServeStatus("Only front customer (Q0) can be served.");
        else if (recipe.result != null && CountItemInInventory(recipe.result) <= 0)
            SetServeStatus($"Missing dish: {recipe.result.displayName}.");
        else
            SetServeStatus("Ready to serve front customer.");
    }

    private void TryServeSelectedOrder()
    {
        TryResolveRestaurantQueueManager();
        if (_restaurantQueueManager == null)
        {
            SetServeStatus("No queue manager found.");
            return;
        }

        if (_selectedServeQueueIndex != 0)
        {
            SetServeStatus("Serve the front customer (Q0) first.");
            return;
        }

        if (_selectedServeRecipe == null || _selectedServeRecipe.result == null)
        {
            SetServeStatus("Select a valid front-customer order first.");
            return;
        }

        if (CountItemInInventory(_selectedServeRecipe.result) <= 0)
        {
            SetServeStatus($"Missing dish: {_selectedServeRecipe.result.displayName}.");
            return;
        }

        bool served = _restaurantQueueManager.TryServeFrontCustomerFromInventory(out string message);
        SetServeStatus(message);

        RefreshAllSlots();

        if (served)
        {
            _selectedServeQueueIndex = -1;
            _selectedServeRecipe = null;
            ShowServeTab();
        }
        else if (_selectedServeRecipe != null)
        {
            ShowServeDetail(0);
        }
    }

    private void TryResolveRestaurantQueueManager()
    {
        if (_restaurantQueueManager == null)
            _restaurantQueueManager = FindFirstObjectByType<RestaurantNpcQueueManager>();
    }

    private void SetServeStatus(string message)
    {
        EnsureServeStatusLabel();
        if (_serveStatusLabel == null)
            return;

        if (string.IsNullOrEmpty(message))
        {
            _serveStatusLabel.style.display = DisplayStyle.None;
            _serveStatusLabel.text = string.Empty;
            return;
        }

        _serveStatusLabel.text = message;
        _serveStatusLabel.style.display = DisplayStyle.Flex;
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
        if (_craftingInventoryGrid == null || _slotsData == null || _hotbarData == null)
            return;

        _craftingInventoryGrid.Clear();
        _cookingSourceSlots.Clear();

        for (int i = 0; i < _hotbarData.Length; i++)
            _cookingSourceSlots.Add(new CookingSourceSlot { fromHotbar = true, slotIndex = i });

        for (int i = 0; i < _slotsData.Length; i++)
            _cookingSourceSlots.Add(new CookingSourceSlot { fromHotbar = false, slotIndex = i });

        for (int i = 0; i < _cookingSourceSlots.Count; i++)
        {
            int sourceIndex = i;
            CookingSourceSlot source = _cookingSourceSlots[sourceIndex];

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

            ItemStack stack = source.fromHotbar ? _hotbarData[source.slotIndex] : _slotsData[source.slotIndex];

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

                slot.RegisterCallback<MouseDownEvent>(evt => OnCookingInventorySlotMouseDown(sourceIndex, slot, evt));
            }

            _craftingInventoryGrid.Add(slot);
        }
    }

    private void OnCookingInventorySlotMouseDown(int sourceIndex, VisualElement slotElement, MouseDownEvent evt)
    {
        if (_slotsData == null || _hotbarData == null)
            return;

        if (sourceIndex < 0 || sourceIndex >= _cookingSourceSlots.Count)
            return;

        CookingSourceSlot source = _cookingSourceSlots[sourceIndex];
        ItemStack sourceStack = source.fromHotbar ? _hotbarData[source.slotIndex] : _slotsData[source.slotIndex];

        if (sourceStack.item == null || sourceStack.amount <= 0)
            return;

        _draggedCookingInventoryIndex = sourceIndex;
        _isDraggingFromCookingInventory = true;

        if (slotElement != null)
            slotElement.style.opacity = 0.5f;

        evt.StopPropagation();
    }

    private void OnCookingIngredientSlotMouseUp(int ingredientSlotIndex, MouseUpEvent evt)
    {
        if (!_isDraggingFromCookingInventory || _draggedCookingInventoryIndex < 0)
            return;

        if (_slotsData == null || _hotbarData == null || _cookingRecipeSlotData == null || _selectedRecipe == null)
            return;

        if (_selectedRecipe.ingredients == null)
            return;

        if (ingredientSlotIndex < 0 || ingredientSlotIndex >= _selectedRecipe.ingredients.Length)
            return;

        if (_draggedCookingInventoryIndex >= _cookingSourceSlots.Count)
        {
            ResetCookingDragState();
            return;
        }

        CookingSourceSlot source = _cookingSourceSlots[_draggedCookingInventoryIndex];
        ItemStack draggedStack = source.fromHotbar ? _hotbarData[source.slotIndex] : _slotsData[source.slotIndex];

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

            ResetCookingDragState();
            evt.StopPropagation();
            return;
        }

        // Already full
        if (placedStack.amount >= requiredIngredient.amount)
        {

            ResetCookingDragState();
            evt.StopPropagation();
            return;
        }

        // Remove 1 from the exact source used in cooking UI (hotbar or inventory).
        draggedStack.amount -= 1;
        if (draggedStack.amount <= 0)
            draggedStack = new ItemStack { item = null, amount = 0 };

        if (source.fromHotbar)
            _hotbarData[source.slotIndex] = draggedStack;
        else
            _slotsData[source.slotIndex] = draggedStack;

        // Add 1 to recipe slot
        _cookingRecipeSlotData[ingredientSlotIndex].item = requiredIngredient.item;
        _cookingRecipeSlotData[ingredientSlotIndex].amount += 1;

        if (source.fromHotbar)
        {
            RefreshHotbarSlot(source.slotIndex);
            SyncExternalHotbarSlot(source.slotIndex);
        }
        else
        {
            RefreshInventorySlot(source.slotIndex);
        }

        PopulateCraftingInventoryFromRealData();
        RefreshCookingIngredientSlotVisual(ingredientSlotIndex);
        MarkInventoryDirty();

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

                return;
            }

            if (_cookingRecipeSlotData[i].amount < _selectedRecipe.ingredients[i].amount)
            {

                return;
            }
        }

        StartCoroutine(AnimateCookingProcess());
    }
    private System.Collections.IEnumerator AnimateCookingProcess()
    {
        _isCooking = true;
        StartCookingLoopSound();

        if (_cookRecipeButton != null)
            _cookRecipeButton.SetEnabled(false);

        if (_backToRecipesButton != null)
            _backToRecipesButton.SetEnabled(false);

        if (_cookingLoadingContainer != null)
            _cookingLoadingContainer.style.display = DisplayStyle.Flex;

        if (_cookingLoadingLabel != null)
            _cookingLoadingLabel.text = $"Preparing {_selectedRecipe.recipeName}...";

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
                _cookingLoadingLabel.text = $"Preparing {_selectedRecipe.recipeName}...";

            yield return null;
        }

        bool cookedSuccessfully = false;
        if (_selectedRecipe.result != null)
            cookedSuccessfully = TryAdd(_selectedRecipe.result, _selectedRecipe.resultAmount);

        for (int i = 0; i < _cookingRecipeSlotData.Length; i++)
        {
            _cookingRecipeSlotData[i] = new ItemStack { item = null, amount = 0 };
            RefreshCookingIngredientSlotVisual(i);
        }

        PopulateCraftingInventoryFromRealData();
        RefreshAllSlots();

        if (_cookingLoadingLabel != null)
            _cookingLoadingLabel.text = $"{_selectedRecipe.recipeName} ready!";

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
        StopCookingLoopSound();

        if (cookedSuccessfully)
            OnRecipeCooked?.Invoke(_selectedRecipe);


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
        EnsureRestaurantRecipeCoverageIfNeeded();

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

            return;
        }

        recipe.Craft(ref _slotsData);

        // Refresh all inventory slots
        for (int i = 0; i < _itemSlots.Length; i++)
            RefreshInventorySlot(i);

        MarkInventoryDirty();


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

        // Count from main inventory
        foreach (var slot in _slotsData)
        {
            if (slot.item == item)
                total += slot.amount;
        }

        // Count from hotbar
        if (_hotbarData != null)
        {
            foreach (var slot in _hotbarData)
            {
                if (slot.item == item)
                    total += slot.amount;
            }
        }

        return total;
    }

    // ==================== MENU HELPERS (FOR FUTURE ORDERS) ====================

    public RecipeDefinition[] GetMenuRecipes()
    {
        EnsureRestaurantRecipeCoverageIfNeeded();

        if (recipes == null || recipes.Length == 0)
            return Array.Empty<RecipeDefinition>();

        int validCount = 0;
        for (int i = 0; i < recipes.Length; i++)
        {
            if (recipes[i] != null)
                validCount++;
        }

        if (validCount == 0)
            return Array.Empty<RecipeDefinition>();

        RecipeDefinition[] menu = new RecipeDefinition[validCount];
        int menuIndex = 0;
        for (int i = 0; i < recipes.Length; i++)
        {
            if (recipes[i] != null)
                menu[menuIndex++] = recipes[i];
        }

        return menu;
    }

    public RecipeDefinition[] GetMenuRecipesByCategory(RecipeCategory category)
    {
        EnsureRestaurantRecipeCoverageIfNeeded();

        if (recipes == null || recipes.Length == 0)
            return Array.Empty<RecipeDefinition>();

        int count = 0;
        for (int i = 0; i < recipes.Length; i++)
        {
            if (recipes[i] != null && recipes[i].category == category)
                count++;
        }

        if (count == 0)
            return Array.Empty<RecipeDefinition>();

        RecipeDefinition[] filtered = new RecipeDefinition[count];
        int filteredIndex = 0;
        for (int i = 0; i < recipes.Length; i++)
        {
            if (recipes[i] != null && recipes[i].category == category)
                filtered[filteredIndex++] = recipes[i];
        }

        return filtered;
    }

    public RecipeDefinition GetRandomMenuRecipe()
    {
        RecipeDefinition[] menu = GetMenuRecipes();
        if (menu.Length == 0)
            return null;

        return menu[UnityEngine.Random.Range(0, menu.Length)];
    }

    public RecipeDefinition GetRandomMenuRecipeByCategory(RecipeCategory category)
    {
        RecipeDefinition[] menu = GetMenuRecipesByCategory(category);
        if (menu.Length == 0)
            return null;

        return menu[UnityEngine.Random.Range(0, menu.Length)];
    }

    private void EnsureRestaurantRecipeCoverageIfNeeded()
    {
        if (_restaurantRecipeAutoLoadAttempted)
            return;

        string sceneName = SceneManager.GetActiveScene().name ?? string.Empty;
        if (sceneName.IndexOf("restaurant", StringComparison.OrdinalIgnoreCase) < 0)
            return;

        // If Bakery/Desserts already has data, nothing to repair.
        if (HasRecipeCategory(RecipeCategory.BakeryDesserts))
        {
            _restaurantRecipeAutoLoadAttempted = true;
            return;
        }

#if UNITY_EDITOR
        string[] recipeGuids = UnityEditor.AssetDatabase.FindAssets("t:RecipeDefinition", new[] { "Assets/Items/RecipeDefinition" });
        if (recipeGuids != null && recipeGuids.Length > 0)
        {
            List<RecipeDefinition> merged = new List<RecipeDefinition>(recipeGuids.Length + (recipes != null ? recipes.Length : 0));
            HashSet<RecipeDefinition> seen = new HashSet<RecipeDefinition>();

            if (recipes != null)
            {
                for (int i = 0; i < recipes.Length; i++)
                {
                    RecipeDefinition existing = recipes[i];
                    if (existing == null || !seen.Add(existing))
                        continue;

                    merged.Add(existing);
                }
            }

            for (int i = 0; i < recipeGuids.Length; i++)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(recipeGuids[i]);
                RecipeDefinition found = UnityEditor.AssetDatabase.LoadAssetAtPath<RecipeDefinition>(path);
                if (found == null || !seen.Add(found))
                    continue;

                merged.Add(found);
            }

            recipes = merged.ToArray();
            RebuildItemLookupFromKnownItems();
        }
#endif

        _restaurantRecipeAutoLoadAttempted = true;
    }

    private bool HasRecipeCategory(RecipeCategory category)
    {
        if (recipes == null || recipes.Length == 0)
            return false;

        for (int i = 0; i < recipes.Length; i++)
        {
            RecipeDefinition recipe = recipes[i];
            if (recipe != null && recipe.category == category)
                return true;
        }

        return false;
    }

    public bool TryCookRecipeFromExternalUI(RecipeDefinition recipe, out string message)
    {
        message = string.Empty;

        if (recipe == null)
        {
            message = "No recipe selected.";
            return false;
        }

        if (_slotsData == null || _slotsData.Length == 0)
        {
            message = "Inventory is not ready.";
            return false;
        }

        if (!recipe.CanCraft(_slotsData))
        {
            message = "Missing ingredients.";
            return false;
        }

        recipe.Craft(ref _slotsData);
        RefreshAllSlots();
        OnRecipeCooked?.Invoke(recipe);
        MarkInventoryDirty();

        message = $"Cooked {recipe.recipeName}!";
        return true;
    }

    /// <summary>
    /// Remove an item from inventory (for farming system - consume seeds)
    /// </summary>
    public bool TryRemoveItem(ItemDefinition item, int amount)
    {
        if (item == null || amount <= 0 || _slotsData == null)
            return false;

        int toRemove = amount;
        bool changed = false;

        // Remove from main inventory slots first
        for (int i = 0; i < _slotsData.Length && toRemove > 0; i++)
        {
            if (_slotsData[i].item == item && _slotsData[i].amount > 0)
            {
                int removed = Mathf.Min(toRemove, _slotsData[i].amount);
                _slotsData[i].amount -= removed;
                toRemove -= removed;
                changed = true;

                if (_slotsData[i].amount <= 0)
                    _slotsData[i] = new ItemStack { item = null, amount = 0 };

                RefreshInventorySlot(i);
            }
        }

        // Remove from hotbar if still need to remove items
        if (toRemove > 0 && _hotbarData != null)
        {
            for (int i = 0; i < _hotbarData.Length && toRemove > 0; i++)
            {
                if (_hotbarData[i].item == item && _hotbarData[i].amount > 0)
                {
                    int removed = Mathf.Min(toRemove, _hotbarData[i].amount);
                    _hotbarData[i].amount -= removed;
                    toRemove -= removed;
                    changed = true;

                    if (_hotbarData[i].amount <= 0)
                        _hotbarData[i] = new ItemStack { item = null, amount = 0 };

                    RefreshHotbarSlot(i);
                    SyncExternalHotbarSlot(i);
                }
            }
        }

        // Sync hotbar display
        SyncExternalHotbarAll();

        if (changed)
            MarkInventoryDirty();

        return toRemove == 0;
    }

    private void SetFooterVisible(bool visible)
    {
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

    /// <summary>
    /// Update the icon sprite shown in a hotbar slot (for tools like watering can with visual states).
    /// Used to switch between empty/full sprites based on tool durability.
    /// </summary>
    public void UpdateHotbarSlotIcon(int slotIndex, Sprite newIcon)
    {
        if (!HasExternalHotbar()) return;
        if (slotIndex < 0 || slotIndex >= HotbarSize) return;

        if (_hotbarData == null || slotIndex >= _hotbarData.Length)
            return;

        var stack = _hotbarData[slotIndex];
        var amount = stack.item != null ? stack.amount : 0;

        // Update the external hotbar display with the new sprite
        SetExternalHotbarSlot(slotIndex, newIcon, amount);
    }

    /// <summary>
    /// Clear all inventory items (used for New Game)
    /// </summary>
    public void ClearAllItems()
    {
        if (_slotsData != null)
        {
            for (int i = 0; i < _slotsData.Length; i++)
            {
                _slotsData[i] = new ItemStack { item = null, amount = 0 };
            }
        }

        if (_hotbarData != null)
        {
            for (int i = 0; i < _hotbarData.Length; i++)
            {
                _hotbarData[i] = new ItemStack { item = null, amount = 0 };
            }
        }

        MarkInventoryDirty();
        RefreshAllSlots();
    }

    /// <summary>
    /// Load inventory data from save (public wrapper for GameManager)
    /// </summary>
    public void LoadPlayerInventory()
    {
        LoadInventoryData();
    }
}


