using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class FarmingInputHandler : MonoBehaviour
{
    private static int sharedSelectedHotbarSlot = 0;

    [Serializable]
    private class DirectionalActionSprites
    {
        public Sprite[] down = Array.Empty<Sprite>();
        public Sprite[] left = Array.Empty<Sprite>();
        public Sprite[] right = Array.Empty<Sprite>();
        public Sprite[] up = Array.Empty<Sprite>();

        public Sprite[] GetSprites(FacingDirection direction)
        {
            return direction switch
            {
                FacingDirection.Up => up,
                FacingDirection.Down => down,
                FacingDirection.Left => left,
                FacingDirection.Right => right,
                _ => down
            };
        }

        public ActionAnimationFrames GetAnimationFrames(FacingDirection direction)
        {
            Sprite[] exactSprites = GetSprites(direction);
            if (HasSprites(exactSprites))
                return new ActionAnimationFrames(exactSprites, false);

            if (direction == FacingDirection.Left && HasSprites(right))
                return new ActionAnimationFrames(right, true);

            if (direction == FacingDirection.Right && HasSprites(left))
                return new ActionAnimationFrames(left, true);

            return new ActionAnimationFrames(exactSprites, false);
        }

        public bool HasAnySprites()
        {
            return HasSprites(down) || HasSprites(left) || HasSprites(right) || HasSprites(up);
        }

        private static bool HasSprites(Sprite[] sprites)
        {
            if (sprites == null)
                return false;

            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null)
                    return true;
            }

            return false;
        }
    }

    private enum FacingDirection { Down, Left, Right, Up }

    private readonly struct ActionAnimationFrames
    {
        public readonly Sprite[] Sprites;
        public readonly bool FlipX;

        public ActionAnimationFrames(Sprite[] sprites, bool flipX)
        {
            Sprites = sprites;
            FlipX = flipX;
        }
    }

    [SerializeField] private FarmingManager farmingManager;
    [SerializeField] private InventoryController inventoryController;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PickupToastUIToolkit pickupToast;

    [Header("Scene Filter")]
    [SerializeField] private bool runOnlyInFarmScene = true;
    [SerializeField] private string farmSceneName = "FarmScene";

    [Header("Tool keywords (lowercase)")]
    [SerializeField] private string hoeKeyword = "hoe";
    [SerializeField] private string wateringCanKeyword = "watering_can";
    [SerializeField] private string handKeyword = "hand";

    [Header("Watering Can")]
    [SerializeField] private int wateringCanCapacity = 10;

    [Header("Action Animation Sprites")]
    [SerializeField, Tooltip("Assign 8 watering sprites for each direction.")]
    private DirectionalActionSprites wateringSprites = new DirectionalActionSprites();
    [SerializeField, Tooltip("Assign 8 hoeing sprites for each direction.")]
    private DirectionalActionSprites hoeSprites = new DirectionalActionSprites();
    [SerializeField, Tooltip("Assign 8 hand-tool digging sprites for each direction.")]
    private DirectionalActionSprites handToolSprites = new DirectionalActionSprites();
    [SerializeField, Tooltip("Optional: assign 8 tree planting sprites for each direction.")]
    private DirectionalActionSprites treePlantingSprites = new DirectionalActionSprites();
    [SerializeField] private float actionFrameSeconds = 0.08f;

    private int selectedHotbarSlot = 0;
    private Dictionary<ItemDefinition, int> wateringCanDurability = new Dictionary<ItemDefinition, int>();
    private TreePlanter _treePlanter = null;
    private SpriteRenderer _playerSpriteRenderer;
    private Animator _playerAnimator;
    private CharacterController2D _playerController;
    private Transform _playerTransform;
    private Coroutine _actionAnimationCoroutine;
    private Sprite _spriteBeforeActionAnimation;
    private bool _animatorWasEnabledBeforeActionAnimation;
    private bool _flipXBeforeActionAnimation;

    private enum FarmingAction { None, Hoe, Plant, Water, Harvest, Dig }

    private void ResolveReferences()
    {
        if (farmingManager == null) farmingManager = FindFirstObjectByType<FarmingManager>();
        if (inventoryController == null) inventoryController = InventoryController.Instance;
        if (inventoryController == null) inventoryController = FindFirstObjectByType<InventoryController>();
        if (mainCamera == null) mainCamera = Camera.main;
        if (pickupToast == null) pickupToast = FindFirstObjectByType<PickupToastUIToolkit>();
        if (_treePlanter == null) _treePlanter = FindFirstObjectByType<TreePlanter>();
        selectedHotbarSlot = sharedSelectedHotbarSlot;
        if (_treePlanter != null) _treePlanter.SetSelectedHotbarSlot(selectedHotbarSlot);
        ResolvePlayerReferences();
    }

    private void ResolvePlayerReferences()
    {
        bool playerReferenceMissing = _playerTransform == null || _playerController == null;
        if (playerReferenceMissing)
        {
            GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            CharacterController2D playerController = taggedPlayer != null
                ? taggedPlayer.GetComponent<CharacterController2D>()
                : null;

            if (playerController == null)
                playerController = FindFirstObjectByType<CharacterController2D>();

            if (playerController != null)
            {
                _playerController = playerController;
                _playerTransform = playerController.transform;
            }
        }

        Transform lookupRoot = _playerTransform != null ? _playerTransform : transform;
        if (_playerSpriteRenderer == null || !_playerSpriteRenderer.transform.IsChildOf(lookupRoot))
            _playerSpriteRenderer = lookupRoot.GetComponentInChildren<SpriteRenderer>();

        if (_playerAnimator == null || _playerAnimator.transform != lookupRoot)
            _playerAnimator = lookupRoot.GetComponent<Animator>();
    }

    private void Awake()
    {
        ResolveReferences();
        farmingManager?.Initialize();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolveReferences();
        farmingManager?.Initialize();
    }

    private void Update()
    {
        if (!IsSceneAllowed())
            return;

        //Debug.Log("[FarmingInputHandler] Update called");
        ReadHotbarKeys();

        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("[FarmingInputHandler] MOUSE CLICK DETECTED");
            HandleLeftClick();
        }
    }

    private void ReadHotbarKeys()
    {
        int previousSlot = selectedHotbarSlot;

        // 1..9 => slots 0..8
        if (Input.GetKeyDown(KeyCode.Alpha1)) selectedHotbarSlot = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) selectedHotbarSlot = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) selectedHotbarSlot = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4)) selectedHotbarSlot = 3;
        if (Input.GetKeyDown(KeyCode.Alpha5)) selectedHotbarSlot = 4;
        if (Input.GetKeyDown(KeyCode.Alpha6)) selectedHotbarSlot = 5;
        if (Input.GetKeyDown(KeyCode.Alpha7)) selectedHotbarSlot = 6;
        if (Input.GetKeyDown(KeyCode.Alpha8)) selectedHotbarSlot = 7;
        if (Input.GetKeyDown(KeyCode.Alpha9)) selectedHotbarSlot = 8;

        // 0 => slot 9
        if (Input.GetKeyDown(KeyCode.Alpha0)) selectedHotbarSlot = 9;

        if (selectedHotbarSlot != previousSlot)
            SetSelectedHotbarSlot(selectedHotbarSlot);
    }

    public void SetSelectedHotbarSlot(int slotIndex)
    {
        int clampedSlot = Mathf.Clamp(slotIndex, 0, InventoryController.HotbarSize - 1);
        sharedSelectedHotbarSlot = clampedSlot;

        FarmingInputHandler[] handlers = FindObjectsByType<FarmingInputHandler>(FindObjectsSortMode.None);
        for (int i = 0; i < handlers.Length; i++)
            handlers[i].ApplySelectedHotbarSlot(clampedSlot);
    }

    private void ApplySelectedHotbarSlot(int slotIndex)
    {
        selectedHotbarSlot = slotIndex;
        if (_treePlanter != null)
            _treePlanter.SetSelectedHotbarSlot(selectedHotbarSlot);
    }

    public void RegisterTreePlanter(TreePlanter planter)
    {
        _treePlanter = planter;
        if (_treePlanter != null)
            _treePlanter.SetSelectedHotbarSlot(selectedHotbarSlot);
    }

    public void UnregisterTreePlanter(TreePlanter planter)
    {
        if (_treePlanter == planter)
            _treePlanter = null;
    }

    private void HandleLeftClick()
    {
        if (!IsSceneAllowed())
            return;

        // Handle stale references when the player object persists across scenes.
        ResolveReferences();

        Debug.Log("[FarmingInputHandler] HandleLeftClick START");

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            // Log which UI object is blocking
            GameObject uiObject = EventSystem.current.currentSelectedGameObject;
            Debug.Log($"[FarmingInputHandler] BLOCKED: Click is over UI: {(uiObject ? uiObject.name : "Unknown UI")}");

            // ALLOW farming anyway - don't block UI clicks from farming
            // Proceed instead of returning
        }

        if (mainCamera == null)
        {
            Debug.LogError("[FarmingInputHandler] BLOCKED: mainCamera is NULL");
            return;
        }

        if (farmingManager == null)
        {
            Debug.LogError("[FarmingInputHandler] BLOCKED: farmingManager is NULL");
            return;
        }

        if (inventoryController == null)
        {
            Debug.LogError("[FarmingInputHandler] BLOCKED: inventoryController is NULL");
            return;
        }

        Debug.Log("[FarmingInputHandler] All references OK, continuing...");

        Vector3 mouse = Input.mousePosition;

        // Ignore clicks outside the actual camera/game render area
        if (!mainCamera.pixelRect.Contains(mouse))
        {
            Debug.Log($"IGNORED CLICK outside pixelRect | Mouse:{mouse} | PixelRect:{mainCamera.pixelRect}");
            return;
        }

        float targetZ = farmingManager.GroundTilemap != null
            ? farmingManager.GroundTilemap.transform.position.z
            : 0f;

        // For an orthographic camera, convert directly using the camera distance to the tile plane
        mouse.z = Mathf.Abs(mainCamera.transform.position.z - targetZ);

        Vector3 world = mainCamera.ScreenToWorldPoint(mouse);
        world.z = targetZ;

        Vector3Int groundCell = farmingManager.GroundTilemap != null
            ? farmingManager.GroundTilemap.WorldToCell(world)
            : Vector3Int.zero;

        Vector3Int cropCell = farmingManager.CropTilemap != null
            ? farmingManager.CropTilemap.WorldToCell(world)
            : Vector3Int.zero;

        Vector3 groundCenter = farmingManager.GroundTilemap != null
            ? farmingManager.GroundTilemap.GetCellCenterWorld(groundCell)
            : Vector3.zero;
        Vector3 animationTarget = farmingManager.GroundTilemap != null
            ? groundCenter
            : world;

        Debug.Log(
            $"CLICK | Screen:{Input.mousePosition} | World:{world} | " +
            $"GroundCell:{groundCell} | CropCell:{cropCell} | GroundCenter:{groundCenter} | " +
            $"Camera:{mainCamera.name} PixelRect:{mainCamera.pixelRect}"
        );

        ItemDefinition selectedItem = inventoryController.GetHotbarItem(selectedHotbarSlot);
        FarmingAction action = GetAction(selectedItem);

        // Handle digging first (with hands tool on grass)
        if (action == FarmingAction.Dig)
        {
            PlayActionAnimation(FarmingAction.Dig, animationTarget);
            if (_treePlanter != null && _treePlanter.TryDigHole(world))
                return;
        }

        // Then try planting a seed in an existing hole
        if (action == FarmingAction.Plant && _treePlanter != null)
        {
            if (_treePlanter.TryPlantTree(world))
            {
                PlayActionAnimation(FarmingAction.Plant, animationTarget);
                return;
            }
        }

        if (farmingManager.HasMatureCropAtWorldPosition(world))
        {
            farmingManager.TryHarvestAtWorldPosition(world);
            return;
        }

        switch (action)
        {
            case FarmingAction.Hoe:
                PlayActionAnimation(FarmingAction.Hoe, animationTarget);
                farmingManager.TryHoeAtWorldPosition(world);
                break;

            case FarmingAction.Dig:
                // Dig already tried above; if we're here it failed, so maybe try to harvest crops instead
                farmingManager.TryHarvestAtWorldPosition(world);
                break;

            case FarmingAction.Water:
                TryWaterWithCan(world, animationTarget, selectedItem);
                break;

            case FarmingAction.Harvest:
                farmingManager.TryHarvestAtWorldPosition(world);
                break;

            case FarmingAction.Plant:
                TryPlant(world, selectedItem);
                break;
        }
    }

    private bool IsSceneAllowed()
    {
        if (!runOnlyInFarmScene)
            return true;

        Scene active = SceneManager.GetActiveScene();
        string activeName = active.name ?? string.Empty;
        string expected = farmSceneName ?? string.Empty;

        if (string.Equals(activeName, expected, System.StringComparison.OrdinalIgnoreCase))
            return true;

        return activeName.IndexOf("farm", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
    private FarmingAction GetAction(ItemDefinition item)
    {
        if (item == null) return FarmingAction.None;

        if (item is WateringCanItem) return FarmingAction.Water;

        string name = GetComparableItemName(item);

        if (name.Contains(NormalizeItemName(hoeKeyword))) return FarmingAction.Hoe;
        if (name.Contains(NormalizeItemName(wateringCanKeyword)) || name.Contains("wateringcan")) return FarmingAction.Water;

        // Hands tool: returns Dig for planting holes or Harvest for crops (context-dependent)
        if (name.Contains(NormalizeItemName(handKeyword))) return FarmingAction.Dig;

        if (name.Contains("seed") || name.Contains("sapling")) return FarmingAction.Plant;

        return FarmingAction.None;
    }

    private string GetComparableItemName(ItemDefinition item)
    {
        if (item == null) return string.Empty;

        string displayName = item.displayName;
        string assetName = item.name;
        return NormalizeItemName($"{displayName} {assetName}");
    }

    private string NormalizeItemName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value
            .ToLowerInvariant()
            .Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty);
    }

    private void TryWaterWithCan(Vector3 world, Vector3 animationTarget, ItemDefinition wateringCanItem)
    {
        if (wateringCanItem == null) return;

        // Get or initialize durability for this can
        if (!wateringCanDurability.ContainsKey(wateringCanItem))
            wateringCanDurability[wateringCanItem] = wateringCanCapacity;

        int currentDurability = wateringCanDurability[wateringCanItem];

        // Check if can is empty
        if (currentDurability <= 0)
        {
            PlayActionAnimation(FarmingAction.Water, animationTarget, 1);

            if (pickupToast != null)
                pickupToast.Show("Watering can is empty! Refill it.");
            return;
        }

        // Perform watering
        if (farmingManager.TryWaterAtWorldPosition(world))
        {
            PlayActionAnimation(FarmingAction.Water, animationTarget);

            // Decrease durability
            wateringCanDurability[wateringCanItem]--;

            // Update visual state in hotbar
            UpdateWateringCanVisualState(wateringCanItem);

            // Show message if can is becoming empty
            if (wateringCanDurability[wateringCanItem] <= 0)
            {
                if (pickupToast != null)
                    pickupToast.Show("Watering can empty! Needs refill.");
            }
            else if (wateringCanDurability[wateringCanItem] <= 3)
            {
                if (pickupToast != null)
                    pickupToast.Show($"Water: {wateringCanDurability[wateringCanItem]}/{wateringCanCapacity}");
            }
        }
    }

    private void PlayActionAnimation(FarmingAction action, Vector3 targetWorldPosition, int maxFrames = 0)
    {
        DirectionalActionSprites sprites = ResolveAnimationSprites(action);
        if (sprites == null)
            return;

        ResolveReferences();

        ActionAnimationFrames frames = sprites.GetAnimationFrames(GetFacingDirection(targetWorldPosition));
        if (frames.Sprites == null || frames.Sprites.Length == 0 || _playerSpriteRenderer == null)
            return;

        if (_actionAnimationCoroutine != null)
        {
            StopCoroutine(_actionAnimationCoroutine);
            RestoreActionAnimationState();
        }

        _actionAnimationCoroutine = StartCoroutine(PlayActionAnimationRoutine(frames, maxFrames));
    }

    private DirectionalActionSprites ResolveAnimationSprites(FarmingAction action)
    {
        DirectionalActionSprites localSprites = GetLocalAnimationSprites(action);
        if (localSprites != null && localSprites.HasAnySprites())
            return localSprites;

        if (_playerTransform == null)
            return localSprites;

        FarmingInputHandler playerHandler = _playerTransform.GetComponent<FarmingInputHandler>();
        if (playerHandler == null || playerHandler == this)
            return localSprites;

        DirectionalActionSprites playerSprites = playerHandler.GetLocalAnimationSprites(action);

        return playerSprites != null && playerSprites.HasAnySprites()
            ? playerSprites
            : localSprites;
    }

    private DirectionalActionSprites GetLocalAnimationSprites(FarmingAction action)
    {
        return action switch
        {
            FarmingAction.Water => wateringSprites,
            FarmingAction.Hoe => hoeSprites,
            FarmingAction.Dig => handToolSprites,
            FarmingAction.Plant => treePlantingSprites,
            _ => null
        };
    }

    private IEnumerator PlayActionAnimationRoutine(ActionAnimationFrames frames, int maxFrames)
    {
        _spriteBeforeActionAnimation = _playerSpriteRenderer.sprite;
        _flipXBeforeActionAnimation = _playerSpriteRenderer.flipX;
        _animatorWasEnabledBeforeActionAnimation = _playerAnimator != null && _playerAnimator.enabled;

        if (_playerAnimator != null)
            _playerAnimator.enabled = false;

        _playerSpriteRenderer.flipX = frames.FlipX;
        float frameSeconds = Mathf.Max(0.01f, actionFrameSeconds);

        int frameCount = maxFrames > 0
            ? Mathf.Min(maxFrames, frames.Sprites.Length)
            : frames.Sprites.Length;

        for (int i = 0; i < frameCount; i++)
        {
            if (frames.Sprites[i] != null)
                _playerSpriteRenderer.sprite = frames.Sprites[i];

            yield return new WaitForSeconds(frameSeconds);
        }

        RestoreActionAnimationState();

        _actionAnimationCoroutine = null;
    }

    private void RestoreActionAnimationState()
    {
        if (_playerAnimator != null)
        {
            if (_playerSpriteRenderer != null)
                _playerSpriteRenderer.flipX = _flipXBeforeActionAnimation;
            _playerAnimator.enabled = _animatorWasEnabledBeforeActionAnimation;
        }
        else if (_playerSpriteRenderer != null && _spriteBeforeActionAnimation != null)
        {
            _playerSpriteRenderer.flipX = _flipXBeforeActionAnimation;
            _playerSpriteRenderer.sprite = _spriteBeforeActionAnimation;
        }
    }

    private FacingDirection GetFacingDirection(Vector3 targetWorldPosition)
    {
        Vector3 playerPosition = _playerTransform != null ? _playerTransform.position : transform.position;
        Vector2 toTarget = targetWorldPosition - playerPosition;

        if (Mathf.Abs(toTarget.x) > Mathf.Abs(toTarget.y))
            return toTarget.x < 0f ? FacingDirection.Left : FacingDirection.Right;

        return toTarget.y < 0f ? FacingDirection.Down : FacingDirection.Up;
    }

    private void TryPlant(Vector3 world, ItemDefinition seedItem)
    {
        if (seedItem == null) return;

        CropDefinition cropDef = farmingManager.GetCropBySeeds(seedItem);
        if (cropDef == null) return;

        farmingManager.TryPlantAtWorldPosition(world, cropDef);
    }

    // Public method to refill watering can from pond or other refill point
    public bool TryRefillWateringCan()
    {
        ResolveReferences();

        if (inventoryController == null)
        {
            if (pickupToast != null)
                pickupToast.Show("Inventory not ready.");
            return false;
        }

        // Get currently equipped item from selected hotbar slot
        ItemDefinition selectedItem = inventoryController.GetHotbarItem(selectedHotbarSlot);

        // Check if it's a watering can
        if (!IsWateringCanItem(selectedItem))
        {
            selectedItem = FindWateringCanInHotbar(out int wateringCanSlot);
            if (selectedItem != null)
            {
                SetSelectedHotbarSlot(wateringCanSlot);
            }
        }

        if (!IsWateringCanItem(selectedItem))
        {
            if (pickupToast != null)
                pickupToast.Show("No watering can in hotbar!");
            return false;
        }

        // Refill the watering can
        wateringCanDurability[selectedItem] = wateringCanCapacity;

        // Update visual state in hotbar
        UpdateWateringCanVisualState(selectedItem);

        return true;
    }

    private ItemDefinition FindWateringCanInHotbar(out int slotIndex)
    {
        slotIndex = -1;

        if (inventoryController == null)
            return null;

        for (int i = 0; i < InventoryController.HotbarSize; i++)
        {
            ItemDefinition item = inventoryController.GetHotbarItem(i);
            if (IsWateringCanItem(item))
            {
                slotIndex = i;
                return item;
            }
        }

        return null;
    }

    private bool IsWateringCanItem(ItemDefinition item)
    {
        if (item == null)
            return false;

        if (item is WateringCanItem)
            return true;

        string itemName = GetComparableItemName(item);
        return itemName.Contains(NormalizeItemName(wateringCanKeyword)) || itemName.Contains("wateringcan");
    }

    // Helper method to update the visual state of watering can in hotbar
    private void UpdateWateringCanVisualState(ItemDefinition wateringCanItem)
    {
        if (inventoryController == null || wateringCanItem == null)
            return;

        // Get current durability
        int currentDurability = wateringCanDurability.ContainsKey(wateringCanItem)
            ? wateringCanDurability[wateringCanItem]
            : 0;

        // Determine which sprite to show (if it's a WateringCanItem)
        Sprite spriteToShow = wateringCanItem.icon; // Default to regular icon

        if (wateringCanItem is WateringCanItem wateringCanDef)
        {
            spriteToShow = wateringCanDef.GetSpriteForDurability(currentDurability, wateringCanCapacity);
        }

        // Update all hotbar slots that have this watering can
        for (int i = 0; i < InventoryController.HotbarSize; i++)
        {
            if (inventoryController.GetHotbarItem(i) == wateringCanItem)
            {
                inventoryController.UpdateHotbarSlotIcon(i, spriteToShow);
            }
        }
    }
}
