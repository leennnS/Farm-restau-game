using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Saves and loads farming state (hoed soil, planted crops) to/from JSON.
/// </summary>
public class FarmingDataSaveSystem : MonoBehaviour
{
    private const string DefaultSaveFileName = "farming_save.json";

    private static FarmingDataSaveSystem _instance;

    [SerializeField] private FarmingManager farmingManager;
    [SerializeField] private string saveFileName = DefaultSaveFileName;

    private readonly List<TreeDataSerializable> pendingTreeRestores = new List<TreeDataSerializable>();
    private readonly List<HarvestPickupDataSerializable> pendingHarvestPickupRestores = new List<HarvestPickupDataSerializable>();

    public static FarmingDataSaveSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<FarmingDataSaveSystem>();

                if (_instance == null)
                {
                    GameObject saveSystemObject = new GameObject(nameof(FarmingDataSaveSystem));
                    _instance = saveSystemObject.AddComponent<FarmingDataSaveSystem>();
                    Debug.LogWarning("[FarmingDataSaveSystem] No save system found in the scene. Created one automatically.");
                }
            }

            return _instance;
        }
    }

    // Convenience check used by other classes to see if an instance exists
    public static bool HasInstance => _instance != null;

    private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        if (farmingManager == null)
            farmingManager = FindFirstObjectByType<FarmingManager>();
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    /// <summary>
    /// Save current farming state to JSON file
    /// </summary>
    public void SaveFarmingData(FarmingManager manager = null)
    {
        FarmingManager targetManager = ResolveFarmingManager(manager);

        if (targetManager == null)
        {
            Debug.LogError("[FarmingDataSaveSystem] FarmingManager not found!");
            return;
        }

        targetManager.Initialize();

        var saveData = new FarmingSaveData();

        // Save hoed soil cells
        foreach (var cell in targetManager.GetHoedSoils())
        {
            saveData.hoedCells.Add(new Vector3IntSerializable(cell.x, cell.y, cell.z));
        }

        // Save planted crops
        foreach (var kvp in targetManager.GetPlantedCrops())
        {
            saveData.plantedCrops.Add(new CropDataSerializable
            {
                cellX = kvp.Key.x,
                cellY = kvp.Key.y,
                cropId = kvp.Value.cropId,
                currentStage = kvp.Value.currentStage,
                dayProgress = kvp.Value.dayProgress,
                wasWateredToday = kvp.Value.wasWateredToday,
                daysWithoutWater = kvp.Value.daysWithoutWater,
                isDead = kvp.Value.isDead,
                isReadyToHarvest = kvp.Value.isReadyToHarvest
            });
        }

        // Save planted trees that were spawned at runtime.
        RuntimePlantedTree[] plantedTrees = FindObjectsByType<RuntimePlantedTree>(FindObjectsSortMode.None);
        foreach (var plantedTree in plantedTrees)
        {
            if (plantedTree == null)
                continue;

            FruitTreeInteraction treeInteraction = plantedTree.GetComponent<FruitTreeInteraction>();
            if (treeInteraction == null)
                continue;

            saveData.plantedTrees.Add(treeInteraction.CaptureSaveData());
        }

        // Save uncollected farm harvest pickups.
        RuntimeFarmHarvestPickup[] harvestPickups = FindObjectsByType<RuntimeFarmHarvestPickup>(FindObjectsSortMode.None);
        foreach (var harvestPickup in harvestPickups)
        {
            if (harvestPickup == null)
                continue;

            PickupComponent pickupComponent = harvestPickup.GetComponent<PickupComponent>();
            if (pickupComponent == null || pickupComponent.item == null || pickupComponent.count <= 0)
                continue;

            saveData.harvestPickups.Add(new HarvestPickupDataSerializable
            {
                positionX = harvestPickup.transform.position.x,
                positionY = harvestPickup.transform.position.y,
                positionZ = harvestPickup.transform.position.z,
                itemKey = GetItemKey(pickupComponent.item),
                count = pickupComponent.count,
                ttlRemaining = pickupComponent.GetTimeToLive()
            });
        }

        try
        {
            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(SavePath, json);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[FarmingDataSaveSystem] Failed to save farming data: {exception}");
            return;
        }

        Debug.Log($"[FarmingDataSaveSystem] Saved to {SavePath}");
        Debug.Log($"  - Hoed cells: {saveData.hoedCells.Count}");
        Debug.Log($"  - Planted crops: {saveData.plantedCrops.Count}");
    }

    /// <summary>
    /// Load farming state from JSON file
    /// </summary>
    public void LoadFarmingData(FarmingManager manager = null)
    {
        FarmingManager targetManager = ResolveFarmingManager(manager);

        if (targetManager == null)
        {
            Debug.LogError("[FarmingDataSaveSystem] FarmingManager not found!");
            return;
        }

        if (!File.Exists(SavePath))
        {
            Debug.LogWarning($"[FarmingDataSaveSystem] No save file found at {SavePath}");
            return;
        }

        FarmingSaveData saveData;

        try
        {
            string json = File.ReadAllText(SavePath);
            saveData = JsonUtility.FromJson<FarmingSaveData>(json);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[FarmingDataSaveSystem] Failed to load farming data, leaving farm empty: {exception.Message}");
            return;
        }

        if (saveData == null)
        {
            Debug.LogWarning("[FarmingDataSaveSystem] Save data was null, leaving farm empty.");
            return;
        }

        targetManager.Initialize();

        // Restore hoed soil cells
        var hoedCells = new HashSet<Vector3Int>();
        if (saveData.hoedCells != null)
        {
            foreach (var cell in saveData.hoedCells)
            {
                hoedCells.Add(new Vector3Int(cell.x, cell.y, cell.z));
            }
        }
        targetManager.SetHoedSoils(hoedCells);

        // Restore planted crops
        var plantedCrops = new Dictionary<Vector3Int, CropData>();
        if (saveData.plantedCrops != null)
        {
            foreach (var cropData in saveData.plantedCrops)
            {
                if (string.IsNullOrEmpty(cropData.cropId))
                    continue;

                var cellPos = new Vector3Int(cropData.cellX, cropData.cellY, 0);
                var crop = new CropData
                {
                    cropId = cropData.cropId,
                    currentStage = cropData.currentStage,
                    dayProgress = cropData.dayProgress,
                    wasWateredToday = cropData.wasWateredToday,
                    daysWithoutWater = cropData.daysWithoutWater,
                    isDead = cropData.isDead,
                    isReadyToHarvest = cropData.isReadyToHarvest
                };

                plantedCrops[cellPos] = crop;
            }
        }
        targetManager.SetPlantedCrops(plantedCrops);

        ClearRuntimePlantedTrees();
        ClearRuntimeHarvestPickups();

        pendingTreeRestores.Clear();
        if (saveData.plantedTrees != null)
            pendingTreeRestores.AddRange(saveData.plantedTrees);

        pendingHarvestPickupRestores.Clear();
        if (saveData.harvestPickups != null)
            pendingHarvestPickupRestores.AddRange(saveData.harvestPickups);

        TryRestorePendingTrees();
        TryRestorePendingHarvestPickups(targetManager);

        Debug.Log($"[FarmingDataSaveSystem] Loaded from {SavePath}");
        Debug.Log($"  - Hoed cells: {hoedCells.Count}");
        Debug.Log($"  - Planted crops: {plantedCrops.Count}");
        Debug.Log($"  - Planted trees: {saveData.plantedTrees.Count}");
        Debug.Log($"  - Harvest pickups: {pendingHarvestPickupRestores.Count}");
    }

    /// <summary>
    /// Delete the save file
    /// </summary>
    public void DeleteSaveFile()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log($"[FarmingDataSaveSystem] Deleted save file at {SavePath}");
        }
    }

    private FarmingManager ResolveFarmingManager(FarmingManager manager)
    {
        if (manager != null)
            return manager;

        if (farmingManager != null)
            return farmingManager;

        farmingManager = FindFirstObjectByType<FarmingManager>();
        return farmingManager;
    }

    private void ClearRuntimePlantedTrees()
    {
        RuntimePlantedTree[] plantedTrees = FindObjectsByType<RuntimePlantedTree>(FindObjectsSortMode.None);
        for (int i = 0; i < plantedTrees.Length; i++)
        {
            if (plantedTrees[i] != null)
                Destroy(plantedTrees[i].gameObject);
        }
    }

    private void ClearRuntimeHarvestPickups()
    {
        RuntimeFarmHarvestPickup[] harvestPickups = FindObjectsByType<RuntimeFarmHarvestPickup>(FindObjectsSortMode.None);
        for (int i = 0; i < harvestPickups.Length; i++)
        {
            if (harvestPickups[i] != null)
                Destroy(harvestPickups[i].gameObject);
        }
    }

    private void TryRestorePendingHarvestPickups(FarmingManager targetManager)
    {
        if (pendingHarvestPickupRestores.Count == 0)
            return;

        List<HarvestPickupDataSerializable> remaining = new List<HarvestPickupDataSerializable>();

        for (int i = 0; i < pendingHarvestPickupRestores.Count; i++)
        {
            HarvestPickupDataSerializable pickupData = pendingHarvestPickupRestores[i];

            ItemDefinition item = ResolveItemForPickupKey(pickupData.itemKey, targetManager);
            if (item == null)
            {
                remaining.Add(pickupData);
                continue;
            }

            Vector3 spawnPosition = new Vector3(pickupData.positionX, pickupData.positionY, pickupData.positionZ);
            GameObject pickupObject = new GameObject($"HarvestPickup_{item.name}");
            pickupObject.transform.position = spawnPosition;

            SpriteRenderer spriteRenderer = pickupObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = item.icon;
            spriteRenderer.sortingOrder = 1;

            CircleCollider2D collider = pickupObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = false;
            collider.radius = 0.2f;

            PickupComponent pickupComponent = pickupObject.AddComponent<PickupComponent>();
            pickupComponent.Set(item, Mathf.Max(1, pickupData.count));
            pickupComponent.SetTimeToLive(Mathf.Max(1f, pickupData.ttlRemaining));
            pickupObject.AddComponent<RuntimeFarmHarvestPickup>();
        }

        pendingHarvestPickupRestores.Clear();
        pendingHarvestPickupRestores.AddRange(remaining);
    }

    private ItemDefinition ResolveItemForPickupKey(string key, FarmingManager targetManager)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        if (targetManager != null)
        {
            CropDefinition[] cropDefinitions = targetManager.GetAvailableCrops();
            if (cropDefinitions != null)
            {
                for (int i = 0; i < cropDefinitions.Length; i++)
                {
                    CropDefinition cropDefinition = cropDefinitions[i];
                    if (cropDefinition == null)
                        continue;

                    if (cropDefinition.harvestItem != null && string.Equals(GetItemKey(cropDefinition.harvestItem), key, StringComparison.Ordinal))
                        return cropDefinition.harvestItem;
                }
            }
        }

        FruitTreeInteraction[] treeInteractions = FindObjectsByType<FruitTreeInteraction>(FindObjectsSortMode.None);
        for (int i = 0; i < treeInteractions.Length; i++)
        {
            if (treeInteractions[i] == null)
                continue;

            ItemDefinition fruitItem = treeInteractions[i].GetFruitItemDefinition();
            if (fruitItem != null && string.Equals(GetItemKey(fruitItem), key, StringComparison.Ordinal))
                return fruitItem;
        }

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

    public void TryRestorePendingTrees()
    {
        if (pendingTreeRestores.Count == 0)
            return;

        TreePlanter[] treePlanters = FindObjectsByType<TreePlanter>(FindObjectsSortMode.None);
        if (treePlanters == null || treePlanters.Length == 0)
            return;

        List<TreeDataSerializable> remaining = new List<TreeDataSerializable>();

        foreach (var treeData in pendingTreeRestores)
        {
            if (string.IsNullOrEmpty(treeData.treeKey))
                continue;

            bool restored = false;
            for (int i = 0; i < treePlanters.Length; i++)
            {
                if (treePlanters[i] == null)
                    continue;

                string planterKey = treePlanters[i].GetTreeKey();
                if (!string.Equals(planterKey, treeData.treeKey, StringComparison.OrdinalIgnoreCase))
                    continue;

                restored = treePlanters[i].RestorePlantedTree(treeData);
                if (restored)
                    break;
            }

            if (!restored)
                remaining.Add(treeData);
        }

        pendingTreeRestores.Clear();
        pendingTreeRestores.AddRange(remaining);
    }

    [ContextMenu("Save Farming Data")]
    public void DebugSave() => SaveFarmingData();

    [ContextMenu("Load Farming Data")]
    public void DebugLoad() => LoadFarmingData();

    [ContextMenu("Delete Save File")]
    public void DebugDeleteSave() => DeleteSaveFile();
}

// ============ SERIALIZABLE STRUCTURES ============

[System.Serializable]
public class FarmingSaveData
{
    public List<Vector3IntSerializable> hoedCells = new List<Vector3IntSerializable>();
    public List<CropDataSerializable> plantedCrops = new List<CropDataSerializable>();
    public List<TreeDataSerializable> plantedTrees = new List<TreeDataSerializable>();
    public List<HarvestPickupDataSerializable> harvestPickups = new List<HarvestPickupDataSerializable>();
}

[System.Serializable]
public struct Vector3IntSerializable
{
    public int x;
    public int y;
    public int z;

    public Vector3IntSerializable(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}
