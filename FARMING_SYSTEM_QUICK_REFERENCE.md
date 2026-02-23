# Farming System - Quick Reference

## Core Classes

### CropDefinition (ScriptableObject)

```
Defines: cropId, displayName, stageTiles[], daysPerStage[], seedItem, harvestItem, harvestAmount
Create via: Right-click > Create > Farming/Crop Definition
```

### FarmingManager (MonoBehaviour)

```
Main farming logic component. Attach to scene GameObject.

PUBLIC METHODS:
- TryHoeAtWorldPosition(Vector3) → bool
- TryPlantAtWorldPosition(Vector3, CropDefinition) → bool
- TryWaterAtWorldPosition(Vector3) → bool
- TryHarvestAtWorldPosition(Vector3) → bool
- AdvanceDay() → void
- GetCropBySeeds(ItemDefinition) → CropDefinition
- GetPlantedCrops() → Dictionary<Vector3Int, CropData>
- GetHoedSoils() → HashSet<Vector3Int>
```

### FarmingInputHandler (MonoBehaviour)

```
Handles player input (mouse clicks, hotbar selection).
Attach to Player GameObject.

HOTKEY MAPPING:
- Keys 1-0: Select hotbar slots 0-9
- Left Click: Execute action with selected tool/item

ACTION MATCHING:
- "hoe" in item name → Hoe action
- "watering_can" in item name → Water action
- "hand" in item name → Harvest action
- "seed" or "sapling" in item name → Plant action
```

### FarmingDataSaveSystem (MonoBehaviour)

```
Saves/loads farming state to JSON.
Attach to scene GameObject.

PUBLIC METHODS:
- SaveFarmingData() → void
- LoadFarmingData() → void
- DeleteSaveFile() → void

CONTEXT MENU (right-click component):
- Save Farming Data
- Load Farming Data
- Delete Save File
```

## Data Structures

### CropData (struct)

```
Runtime state of one planted crop:
- cropId: string (references CropDefinition)
- currentStage: int (0 to TotalStages-1)
- dayProgress: int (0 to daysPerStage[stage])
- wasWateredToday: bool
```

## Events

### DayNightCycleNice2D.OnDayAdvanced

```
Static event fired when in-game day advances.

SUBSCRIBE:
DayNightCycleNice2D.OnDayAdvanced += MyMethod;

EXAMPLE:
void MyMethod() { Debug.Log("New day!"); }
```

## InventoryController Extensions

### CountItemInInventory(ItemDefinition item) → int

```
Count total amount of item in all inventory slots.
EXAMPLE: int seedCount = inv.CountItemInInventory(tomatoSeed);
```

### TryRemoveItem(ItemDefinition item, int amount) → bool

```
Remove amount of item from inventory.
Returns true if successful, false if not enough items.
EXAMPLE: inv.TryRemoveItem(tomatoSeed, 1); // Consume seed
```

### GetHotbarItem(int slotIndex) → ItemDefinition

```
Get item at hotbar slot (0-11).
Returns null if empty.
EXAMPLE: ItemDefinition selected = inv.GetHotbarItem(selectedSlotIndex);
```

## Common Workflows

### 1. Create a New Crop

```
1. Right-click Assets > Create > Farming/Crop Definition
2. Set cropId (unique string)
3. Assign stageTiles (array of TileBase from tileset)
4. Assign seedItem (ItemDefinition for seed)
5. Assign harvestItem (ItemDefinition for result)
6. Add to FarmingManager > Available Crops array
```

### 2. Manually Hoe a Tile (Code)

```
FarmingManager fm = FindObjectOfType<FarmingManager>();
Vector3 worldPos = new Vector3(5f, 3f, 0f);
if (fm.TryHoeAtWorldPosition(worldPos))
    Debug.Log("Hoeing successful!");
```

### 3. Plant a Crop (Code)

```
FarmingManager fm = FindObjectOfType<FarmingManager>();
CropDefinition tomato = fm.GetCropBySeeds(tomatoSeedItem);
if (fm.TryPlantAtWorldPosition(mouseWorldPos, tomato))
    Debug.Log("Plant successful!");
```

### 4. Save Game State

```
FarmingDataSaveSystem saveSystem = FindObjectOfType<FarmingDataSaveSystem>();
saveSystem.SaveFarmingData();
// Check Application.persistentDataPath/farming_save.json
```

## Debugging

### Logs to Check

```
[FarmingManager] - Core farming operations
[FarmingInputHandler] - Input processing
[FarmingDataSaveSystem] - Save/load operations
[DayNightCycleNice2D] - Day advancement
```

### Context Menu Commands (Inspector)

**On FarmingManager:**

- Log Farming State → Prints all cells & crops to console
- Clear All Hoed Soil → Debug cleanup
- Clear All Crops → Debug cleanup

**On FarmingDataSaveSystem:**

- Save Farming Data → Manual save
- Load Farming Data → Manual load
- Delete Save File → Clear saved data

### Common Error Messages

| Error                        | Cause                      | Fix                                    |
| ---------------------------- | -------------------------- | -------------------------------------- |
| "FarmingManager not found"   | Component not in scene     | Add FarmingManager to empty GameObject |
| "No seed found in inventory" | Player lacks seed item     | Add seed to hotbar via inventory       |
| "Cell not hoed"              | Tried to plant on grass    | Hoe the soil first (press 1, click)    |
| "Crop not mature"            | Tried to harvest seedling  | Wait for day cycle to advance          |
| "Save file not found"        | Attempted load before save | Save first, or delete save file        |

## Asset Organization (Recommended)

```
Assets/
├── Art/
│   └── Crops/
│       ├── TomatoSpriteSheet.png (sliced into 3 tiles)
│       └── CarrotSpriteSheet.png
├── Items/
│   ├── hoe.asset
│   ├── watering_can.asset
│   ├── hand.asset
│   ├── tomato_seed.asset
│   └── tomato.asset
├── Prefabs/
│   └── FarmingManager.prefab (pre-configured)
└── Scripts/
    └── Farming/
        ├── CropDefinition.cs
        ├── CropData.cs
        ├── FarmingManager.cs
        ├── FarmingInputHandler.cs
        └── FarmingDataSaveSystem.cs
```

## Performance Notes

- **Hoed cells:** Stored in HashSet - O(1) lookup, scales to 10000+ cells
- **Crops:** Stored in Dictionary - O(1) lookup by position
- **Day advancement:** Linear O(n) where n = # crops (typically <100)
- **Save file:** ~2-5KB per 100 crops on disk

---

**For full setup instructions, see FARMING_SYSTEM_SETUP.md**  
**For implementation details, see FARMING_SYSTEM_IMPLEMENTATION.md**
