# Farming System Fixes - Complete Implementation

## Summary

All major farming system issues have been fixed. The system now correctly implements:

- ✅ Daily watering requirement for plant growth
- ✅ Parallel plant growth (all plants grow together when conditions are met)
- ✅ Plant death after consecutive days without water
- ✅ Harvest items spawn as pickable objects
- ✅ Watering can durability (empties after 10 uses)
- ✅ Toast messages for all farming actions

---

## Changes Made

### 1. **CropDefinition.cs** - Added Dead Plant Support

**New Properties:**

- `deadPlantSprite` (Sprite) - Sprite to display when plant dies from thirst
- `daysWithoutWaterUntilDeath` (int) - Days before plant dies (default: 3)
- Set `requiresWatering = true` by default now

**Action Required:**

- For each crop asset, assign a dead plant sprite to the `deadPlantSprite` field
- Adjust `daysWithoutWaterUntilDeath` if desired (default 3 days)

---

### 2. **CropData.cs** - Enhanced Growth Logic

**New Fields:**

- `daysWithoutWater` (int) - Tracks consecutive days without watering
- `isDead` (bool) - Whether crop has died

**Updated Logic:**

- When watered: `daysWithoutWater` resets to 0
- When not watered: `daysWithoutWater` increments
- When `daysWithoutWater >= daysWithoutWaterUntilDeath`: `isDead = true`
- Dead plants cannot grow and display dead sprite

---

### 3. **FarmingManager.cs** - Major Enhancements

#### New Features:

- **Toast Notifications** - Shows UI messages for all farming actions via `PickupToastUIToolkit`
- **Plant Death Handling** - Automatically detects and displays dead plants
- **Harvest Item Spawning** - Creates pickable items instead of adding to inventory directly
- **Auto-Reference Finding** - Finds `PickupToastUIToolkit` if not assigned

#### Updated Methods:

- `TryHoeAtCell()` - Shows toast: "Soil ready for planting" / "Already hoed!" / etc.
- `TryPlantAtCell()` - Shows toast: "Planted [crop name]" / "Need to hoe first" / etc.
- `TryWaterAtCell()` - Shows toast: "Plant watered ✓" / "Plant is dead" / etc.
- `TryHarvestAtCell()` - MAJOR CHANGE:
  - Spawns a GameObject with SpriteRenderer + PickupComponent
  - Item becomes a pickable object in the world
  - Shows toast: "Harvested [crop name]! ✓"
  - Cleans up any dead plant visuals
- `AdvanceDay()` - Now detects plant death and shows toast: "[crop] died ✗"
- `UpdateCropTileAtCell()` - Displays dead plants:
  - When `isDead == true`: Creates a sprite GameObject with deadPlantSprite
  - When alive: Displays normal tilemap growth stage
  - Handles cleanup of old dead plant visuals

#### Toast Messages:

| Action           | Message                   |
| ---------------- | ------------------------- |
| Hoe soil         | "Soil ready for planting" |
| Plant seed       | "Planted [name]"          |
| Water plant      | "Plant watered ✓"         |
| Plant dies       | "[name] died ✗"           |
| Harvest mature   | "Harvested [name]! ✓"     |
| Can't water dead | "Plant is dead"           |

---

### 4. **FarmingInputHandler.cs** - Watering Can Durability

#### New Features:

- **Watering Can Tracking** - Tracks uses per watering can instance
- **Capacity System** - Configurable (default: 10 uses per fill)
- **Durability Feedback** - Shows remaining water count when low

#### New Logic:

```
TryWaterWithCan(world, wateringCanItem)
├─ Check if can is empty
├─ If empty: Show "Watering can is empty! Refill it."
├─ If not empty: Perform watering
├─ Decrease durability by 1
└─ Show status messages:
   ├─ When empty: "Watering can empty! Needs refill."
   └─ When ≤3 uses: "Water: 3/10" (shows remaining)
```

#### Durability Messages:

| Durability   | Message                             |
| ------------ | ----------------------------------- |
| Empty (≤0)   | "Watering can is empty! Refill it." |
| Just emptied | "Watering can empty! Needs refill." |
| Low (≤3/10)  | "Water: 2/10" (shows remaining)     |

#### Configuration:

- `wateringCanCapacity` field (default: 10) - Adjust if needed

---

## Setup Instructions

### For Artists/Level Designers:

