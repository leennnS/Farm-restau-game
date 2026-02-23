using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// Core farming system manager.
/// Handles: tilling soil, planting crops, watering, harvesting, day advancement.
/// Tracks hoed tiles and planted crops by cell position.
/// </summary>
public class FarmingManager : MonoBehaviour
{
    [Header("Tilemaps")]
    [SerializeField] private Tilemap groundTilemap;      // Contains Grass and Soil tiles
    [SerializeField] private Tilemap cropTilemap;        // Displays crops (separate layer)
    [SerializeField] private TileBase grassTile;         // Tile for untilled grass
    [SerializeField] private TileBase soilTile;          // Tile for tilled soil

    [Header("Crops")]
    [SerializeField] private CropDefinition[] availableCrops;

    [Header("Inventory")]
    [SerializeField] private InventoryController inventoryController;

    [Header("Digging")]
    [Tooltip("Tile to replace Grass with when hoed")]
    [SerializeField] private TileBase hoeTileFeedback;   // Optional: visual feedback for hoe action

    // Runtime tracking
    private HashSet<Vector3Int> hoedSoilCells = new HashSet<Vector3Int>();      // Cells that are tilled
    private Dictionary<Vector3Int, CropData> plantedCrops = new Dictionary<Vector3Int, CropData>(); // Crops by cell

    // Crop definition lookup
    private Dictionary<string, CropDefinition> cropDefinitionLookup = new Dictionary<string, CropDefinition>();

