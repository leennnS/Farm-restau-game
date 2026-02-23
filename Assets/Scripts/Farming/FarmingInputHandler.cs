using UnityEngine;
using UnityEngine.EventSystems;

public class FarmingInputHandler : MonoBehaviour
{
    [SerializeField] private FarmingManager farmingManager;
    [SerializeField] private InventoryController inventoryController;
    [SerializeField] private Camera mainCamera;

    [Header("Tool keywords (lowercase)")]
    [SerializeField] private string hoeKeyword = "hoe";
    [SerializeField] private string wateringCanKeyword = "watering_can";
    [SerializeField] private string handKeyword = "hand";

    private int selectedHotbarSlot = 0;

    private enum FarmingAction { None, Hoe, Plant, Water, Harvest }

    private void Awake()
    {
        if (farmingManager == null) farmingManager = FindObjectOfType<FarmingManager>();
        if (inventoryController == null) inventoryController = FindObjectOfType<InventoryController>();
        if (mainCamera == null) mainCamera = Camera.main;

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
                farmingManager.TryWaterAtWorldPosition(world);
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

    private void TryPlant(Vector3 world, ItemDefinition seedItem)
    {
        if (seedItem == null) return;

        CropDefinition cropDef = farmingManager.GetCropBySeeds(seedItem);
        if (cropDef == null) return;

        farmingManager.TryPlantAtWorldPosition(world, cropDef);
    }
}