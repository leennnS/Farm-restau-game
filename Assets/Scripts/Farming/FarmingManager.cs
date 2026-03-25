using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

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

    [Header("UI")]
    [SerializeField] private PickupToastUIToolkit pickupToast;

    [Header("Harvest")]
    [SerializeField] private GameObject harvestItemPrefab; // Prefab for harvested crop items

    [Header("Digging")]
    [Tooltip("Tile to replace Grass with when hoed")]
    [SerializeField] private TileBase hoeTileFeedback;   // Optional: visual feedback for hoe action

    // Runtime tracking
    private HashSet<Vector3Int> hoedSoilCells = new HashSet<Vector3Int>();      // Cells that are tilled
    private Dictionary<Vector3Int, CropData> plantedCrops = new Dictionary<Vector3Int, CropData>(); // Crops by cell
    private Dictionary<Vector3Int, GameObject> deadPlantVisuals = new Dictionary<Vector3Int, GameObject>(); // Dead plant sprite GameObjects

    // Crop definition lookup
    private Dictionary<string, CropDefinition> cropDefinitionLookup = new Dictionary<string, CropDefinition>();

    private bool initialized = false;

    private void ResolveReferences()
    {
        if (groundTilemap == null)
            groundTilemap = FindFirstObjectByType<Tilemap>();

        if (cropTilemap == null)
        {
            Tilemap[] tilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
            if (tilemaps.Length > 1)
                cropTilemap = tilemaps[1];
        }

        if (inventoryController == null)
            inventoryController = FindFirstObjectByType<InventoryController>();

        if (pickupToast == null)
            pickupToast = FindFirstObjectByType<PickupToastUIToolkit>();
    }

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        // Subscribe to day advancement event
        DayNightCycleNice2D.OnDayAdvanced += AdvanceDay;
        SceneManager.sceneLoaded += OnSceneLoaded;
        ResolveReferences();
    }

    private void OnDisable()
    {
        // Unsubscribe from day advancement event
        DayNightCycleNice2D.OnDayAdvanced -= AdvanceDay;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolveReferences();
    }

    public void Initialize()
    {
        if (initialized) return;

        ResolveReferences();

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
        {
            if (pickupToast != null)
                pickupToast.Show("Already hoed!");
            return false;
        }

        // Get current tile
        TileBase currentTile = groundTilemap.GetTile(cellPos);

        // Only hoe if it's grass or similar (check by tile comparison)
        if (currentTile != grassTile && currentTile != null)
        {
            // Assume any non-null tile is already worked or invalid
            if (pickupToast != null)
                pickupToast.Show("Cannot hoe this tile");
            return false;
        }

        // Mark as hoed and set soil tile
        hoedSoilCells.Add(cellPos);
        groundTilemap.SetTile(cellPos, soilTile ?? grassTile);

        if (pickupToast != null)
            pickupToast.Show("Soil ready for planting");

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
        ResolveReferences();

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
            if (pickupToast != null)
                pickupToast.Show("Need to hoe first");
            Debug.LogWarning($"[FarmingManager] Cannot plant: cell {cellPos} is not hoed soil");
            return false;
        }

        // Check no existing crop
        if (plantedCrops.ContainsKey(cellPos))
        {
            if (pickupToast != null)
                pickupToast.Show("Already have a crop here");
            Debug.LogWarning($"[FarmingManager] Cannot plant: crop already exists at {cellPos}");
            return false;
        }

        // Check inventory has seed
        if (inventoryController == null || !HasItemInInventory(cropDef.seedItem, 1))
        {
            if (pickupToast != null)
                pickupToast.Show($"No {cropDef.seedItem.displayName} seeds");
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

        if (pickupToast != null)
            pickupToast.Show($"Planted {cropDef.displayName}");

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
        {
            if (pickupToast != null)
                pickupToast.Show("No crop to water");
            return false;
        }

        if (crop.isDead)
        {
            if (pickupToast != null)
                pickupToast.Show("Plant is dead");
            return false;
        }

        crop.wasWateredToday = true;
        plantedCrops[cellPos] = crop;

        if (pickupToast != null)
            pickupToast.Show("Plant watered ✓");

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
        ResolveReferences();

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
            if (pickupToast != null)
                pickupToast.Show("Not ready to harvest");
            Debug.LogWarning($"[FarmingManager] Crop at {cellPos} is not mature yet (stage {crop.currentStage}/{cropDef.TotalStages - 1})");
            return false;
        }

        // Get world position of crop (center of tile)
        Vector3 worldPos = cropTilemap != null ? cropTilemap.CellToWorld(cellPos) : Vector3.zero;
        worldPos.x += 0.5f; // Center horizontally on tile
        worldPos.y += 0.5f; // Center vertically on tile
        worldPos.z = 0f; // Ensure 2D positioning

        // Spawn 4 harvest items with offset positions
        Vector3[] itemOffsets = new Vector3[4]
        {
            new Vector3(-0.2f, 0.2f, 0f),   // top-left
            new Vector3(0.2f, 0.2f, 0f),    // top-right
            new Vector3(-0.2f, -0.2f, 0f),  // bottom-left
            new Vector3(0.2f, -0.2f, 0f)    // bottom-right
        };

        for (int i = 0; i < 4; i++)
        {
            Vector3 itemPos = worldPos + itemOffsets[i];

            // Spawn harvest item - use prefab if available, otherwise create dynamically
            GameObject harvestGO;
            if (cropDef.harvestPrefab != null)
            {
                Debug.Log($"[FarmingManager] Instantiating harvest prefab {i + 1}/4 for {cropDef.displayName}");
                // Instantiate the harvest prefab
                harvestGO = Instantiate(cropDef.harvestPrefab, itemPos, Quaternion.identity);

                // Ensure SpriteRenderer is visible on top of soil
                SpriteRenderer prefabSR = harvestGO.GetComponent<SpriteRenderer>();
                if (prefabSR != null)
                {
                    prefabSR.sortingOrder = 1; // Render above soil/tilemap
                }

                // Ensure it has PickupComponent with correct item/count
                PickupComponent pickup = harvestGO.GetComponent<PickupComponent>();
                if (pickup != null)
                {
                    pickup.Set(cropDef.harvestItem, cropDef.harvestAmount);
                    Debug.Log($"[FarmingManager] Set harvest prefab item {i + 1}/4 to {cropDef.harvestItem.displayName} x{cropDef.harvestAmount}");
                }
                else
                {
                    Debug.LogWarning($"[FarmingManager] Harvest prefab missing PickupComponent! Adding one dynamically.");
                    pickup = harvestGO.AddComponent<PickupComponent>();
                    pickup.Set(cropDef.harvestItem, cropDef.harvestAmount);
                }
            }
            else
            {
                Debug.Log($"[FarmingManager] No harvest prefab assigned for {cropDef.displayName}. Creating dynamically ({i + 1}/4).");
                // Fallback: create dynamically
                harvestGO = new GameObject($"{cropDef.displayName}");
                harvestGO.transform.position = itemPos;
                harvestGO.transform.rotation = Quaternion.identity;

                // Add sprite renderer
                SpriteRenderer harvestSR = harvestGO.AddComponent<SpriteRenderer>();
                if (cropDef.harvestItem != null && cropDef.harvestItem.icon != null)
                {
                    harvestSR.sprite = cropDef.harvestItem.icon;
                    harvestSR.sortingOrder = 1;
                }

                // Add circle collider for pickup
                CircleCollider2D collider = harvestGO.AddComponent<CircleCollider2D>();
                collider.isTrigger = false;
                collider.radius = 0.2f;

                // Add pickup component
                PickupComponent pickup = harvestGO.AddComponent<PickupComponent>();
                pickup.Set(cropDef.harvestItem, cropDef.harvestAmount);
            }
        }

        // Show toast
        if (pickupToast != null)
            pickupToast.Show($"Harvested {cropDef.displayName}! ✓");

        // Clean up any dead plant visual
        if (deadPlantVisuals.TryGetValue(cellPos, out GameObject deadGO))
        {
            Destroy(deadGO);
            deadPlantVisuals.Remove(cellPos);
        }

        // Remove crop from world
        plantedCrops.Remove(cellPos);
        if (cropTilemap != null)
            cropTilemap.SetTile(cellPos, null);

        // Optionally revert soil
        if (cropDef.reveritToSoilAfterHarvest && groundTilemap != null)
        {
            groundTilemap.SetTile(cellPos, soilTile);
        }

        Debug.Log($"[FarmingManager] Harvested {cropDef.displayName} at {cellPos}. Spawned pickup.");
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

        List<Vector3Int> cellsToCheck = new List<Vector3Int>(plantedCrops.Keys);

        foreach (var cellPos in cellsToCheck)
        {
            if (!plantedCrops.TryGetValue(cellPos, out CropData crop))
                continue;

            if (!cropDefinitionLookup.TryGetValue(crop.cropId, out CropDefinition cropDef))
                continue;

            // Check if plant just died
            bool wasDead = crop.isDead;
            crop.AdvanceDay(cropDef, crop.wasWateredToday);
            plantedCrops[cellPos] = crop;

            // Show toast if plant died this day
            if (!wasDead && crop.isDead)
            {
                if (pickupToast != null)
                    pickupToast.Show($"{cropDef.displayName} died ✗");
            }

            // Update display
            UpdateCropTileAtCell(cellPos, cropDef);
        }

        Debug.Log($"[FarmingManager] Day advanced. {plantedCrops.Count} crops updated.");
    }

    // ============ INTERNAL HELPERS ============

    private void UpdateCropTileAtCell(Vector3Int cellPos, CropDefinition cropDef)
    {
        if (cropTilemap == null || cropDef == null) return;

        if (!plantedCrops.TryGetValue(cellPos, out CropData crop))
            return;

        // If dead, show dead plant sprite as GameObject
        if (crop.isDead)
        {
            if (cropDef.deadPlantSprite != null)
            {
                // Create or get the dead plant visual GameObject
                if (!deadPlantVisuals.ContainsKey(cellPos))
                {
                    Vector3 worldPos = cropTilemap.CellToWorld(cellPos); worldPos.x += 0.5f; // Center horizontally on tile
                    worldPos.y += 0.5f; // Center vertically on tile                    worldPos.z = -1f; // Set Z behind tilemap so it's visible

                    GameObject deadPlantGO = new GameObject($"Dead{cropDef.displayName}");
                    deadPlantGO.transform.position = worldPos;
                    deadPlantGO.transform.rotation = Quaternion.identity;

                    SpriteRenderer sr = deadPlantGO.AddComponent<SpriteRenderer>();
                    sr.sprite = cropDef.deadPlantSprite;
                    sr.sortingOrder = 1; // Render on top of soil/grass layer
                    sr.sortingLayerName = "Default"; // Ensure it's on a visible sorting layer

                    Debug.Log($"[FarmingManager] Created dead plant visual for {cropDef.displayName} at {cellPos}");
                    deadPlantVisuals[cellPos] = deadPlantGO;
                }
                else
                {
                    Debug.Log($"[FarmingManager] Dead plant visual already exists at {cellPos}");
                }

                // Clear the tilemap tile
                cropTilemap.SetTile(cellPos, null);
            }
            else
            {
                Debug.LogWarning($"[FarmingManager] No deadPlantSprite assigned for {cropDef.displayName}! Assign it in the CropDefinition inspector.");
            }
            return;
        }

        // Alive crop: clear any dead plant visual and show stage tile
        if (deadPlantVisuals.TryGetValue(cellPos, out GameObject deadGO))
        {
            Destroy(deadGO);
            deadPlantVisuals.Remove(cellPos);
        }

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

        // Clean up old dead plant visuals first
        foreach (var deadGO in deadPlantVisuals.Values)
        {
            if (deadGO != null)
                Destroy(deadGO);
        }
        deadPlantVisuals.Clear();

        // Refresh all crops with proper tile/sprite display
        foreach (var kvp in plantedCrops)
        {
            Vector3Int cellPos = kvp.Key;
            CropData crop = kvp.Value;

            if (cropDefinitionLookup.TryGetValue(crop.cropId, out CropDefinition cropDef))
            {
                UpdateCropTileAtCell(cellPos, cropDef);
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

        // Clean up dead plant visuals
        foreach (var deadGO in deadPlantVisuals.Values)
        {
            if (deadGO != null)
                Destroy(deadGO);
        }
        deadPlantVisuals.Clear();

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