    private bool initialized = false;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        // Subscribe to day advancement event
        DayNightCycleNice2D.OnDayAdvanced += AdvanceDay;
    }

    private void OnDisable()
    {
        // Unsubscribe from day advancement event
        DayNightCycleNice2D.OnDayAdvanced -= AdvanceDay;
    }

    public void Initialize()
    {
        if (initialized) return;

        // Auto-find references if not set
        if (groundTilemap == null)
            groundTilemap = FindObjectOfType<Tilemap>();
        if (cropTilemap == null)
        {
            // Try to find a second tilemap (assumes Ground is first, Crop is second)
            Tilemap[] tilemaps = FindObjectsOfType<Tilemap>();
            if (tilemaps.Length > 1)
                cropTilemap = tilemaps[1];
        }
        if (inventoryController == null)
            inventoryController = FindObjectOfType<InventoryController>();

        // Build crop lookup
        if (availableCrops != null)
        {
            foreach (var crop in availableCrops)
            {
                if (crop != null && !string.IsNullOrEmpty(crop.cropId))
                    cropDefinitionLookup[crop.cropId] = crop;
            }
        }

        Debug.Log($"[FarmingManager] Initialized. Crops available: {cropDefinitionLookup.Count}");
        initialized = true;
    }

    // ============ HOING (Grass -> Soil) ============

    /// <summary>
    /// Hoe a grass tile at world position, converting it to soil.
    /// Returns true if successful, false if already soil or invalid.
    /// </summary>
    public bool TryHoeAtWorldPosition(Vector3 worldPos)
    {
        if (groundTilemap == null) return false;

        Vector3Int cellPos = groundTilemap.WorldToCell(worldPos);
        return TryHoeAtCell(cellPos);
    }

    public bool TryHoeAtCell(Vector3Int cellPos)
    {
        if (groundTilemap == null) return false;

        // Check if already hoed
        if (hoedSoilCells.Contains(cellPos))
            return false;

        // Get current tile
        TileBase currentTile = groundTilemap.GetTile(cellPos);

        // Only hoe if it's grass or similar (check by tile comparison)
        if (currentTile != grassTile && currentTile != null)
        {
            // Assume any non-null tile is already worked or invalid
            return false;
        }

        // Mark as hoed and set soil tile
        hoedSoilCells.Add(cellPos);
        groundTilemap.SetTile(cellPos, soilTile ?? grassTile);

        Debug.Log($"[FarmingManager] Hoed soil at {cellPos}");
        return true;
    }

    public bool IsHoedSoil(Vector3Int cellPos) => hoedSoilCells.Contains(cellPos);

    // ============ PLANTING ============

    /// <summary>
    /// Plant a crop at world position.
    /// Requires: cell is hoed soil, no existing crop, inventory has seed.
    /// Consumes 1 seed from inventory.
    /// </summary>
    public bool TryPlantAtWorldPosition(Vector3 worldPos, CropDefinition cropDef)
    {
        if (cropTilemap == null || inventoryController == null || cropDef == null)
            return false;

        Vector3Int cellPos = cropTilemap.WorldToCell(worldPos);
        return TryPlantAtCell(cellPos, cropDef);
    }

    public bool TryPlantAtCell(Vector3Int cellPos, CropDefinition cropDef)
    {
        if (cropDef == null || !cropDef.IsValid)
            return false;

        // Check cell is hoed
        if (!IsHoedSoil(cellPos))
        {
            Debug.LogWarning($"[FarmingManager] Cannot plant: cell {cellPos} is not hoed soil");
            return false;
        }

        // Check no existing crop
        if (plantedCrops.ContainsKey(cellPos))
        {
            Debug.LogWarning($"[FarmingManager] Cannot plant: crop already exists at {cellPos}");
            return false;
        }

        // Check inventory has seed
        if (inventoryController == null || !HasItemInInventory(cropDef.seedItem, 1))
        {
            Debug.LogWarning($"[FarmingManager] Cannot plant: no seed ({cropDef.seedItem.displayName}) in inventory");
            return false;
        }

        // Consume seed
        RemoveItemFromInventory(cropDef.seedItem, 1);

        // Create and plant crop
        CropData newCrop = new CropData(cropDef.cropId, 0);
        plantedCrops[cellPos] = newCrop;

        // Display crop on tilemap
        UpdateCropTileAtCell(cellPos, cropDef);

        Debug.Log($"[FarmingManager] Planted {cropDef.displayName} at {cellPos}");
        return true;
    }

    // ============ WATERING ============

    /// <summary>
    /// Water a crop at world position.
    /// Marks the crop as watered for today (affects growth).
    /// </summary>
    public bool TryWaterAtWorldPosition(Vector3 worldPos)
    {
        if (cropTilemap == null)
            return false;

        Vector3Int cellPos = cropTilemap.WorldToCell(worldPos);
        return TryWaterAtCell(cellPos);
    }

    public bool TryWaterAtCell(Vector3Int cellPos)
    {
        if (!plantedCrops.TryGetValue(cellPos, out CropData crop))
            return false;

        crop.wasWateredToday = true;
        plantedCrops[cellPos] = crop;

        Debug.Log($"[FarmingManager] Watered crop at {cellPos}");
        return true;
    }

    // ============ HARVESTING ============

    /// <summary>
    /// Harvest a mature crop at world position.
    /// Requires crop to be at final stage.
    /// Adds harvest items to inventory, removes crop from world.
    /// </summary>
    public bool TryHarvestAtWorldPosition(Vector3 worldPos)
    {
        if (inventoryController == null || cropTilemap == null)
            return false;

        Vector3Int cellPos = cropTilemap.WorldToCell(worldPos);
        return TryHarvestAtCell(cellPos);
    }

    public bool TryHarvestAtCell(Vector3Int cellPos)
    {
        if (!plantedCrops.TryGetValue(cellPos, out CropData crop))
            return false;

        if (!cropDefinitionLookup.TryGetValue(crop.cropId, out CropDefinition cropDef))
            return false;

        // Check if mature
        if (!crop.IsMature(cropDef))
        {
            Debug.LogWarning($"[FarmingManager] Crop at {cellPos} is not mature yet (stage {crop.currentStage}/{cropDef.TotalStages - 1})");
            return false;
        }

        // Add harvest item to inventory
        inventoryController.TryAdd(cropDef.harvestItem, cropDef.harvestAmount);

        // Remove crop from world
        plantedCrops.Remove(cellPos);
        if (cropTilemap != null)
            cropTilemap.SetTile(cellPos, null);

        // Optionally revert soil
        if (cropDef.reveritToSoilAfterHarvest && groundTilemap != null)
        {
            groundTilemap.SetTile(cellPos, soilTile);
        }

        Debug.Log($"[FarmingManager] Harvested {cropDef.displayName} at {cellPos}. Added {cropDef.harvestAmount}x {cropDef.harvestItem.displayName} to inventory");
        return true;
    }

    // ============ DAY ADVANCEMENT ============

    /// <summary>
    /// Called each in-game day to advance crop growth.
    /// Resets water flags for next day.
    /// </summary>
    public void AdvanceDay()
    {
        Debug.Log("[FarmingManager] Advancing day...");

        List<Vector3Int> cellsToRemove = new List<Vector3Int>();

        foreach (var kvp in plantedCrops)
        {
            Vector3Int cellPos = kvp.Key;
            CropData crop = kvp.Value;

            if (cropDefinitionLookup.TryGetValue(crop.cropId, out CropDefinition cropDef))
            {
                crop.AdvanceDay(cropDef, crop.wasWateredToday);
                plantedCrops[cellPos] = crop;

                // Update display
                UpdateCropTileAtCell(cellPos, cropDef);
            }
        }

        Debug.Log($"[FarmingManager] Day advanced. {plantedCrops.Count} crops updated.");
    }

    // ============ INTERNAL HELPERS ============

    private void UpdateCropTileAtCell(Vector3Int cellPos, CropDefinition cropDef)
    {
        if (cropTilemap == null || cropDef == null) return;

        if (!plantedCrops.TryGetValue(cellPos, out CropData crop))
            return;

        TileBase stageTile = cropDef.GetStageTile(crop.currentStage);
        cropTilemap.SetTile(cellPos, stageTile);
    }

    private bool HasItemInInventory(ItemDefinition item, int amount)
    {
        if (inventoryController == null || item == null) return false;

        // Access inventory through reflection or public method (assuming InventoryController has public access)
        // For now, we'll assume InventoryController is modified to expose this
        int totalFound = inventoryController.CountItemInInventory(item);
        return totalFound >= amount;
    }

    private bool RemoveItemFromInventory(ItemDefinition item, int amount)
    {
        if (inventoryController == null || item == null || amount <= 0)
            return false;

        // Use public method if available, or implement via InventoryController
        return inventoryController.TryRemoveItem(item, amount);
    }

    // ============ DATA ACCESSORS ============

    public Dictionary<Vector3Int, CropData> GetPlantedCrops() => new Dictionary<Vector3Int, CropData>(plantedCrops);
    public HashSet<Vector3Int> GetHoedSoils() => new HashSet<Vector3Int>(hoedSoilCells);

    /// <summary>
    /// Find a crop definition by seed item (for planting system)
    /// </summary>
    public CropDefinition GetCropBySeeds(ItemDefinition seedItem)
    {
        foreach (var crop in availableCrops)
        {
            if (crop != null && crop.seedItem == seedItem)
                return crop;
        }
        return null;
    }

    public void SetPlantedCrops(Dictionary<Vector3Int, CropData> crops)
    {
        plantedCrops = new Dictionary<Vector3Int, CropData>(crops);
        RefreshAllCropTiles();
    }

    public void SetHoedSoils(HashSet<Vector3Int> soilCells)
    {
        hoedSoilCells = new HashSet<Vector3Int>(soilCells);
        RefreshAllSoilTiles();
    }

    private void RefreshAllCropTiles()
    {
        if (cropTilemap == null) return;
        cropTilemap.ClearAllTiles();

        foreach (var kvp in plantedCrops)
        {
            Vector3Int cellPos = kvp.Key;
            CropData crop = kvp.Value;

            if (cropDefinitionLookup.TryGetValue(crop.cropId, out CropDefinition cropDef))
            {
                TileBase stageTile = cropDef.GetStageTile(crop.currentStage);
                cropTilemap.SetTile(cellPos, stageTile);
            }
        }
    }

    private void RefreshAllSoilTiles()
    {
        if (groundTilemap == null) return;

        foreach (var cellPos in hoedSoilCells)
        {
            groundTilemap.SetTile(cellPos, soilTile);
        }
    }

    // ============ DEBUG ============

    [ContextMenu("Clear All Hoed Soil")]
    public void DebugClearSoil()
    {
        hoedSoilCells.Clear();
        if (groundTilemap != null)
            groundTilemap.ClearAllTiles();
        Debug.Log("[FarmingManager] Cleared all hoed soil");
    }

    [ContextMenu("Clear All Crops")]
    public void DebugClearCrops()
    {
        plantedCrops.Clear();
        if (cropTilemap != null)
            cropTilemap.ClearAllTiles();
        Debug.Log("[FarmingManager] Cleared all crops");
    }

    [ContextMenu("Log Farming State")]
    public void DebugLogState()
    {
        Debug.Log($"[FarmingManager] Hoed cells: {hoedSoilCells.Count}");
        Debug.Log($"[FarmingManager] Planted crops: {plantedCrops.Count}");
        foreach (var kvp in plantedCrops)
        {
            Debug.Log($"  - {kvp.Key}: {kvp.Value.cropId} (stage {kvp.Value.currentStage}, progress {kvp.Value.dayProgress})");
        }
    }
}
