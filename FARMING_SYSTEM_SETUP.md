# Farming System Setup Guide

## Overview

This is a **complete Stardew-like farming system** integrated into your game. It includes:

- ✅ Hoe action: Grass → Soil
- ✅ Plant action: consume seed, grow crop through stages
- ✅ Water action: mark crop as watered
- ✅ Harvest action: mature crop → add items to inventory
- ✅ Day advancement: crops grow each in-game day
- ✅ Save/Load: JSON persistence of soil and crop state

---

## FILES CREATED

### New Scripts (in `Assets/Scripts/Farming/`)

1. **CropDefinition.cs** - ScriptableObject defining crop properties
2. **CropData.cs** - Runtime data structure for planted crops
3. **FarmingManager.cs** - Core farming logic (hoe, plant, water, harvest, day advance)
4. **FarmingDataSaveSystem.cs** - JSON save/load system
5. **FarmingInputHandler.cs** - Input handling for farming actions

### Modified Scripts

1. **InventoryController.cs** - Added:
   - `CountItemInInventory(ItemDefinition item)` - count seeds
   - `TryRemoveItem(ItemDefinition, amount)` - consume seeds
   - `GetHotbarItem(int slot)` - get current tool/item
2. **DayNightCycleNice2D.cs** - Added:
   - `OnDayAdvanced` event for day cycle notifications

### New Items (in `Assets/Items/`)

1. **hoe.asset** - Farming tool
2. **watering_can.asset** - Farming tool
3. **hand.asset** - Farming tool to harvest
4. **tomato_seed.asset** - Seed item (example)
5. **tomato_harvest.asset** - Harvest result (example)

---

## UNITY SETUP STEPS

### Step 1: Create Tilemaps

You need **two separate Tilemaps** in your farm scene:

**A) Ground Tilemap** (existing or new):

- Contains grass and soil tiles
- Layer name: `Ground` (or similar)
- Add tiles:
  - Grass tile (default untilled state)
  - Soil tile (tilled state after hoeing)

**B) Crop Tilemap** (NEW - CREATE THIS):

- Displays growing crops only
- Layer name: `Crops`
- Leave empty at start (FarmingManager fills it)
- Sort order should be ABOVE Ground tilemap

**In Hierarchy:**

```
Farm Scene
├── Ground (Tilemap)
│   └── Grid
│       ├── Ground (TilemapRenderer)
│       └── Ground (Tilemap) ← Grass & Soil tiles
└── Crops (Tilemap)
    └── Grid
        ├── Crops (TilemapRenderer)
        └── Crops (Tilemap) ← Empty, FarmingManager populates
```

### Step 2: Create Crop Definitions

**Option A: Single Tomato Crop (Recommended)**

1. Right-click in `Assets/` → **Create > Farming/Crop Definition**
2. Name it `Tomato`
3. Configure:
   - **cropId:** `tomato`
   - **displayName:** `Tomato`
   - **seedItem:** Drag `tomato_seed` from Assets/Items/
   - **harvestItem:** Drag `tomato_harvest` from Assets/Items/
   - **harvestAmount:** `3`
   - **stageTiles:** Array of 3 tiles (seedling, young, mature) ← **GET THESE FROM YOUR TILESETS**
   - **daysPerStage:** [2, 2, 0] (2 days per stage, 0 for mature = no more growth)
   - **requiresWatering:** false (for now)
   - **reveritToSoilAfterHarvest:** true

   **Where to get stage tiles:**
   - Import your crop sprite sheet into `Assets/Art/`
   - Slice into individual tiles via Sprite Editor
   - Assign them as sprite tiles to your tileset
   - Reference the 3 stage tiles in the array

**Note:** You do NOT need a Resources/Crops folder. FarmingManager references crops directly.

### Step 3: Add FarmingManager to Canvas/Game Manager

Add a new **empty GameObject** called `FarmingManager`:

- Add component: **FarmingManager**
- Inspector settings:
  - **Ground Tilemap:** Drag your Ground Tilemap here (or leave empty to auto-find)
  - **Crop Tilemap:** Drag your Crops Tilemap here (or leave empty to auto-find)
  - **Grass Tile:** Drag the grass tile from your tileset
  - **Soil Tile:** Drag the soil tile from your tileset
  - **Available Crops:** Array size 1, Element 0 = `Tomato` CropDefinition you created

### Step 4: Add FarmingInputHandler to Player

Add to your **Player GameObject**:

- Add component: **FarmingInputHandler**
- Inspector settings:
  - **Farming Manager:** Drag FarmingManager here
  - **Inventory Controller:** Auto-finds (leave empty)
  - **Hotbar HUD:** Auto-finds (leave empty)
  - **Main Camera:** Auto-finds (leave empty)
  - Leave item name defaults as-is

### Step 5: Add FarmingDataSaveSystem

Add a new **empty GameObject** called `FarmingDataManager`:

- Add component: **FarmingDataSaveSystem**
- Inspector settings:
  - **Farming Manager:** Drag FarmingManager here
  - **Save File Name:** `farming_save.json` (default)

### Step 6: Populate Hotbar with Tools (for testing)

1. Start the game in the scene
2. Open Inventory (Press **I**)
3. Add test items via **Quick Test** (Press **K**) or drag from Assets
4. Drag to hotbar:
   - Slot 1: `hoe`
   - Slot 2: `tomato_seed`
   - Slot 3: `watering_can`
   - Slot 4: `hand` (for harvesting)

