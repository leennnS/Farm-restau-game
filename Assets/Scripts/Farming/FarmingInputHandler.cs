using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class FarmingInputHandler : MonoBehaviour
{
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

    private int selectedHotbarSlot = 0;
    private Dictionary<ItemDefinition, int> wateringCanDurability = new Dictionary<ItemDefinition, int>();
    private TreePlanter _treePlanter = null;

    private enum FarmingAction { None, Hoe, Plant, Water, Harvest, Dig }

    private void ResolveReferences()
    {
        if (farmingManager == null) farmingManager = FindFirstObjectByType<FarmingManager>();
        if (inventoryController == null) inventoryController = FindFirstObjectByType<InventoryController>();
        if (mainCamera == null) mainCamera = Camera.main;
        if (pickupToast == null) pickupToast = FindFirstObjectByType<PickupToastUIToolkit>();
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
    }

    public void SetSelectedHotbarSlot(int slotIndex)
    {
        selectedHotbarSlot = Mathf.Clamp(slotIndex, 0, InventoryController.HotbarSize - 1);
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

        Debug.Log(
            $"CLICK | Screen:{Input.mousePosition} | World:{world} | " +
            $"GroundCell:{groundCell} | CropCell:{cropCell} | GroundCenter:{groundCenter} | " +
            $"Camera:{mainCamera.name} PixelRect:{mainCamera.pixelRect}"
        );

        Debug.DrawLine(groundCenter + Vector3.left * 0.3f, groundCenter + Vector3.right * 0.3f, Color.red, 2f);
        Debug.DrawLine(groundCenter + Vector3.up * 0.3f, groundCenter + Vector3.down * 0.3f, Color.red, 2f);

        SpawnDebugCross(world, Color.cyan);
        SpawnDebugCross(groundCenter, Color.yellow);

        ItemDefinition selectedItem = inventoryController.GetHotbarItem(selectedHotbarSlot);
        FarmingAction action = GetAction(selectedItem);

        // Handle digging first (with hands tool on grass)
        if (action == FarmingAction.Dig)
        {
            if (_treePlanter != null && _treePlanter.TryDigHole(world))
                return;
        }

        // Then try planting a seed in an existing hole
        if (action == FarmingAction.Plant && _treePlanter != null && _treePlanter.TryPlantTree(world))
            return;

        switch (action)
        {
            case FarmingAction.Hoe:
                farmingManager.TryHoeAtWorldPosition(world);
                break;

            case FarmingAction.Dig:
                // Dig already tried above; if we're here it failed, so maybe try to harvest crops instead
                farmingManager.TryHarvestAtWorldPosition(world);
                break;

            case FarmingAction.Water:
                TryWaterWithCan(world, selectedItem);
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
    private void SpawnDebugCross(Vector3 pos, Color color)
    {
        GameObject go = new GameObject("ClickDebugCross");
        go.transform.position = pos;

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 4; // two segments
        lr.useWorldSpace = true;
        lr.startWidth = 0.03f;
        lr.endWidth = 0.03f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;
        lr.sortingOrder = 1000;

        Vector3 left = pos + Vector3.left * 0.2f;
        Vector3 right = pos + Vector3.right * 0.2f;
        Vector3 up = pos + Vector3.up * 0.2f;
        Vector3 down = pos + Vector3.down * 0.2f;

        lr.SetPosition(0, left);
        lr.SetPosition(1, right);
        lr.SetPosition(2, up);
        lr.SetPosition(3, down);

        Destroy(go, 2f);
    }

    private FarmingAction GetAction(ItemDefinition item)
    {
        if (item == null) return FarmingAction.None;

        // Use displayName if you have it; otherwise fallback to asset name
        string name = (item.displayName != null && item.displayName.Length > 0)
            ? item.displayName.ToLower()
            : item.name.ToLower();

        if (name.Contains(hoeKeyword)) return FarmingAction.Hoe;
        if (name.Contains(wateringCanKeyword)) return FarmingAction.Water;

        // Hands tool: returns Dig for planting holes or Harvest for crops (context-dependent)
        if (name.Contains(handKeyword)) return FarmingAction.Dig;

        if (name.Contains("seed") || name.Contains("sapling")) return FarmingAction.Plant;

        return FarmingAction.None;
    }

    private void TryWaterWithCan(Vector3 world, ItemDefinition wateringCanItem)
    {
        if (wateringCanItem == null) return;

        // Get or initialize durability for this can
        if (!wateringCanDurability.ContainsKey(wateringCanItem))
            wateringCanDurability[wateringCanItem] = wateringCanCapacity;

        int currentDurability = wateringCanDurability[wateringCanItem];

        // Check if can is empty
        if (currentDurability <= 0)
        {
            if (pickupToast != null)
                pickupToast.Show("Watering can is empty! Refill it.");
            return;
        }

        // Perform watering
        if (farmingManager.TryWaterAtWorldPosition(world))
        {
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
        // Get currently equipped item from selected hotbar slot
        ItemDefinition selectedItem = inventoryController.GetHotbarItem(selectedHotbarSlot);

        // Check if it's a watering can
        string itemName = (selectedItem != null && selectedItem.displayName != null && selectedItem.displayName.Length > 0)
            ? selectedItem.displayName.ToLower()
            : (selectedItem != null ? selectedItem.name.ToLower() : "");

        if (!itemName.Contains(wateringCanKeyword))
        {
            if (pickupToast != null)
                pickupToast.Show("No watering can equipped!");
            return false;
        }

        // Refill the watering can
        wateringCanDurability[selectedItem] = wateringCanCapacity;

        // Update visual state in hotbar
        UpdateWateringCanVisualState(selectedItem);

        return true;
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