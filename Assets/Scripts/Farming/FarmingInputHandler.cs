using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class FarmingInputHandler : MonoBehaviour
{
    [SerializeField] private FarmingManager farmingManager;
    [SerializeField] private InventoryController inventoryController;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PickupToastUIToolkit pickupToast;

    [Header("Tool keywords (lowercase)")]
    [SerializeField] private string hoeKeyword = "hoe";
    [SerializeField] private string wateringCanKeyword = "watering_can";
    [SerializeField] private string handKeyword = "hand";

    [Header("Watering Can")]
    [SerializeField] private int wateringCanCapacity = 10;

    private int selectedHotbarSlot = 0;
    private Dictionary<ItemDefinition, int> wateringCanDurability = new Dictionary<ItemDefinition, int>();

    private enum FarmingAction { None, Hoe, Plant, Water, Harvest }

    private void Awake()
    {
        if (farmingManager == null) farmingManager = FindFirstObjectByType<FarmingManager>();
        if (inventoryController == null) inventoryController = FindFirstObjectByType<InventoryController>();
        if (mainCamera == null) mainCamera = Camera.main;
        if (pickupToast == null) pickupToast = FindFirstObjectByType<PickupToastUIToolkit>();

        farmingManager?.Initialize();
    }

    private void Update()
    {
        ReadHotbarKeys();

        if (Input.GetMouseButtonDown(0))
            HandleLeftClick();
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
    }

    private void HandleLeftClick()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (mainCamera == null || farmingManager == null || inventoryController == null)
            return;

        Vector3 world = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        world.z = 0f;

        ItemDefinition selectedItem = inventoryController.GetHotbarItem(selectedHotbarSlot);
        FarmingAction action = GetAction(selectedItem);

        switch (action)
        {
            case FarmingAction.Hoe:
                farmingManager.TryHoeAtWorldPosition(world);
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

    private FarmingAction GetAction(ItemDefinition item)
    {
        if (item == null) return FarmingAction.None;

        // Use displayName if you have it; otherwise fallback to asset name
        string name = (item.displayName != null && item.displayName.Length > 0)
            ? item.displayName.ToLower()
            : item.name.ToLower();

        if (name.Contains(hoeKeyword)) return FarmingAction.Hoe;
        if (name.Contains(wateringCanKeyword)) return FarmingAction.Water;
        if (name.Contains(handKeyword)) return FarmingAction.Harvest;

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