---

## GAMEPLAY CONTROLS

| Action                | Input                                  |
| --------------------- | -------------------------------------- |
| Select Hotbar         | Number keys **1-0**                    |
| Left Click            | Perform action with selected tool/item |
| Open Inventory        | **I**                                  |
| Quick Add Item (test) | **K**                                  |

---

## FARMING WORKFLOW

1. **Hoe Soil:** Select Hoe (slot 1) → Click on grass tile → becomes soil
2. **Plant Crop:** Select Tomato Seed (slot 2) → Click on soil → crop appears, seed consumed
3. **Water (Optional):** Select Watering Can (slot 3) → Click crop → watered (helps if requiresWatering=true)
4. **Wait:** In-game days pass (1 real day every 60 seconds by default, see DayNightCycleNice2D config)
5. **Harvest:** When mature (stage 3), select Hand (slot 4) → Click crop → removes crop, adds 3x Tomato to inventory

---

## SAVE/LOAD

### Manual Save/Load (via Context Menu)

In the Inspector:

- Select **FarmingDataManager** GameObject
- In FarmingDataSaveSystem component, right-click script name → **Save Farming Data**, **Load Farming Data**, **Delete Save File**

### Programmatic Save/Load

Add to your save/load system:

```csharp
FarmingDataSaveSystem saveSystem = FindObjectOfType<FarmingDataSaveSystem>();
saveSystem.SaveFarmingData();  // Save
saveSystem.LoadFarmingData();  // Load
```

Save file location: `Application.persistentDataPath/farming_save.json`

---

## COMMON ISSUES & SOLUTIONS

### Issue: "Tilemap not found"

- **Solution:** Manually assign Ground & Crop Tilemaps in FarmingManager inspector, or ensure they're named correctly.

### Issue: Clicking doesn't hoe/plant

- **Check 1:** Is FarmingInputHandler assigned to the Player?
- **Check 2:** Is the clicked tile actually a grass/soil tile? (Not off-tilemap)
- **Check 3:** Is selected hotbar slot empty? Select slot 1 or press **1** key.

### Issue: Crops don't grow

- **Check 1:** Is more than one in-game day passing? Day length = 60 real seconds by default.
- **Check 2:** Is crop requiresWatering=true and it's not watered? Water it first.
- **Check 3:** Check console for error logs: `[FarmingManager]` logs.

### Issue: Harvest doesn't add items

- **Check 1:** Is crop fully mature? Stage should equal TotalStages-1.
- **Check 2:** Is inventory full? Should stack or use empty slot.
- **Check 3:** Harvest item reference set? Check CropDefinition harvestItem.

### Issue: Save file not found after loading

- **Solution:** File is in `Application.persistentDataPath`, not project folder. On Windows typically: `C:\Users\<username>\AppData\LocalLow\<CompanyName>\<ProductName>\farming_save.json`

---

## EXTENDING THE SYSTEM

### Add More Crops

1. Create new CropDefinition (Right-click > Create > Farming/Crop Definition)
2. Set unique cropId, stageTiles, seed/harvest items
3. Add to FarmingManager's "Available Crops" array

### Enable Watering Requirement

1. Open Tomato CropDefinition
2. Set **requiresWatering** = true
3. Players MUST water daily or crop won't grow

### Change Day Length

1. Select DayNightCycleNice2D in scene
2. Adjust **Day Length Seconds** (60 = fast, 300 = realistic)

### Modify Tile References

1. If your grass/soil tile names differ, update FarmingManager inspector references
2. Or in FarmingInputHandler, adjust `hoeItemName` pattern matching

---

## TEST CHECKLIST

- [ ] **Hoe works:** Select Hoe → Click grass → becomes soil
- [ ] **Plant works:** Select Tomato Seed → Click soil → crop appears, seed gone from inventory
- [ ] **Seed consumed:** Inventory count decreases by 1
- [ ] **Day advances:** Wait ~60 seconds → crop changes to next stage
- [ ] **Harvest works:** Wait until mature → Select Hand → Click crop → crop disappears, 3x Tomato added to inventory
- [ ] **Multiple crops:** Plant 3 crops → all grow independently
- [ ] **Watering works** (if enabled): Select Watering Can → Click crop → logs "Watered crop"
- [ ] **Save works:** Plant crop → Right-click FarmingDataSaveSystem > Save Farming Data → check console
- [ ] **Load works:** Delete world → Right-click FarmingDataSaveSystem > Load Farming Data → soil & crops restored
- [ ] **UI doesn't break:** Click doesn't trigger farming when pointer over inventory UI

---

## DEBUG CONTEXT MENUS

Select **FarmingManager** in Inspector, right-click component script name:

- **Log Farming State** - Prints all hoed cells & crops to console
- **Clear All Hoed Soil** - Removes all soil tiles
- **Clear All Crops** - Removes all crops from world

---

## NEXT STEPS (OPTIONAL)

1. **Add crop animations:** Emit particles when harvesting
2. **Crop failure:** Add pests/disease with RNG
3. **Seasonal crops:** Add season field to CropDefinition
4. **Tool durability:** Track Hoe uses, degrades
5. **Soil nutrients:** Track N/P/K levels, deplete each harvest
6. **Advanced water:** Show wet soil visually, drain over days

---

**Questions? Check the script comments in FarmingManager.cs, CropDefinition.cs, and FarmingInputHandler.cs!**