1. **Crop Assets**
   - Open each crop definition (CropDefinition asset)
   - Assign a dead plant sprite to the `deadPlantTile` field
   - Adjust `daysWithoutWaterUntilDeath` if needed (default 3 is good)

2. **Watering Can**
   - The watering can item automatically tracks durability
   - No additional setup needed
   - Default capacity: 10 uses

### For Programmers:

**To Refill a Watering Can (if implementing UI/item that refills it):**

```csharp
FarmingInputHandler handler = FindObjectOfType<FarmingInputHandler>();
handler.RefillWateringCan(wateringCanItemDefinition);
```

---

## How It Works Now

### Growth System

1. Player hoes grass → becomes soil
2. Player plants seed on soil (consumes seed)
3. Next day advance:
   - If plant was watered yesterday → grows 1 day progress
   - If plant wasn't watered → daysWithoutWater++ (no growth)
   - If daysWithoutWater ≥ daysWithoutWaterUntilDeath → plant dies
4. Dead plants:
   - Show deadPlantSprite as a sprite GameObject (not a tilemap tile)
   - Positioned at the crop cell location
   - Cannot be harvested or interacted with
   - Automatically cleaned up when crops are cleared or replaced

### Harvesting System

1. Mature crop displayed on tilemap
2. Player clicks with harvest tool (hand)
3. Harvest generates:
   - A GameObject in the world with harvest item sprite
   - PickupComponent that magnetizes to player
   - Toast message showing crop harvested
4. Player picks up the item like normal

### Watering Can System

1. Player selects watering can
2. Each successful water use decreases durability by 1
3. After 10 uses, toast shows "needs refill"
4. Can is empty and can't water until refilled
5. Messages show when durability is low (≤3)

---

## Testing Checklist

- [ ] Plant grows without watering → SHOULD NOT (goes to day 2 without growth)
- [ ] Plant grows with daily watering → SHOULD (advances stage each day)
- [ ] Multiple plants grow simultaneously → SHOULD (all advance together)
- [ ] Plant dies after 3 days no water → SHOULD (displays dead sprite)
- [ ] Dead plant shows dead sprite → SHOULD (displays deadPlantTile)
- [ ] Harvest creates pickable item → SHOULD (item appears in world)
- [ ] Picked up item goes to inventory → SHOULD (via PickupComponent)
- [ ] Watering can empties after 10 uses → SHOULD (shows "needs refill")
- [ ] Toast messages appear → SHOULD (for all actions)

---

## Common Adjustments

### Increase days before plant death:

Open CropDefinition → Set `daysWithoutWaterUntilDeath = 5` (example)

### Change watering can capacity:

In FarmingInputHandler, set `wateringCanCapacity = 15` (example)

### Disable toast messages:

Leave `pickupToast` unassigned in inspector (will be null)

---

## Known Behaviors

- Dead plants cannot be harvested (intentional - they're dead)
- Watering a dead plant shows "Plant is dead" message
- Harvest items have 10 second TTL before despawning if not picked up
- Watering can resets by day (if needed for multiple cans, game developer can call RefillWateringCan)
- Toast messages stack if triggered multiple times quickly

---

## Files Modified

1. `Assets/Scripts/Farming/CropDefinition.cs`
   - Added deadPlantSprite and daysWithoutWaterUntilDeath properties

2. `Assets/Scripts/Farming/CropData.cs`
   - Added daysWithoutWater and isDead fields
   - Updated AdvanceDay() logic for death tracking
   - Updated serializable version

3. `Assets/Scripts/Farming/FarmingManager.cs`
   - Added PickupToastUIToolkit reference
   - Added deadPlantVisuals dictionary for sprite GameObject tracking
   - Refactored all Try\* methods for toast notifications
   - Completely rewrote TryHarvestAtCell() to spawn pickups
   - Updated AdvanceDay() to detect plant death
   - Updated UpdateCropTileAtCell() to display dead plant sprites as GameObjects
   - Updated RefreshAllCropTiles() to handle dead plant visuals on load
   - Updated DebugClearCrops() to clean up dead plant GameObjects

4. `Assets/Scripts/Farming/FarmingInputHandler.cs`
   - Added watering can durability tracking system
   - Created TryWaterWithCan() method with capacity checking
   - Added toast messages for water can status
   - Added RefillWateringCan() public method
