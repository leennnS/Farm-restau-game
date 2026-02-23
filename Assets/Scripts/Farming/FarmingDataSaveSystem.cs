using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Saves and loads farming state (hoed soil, planted crops) to/from JSON.
/// </summary>
public class FarmingDataSaveSystem : MonoBehaviour
{
    [SerializeField] private FarmingManager farmingManager;
    [SerializeField] private string saveFileName = "farming_save.json";

    private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

    private void Awake()
    {
        if (farmingManager == null)
            farmingManager = FindObjectOfType<FarmingManager>();
    }

    /// <summary>
    /// Save current farming state to JSON file
    /// </summary>
    public void SaveFarmingData()
    {
        if (farmingManager == null)
        {
            Debug.LogError("[FarmingDataSaveSystem] FarmingManager not found!");
            return;
        }

        farmingManager.Initialize();

        var saveData = new FarmingSaveData();

        // Save hoed soil cells
        foreach (var cell in farmingManager.GetHoedSoils())
        {
            saveData.hoedCells.Add(new Vector3IntSerializable(cell.x, cell.y, cell.z));
        }

        // Save planted crops
        foreach (var kvp in farmingManager.GetPlantedCrops())
        {
            saveData.plantedCrops.Add(new CropDataSerializable
            {
                cellX = kvp.Key.x,
                cellY = kvp.Key.y,
                cropId = kvp.Value.cropId,
                currentStage = kvp.Value.currentStage,
                dayProgress = kvp.Value.dayProgress,
                wasWateredToday = kvp.Value.wasWateredToday
            });
        }

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(SavePath, json);

        Debug.Log($"[FarmingDataSaveSystem] Saved to {SavePath}");
        Debug.Log($"  - Hoed cells: {saveData.hoedCells.Count}");
        Debug.Log($"  - Planted crops: {saveData.plantedCrops.Count}");
    }

    /// <summary>
    /// Load farming state from JSON file
    /// </summary>
    public void LoadFarmingData()
    {
        if (farmingManager == null)
        {
            Debug.LogError("[FarmingDataSaveSystem] FarmingManager not found!");
            return;
        }

        if (!File.Exists(SavePath))
        {
            Debug.LogWarning($"[FarmingDataSaveSystem] No save file found at {SavePath}");
            return;
        }

        string json = File.ReadAllText(SavePath);
        var saveData = JsonUtility.FromJson<FarmingSaveData>(json);

        farmingManager.Initialize();

        // Restore hoed soil cells
        var hoedCells = new HashSet<Vector3Int>();
        foreach (var cell in saveData.hoedCells)
        {
            hoedCells.Add(new Vector3Int(cell.x, cell.y, cell.z));
        }
        farmingManager.SetHoedSoils(hoedCells);

        // Restore planted crops
        var plantedCrops = new Dictionary<Vector3Int, CropData>();
        foreach (var cropData in saveData.plantedCrops)
        {
            var cellPos = new Vector3Int(cropData.cellX, cropData.cellY, 0);
            var crop = new CropData
            {
                cropId = cropData.cropId,
                currentStage = cropData.currentStage,
                dayProgress = cropData.dayProgress,
                wasWateredToday = cropData.wasWateredToday
            };
            plantedCrops[cellPos] = crop;
        }
        farmingManager.SetPlantedCrops(plantedCrops);

        Debug.Log($"[FarmingDataSaveSystem] Loaded from {SavePath}");
        Debug.Log($"  - Hoed cells: {hoedCells.Count}");
        Debug.Log($"  - Planted crops: {plantedCrops.Count}");
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
