# Chicken Egg System Setup Guide

## Overview

This system allows chickens to automatically lay eggs once per day. The eggs are items that get picked up automatically when the player comes nearby.

## Files Created

### 1. **ChickenController.cs**

- Location: `Assets/Scripts/NPCs/ChickenController.cs`
- Manages automatic egg laying based on game time
- Spawns egg prefabs at configurable times

### 2. **EggItem.cs**

- Location: `Assets/Scripts/Items/EggItem.cs`
- Metadata script for egg item (name, description, price)
- Can be extended for special behaviors

## Step-by-Step Unity Setup

### Step 1: Create the Egg ItemDefinition (ScriptableObject)

1. Right-click in `Assets/Items` folder
2. Select **Create > Inventory > Item**
3. Name it `Egg`
4. In the Inspector, set:
   - **Display Name**: "Egg"
   - **Icon**: Select an egg sprite from your art assets
   - **Max Stack**: 99 (or your preferred amount)
5. Save it

### Step 2: Create the Egg Prefab

1. Create a new empty GameObject in your scene called `Egg`
2. Add these components:
   - **Sprite Renderer** - assign your egg sprite
   - **BoxCollider2D** - for collision detection
     - Check "Is Trigger"
     - Size: ~(0.3, 0.3)
   - **PickupComponent** (from Assets/Scripts/Items/PickupComponent.cs)
     - Leave Item and Count at defaults (ChickenController will set these)
     - **Speed**: 5
     - **Pickup Distance**: 1.5
     - **Collect Distance**: 0.1
     - **TTL**: 30 (lifetime in seconds)
   - **EggItem** (optional, for extra metadata)

3. Drag this GameObject into `Assets/Resources/Prefabs/Items/` folder
   - **Create the folder structure if it doesn't exist**:
     - `Assets/Resources/`
     - `Assets/Resources/Prefabs/`
     - `Assets/Resources/Prefabs/Items/`
   - Save as prefab named `Egg`

4. Delete from scene (keep only the prefab)

### Step 3: Set Up Chicken GameObjects

For each chicken in your farm scene:

1. Select the chicken GameObject
2. Add the **ChickenController** script component
3. Configure the settings:

   **Egg Laying Settings:**
   - **Egg Item**: Drag the `Egg` ItemDefinition you created in Step 1
   - **Egg Count**: 1 (how many eggs per lay)
   - **Egg Laying Time**: 8 (hour of day to lay, in 24-hour format)
     - 8 = 8 AM
     - 14 = 2 PM
     - Set to match your game's day cycle
   - **Egg Laying Time Window**: 1 (hour window for laying)
     - Eggs will lay between 8:00-9:00 AM if set to 8 and 1

   **Egg Prefab:**
   - **Egg Prefab Path**: Drag the `Egg` prefab you created in Step 2
   - (Or leave empty to auto-load from Resources/Prefabs/Items/Egg)

   **References:**
   - **Day Night Cycle**: Drag your `DayNightCycleNice2D` GameObject
   - (Or leave empty to auto-find)

   **Spawn Settings:**
   - **Spawn Offset**: (0.5, 0, 0) (default: egg spawns beside chicken for easy visibility)
   - **Spawn Random Radius**: 0.2 (small radius around spawn position)
   - **Egg Time To Live**: 120 (default: 120 seconds = 2 minutes before disappearing)
   - **Physics**: Automatically disabled on spawn (kinematic, no gravity, no push-back)

### Step 4: Verify Your FarmingManager (if using)

The ChickenController integrates with your existing `DayNightCycleNice2D`:

- Listens for `OnDayAdvanced` event to reset egg-laying each day
- Uses `TimeNormalized` to calculate current hour
- No additional setup needed if DayNightCycleNice2D already broadcasts the event

## How It Works

1. **Daily Reset**: When a new day starts (via `OnDayAdvanced` event), `hasLaidEggToday` is reset
2. **Time Check**: Every frame, ChickenController checks if current game time is within the egg-laying window
3. **Spawn Egg**: When the window is reached, an egg prefab is instantiated
4. **Auto Pickup**: Player moving near the egg triggers PickupComponent, which:
   - Moves the egg toward the player
   - Adds it to inventory when close enough
   - Destroys the egg GameObject

## Configuration Examples

### Morning Chicken (8 AM)

- Egg Laying Time: **8**
- Egg Laying Time Window: **1**
- Lays between 8:00-9:00 AM

### Afternoon Chicken (2 PM)

- Egg Laying Time: **14**
- Egg Laying Time Window: **1**
- Lays between 2:00-3:00 PM

### All-day Chicken (multiple eggs)

- Create 2 chickens with different times
- Chicken 1: Time=8, Window=1
- Chicken 2: Time=16, Window=1

## Customization Options

### Change egg item properties:

- Edit the `Egg` ScriptableObject to adjust max stack size
- Adjust sell price in `EggItem.cs`

### Change spawn behavior:

- Increase `Spawn Random Radius` for wider scatter
- Adjust `Spawn Offset` to lay eggs slightly away from chicken

### Change pickup behavior:

- Modify PickupComponent's `pickupDistance` on the prefab
- Adjust `collectDistance` for how close player must be

### Multiple eggs per day:

- Create multiple ChickenController instances on same chicken
- Set different laying times
- Or create separate chicken prefabs

## Troubleshooting

**Eggs not spawning:**

- Check that DayNightCycleNice2D is assigned or findable
- Verify egg prefab path is correct (Resources/Prefabs/Items/Egg)
- Check Console for debug logs

**Eggs not being picked up:**

- Ensure PickupComponent is on egg prefab
- Verify player has "Player" tag
- Check that inventory isn't full
- Increase pickupDistance if too small

**Time not advancing:**

- Verify dayLengthSeconds isn't 0
- Check DayNightCycleNice2D is running

**Same egg laying multiple times:**

- ChickenController caches hasLaidEggToday, should only lay once per day
- If reset, check OnDayAdvanced event is being called

## Integration with Existing Systems

- **Inventory**: Uses existing `InventoryController.TryAdd()` method
- **Item Definitions**: Uses your existing `ItemDefinition` system
- **Pickup**: Reuses existing `PickupComponent` script
- **Day Cycle**: Integrates with `DayNightCycleNice2D` events
- **UI Feedback**: Optional integration with `PickupToastUIToolkit` (can be added)

## Optional: Add Pickup Toast Notification

To show "Picked up Egg!" notification when collected:

1. Open the egg prefab
2. Find the PickupComponent script
3. Modify to add:

```csharp
private PickupToastUIToolkit pickupToast;

private void Awake()
{
    // ... existing code ...
    pickupToast = FindFirstObjectByType<PickupToastUIToolkit>();
}

// In the pickup collection:
if (added)
{
    if (pickupToast != null)
        pickupToast.Show($"+{count} {item.displayName}");
    Destroy(gameObject);
}
```

## What The Player Experiences

1. ✅ Chicken exists in the farm
2. ✅ At specified time each day, egg appears near chicken
3. ✅ Player walks near egg
4. ✅ Egg automatically flies to player
5. ✅ Egg added to inventory automatically
6. ✅ Player can sell/use eggs
7. ✅ Next day, new egg appears (cycle repeats)

## File Locations Summary

```
Assets/
├── Scripts/
│   ├── NPCs/
│   │   └── ChickenController.cs ✅ (NEW)
│   └── Items/
│       └── EggItem.cs ✅ (NEW)
├── Items/
│   └── Egg.asset ✅ (CREATE via Inspector)
└── Resources/
    └── Prefabs/
        └── Items/
            └── Egg.prefab ✅ (CREATE via Editor)
```
