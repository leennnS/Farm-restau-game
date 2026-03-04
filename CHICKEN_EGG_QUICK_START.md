# Chicken Egg System - Quick Setup Checklist

## What Was Created For You ✅

### Scripts (Ready to Use)

- ✅ **ChickenController.cs** - Manages egg laying timing and spawning
- ✅ **EggItem.cs** - Egg item metadata (optional but recommended)

## What You Need To Create In Unity ⚠️

### 1. Egg ItemDefinition Asset 📋

Create in Inspector: **Right-click > Create > Inventory > Item** → Name: `Egg`

```
Display Name: Egg
Icon: [Your egg sprite]
Max Stack: 99
```

Save in: `Assets/Items/Egg.asset`

### 2. Egg Prefab 📦

Create GameObject with these components:

```
Egg (GameObject)
├── Sprite Renderer (with egg sprite)
├── BoxCollider2D (Is Trigger ✓, Size: 0.3x0.3)
├── PickupComponent (from existing script)
│   ├── Speed: 5
│   ├── Pickup Distance: 1.5
│   ├── Collect Distance: 0.1
│   └── TTL: 30
└── EggItem (optional metadata)
```

Save as prefab at: `Assets/Resources/Prefabs/Items/Egg.prefab`

### 3. Configure Chickens 🐔

For EACH chicken in your scene:

1. Add **ChickenController** component
2. Set these fields:
   ```
   • Egg Item: [Drag Egg asset]
   • Egg Count: 1
   • Egg Laying Time: 8 (8 AM) - adjust as needed
   • Egg Laying Time Window: 1
   • Egg Prefab Path: [Drag Egg prefab]
   • Day Night Cycle: [Drag DayNightCycleNice2D]
   • Spawn Offset: (0.5, 0, 0) - spawns beside chicken
   • Spawn Random Radius: 0.2 - small radius
   • Egg Time To Live: 120 - eggs stay 2 minutes before disappearing
   ```

## Result 🎯

| Action            | Result                  |
| ----------------- | ----------------------- |
| Day starts        | Hen ready to lay        |
| 8:00 AM arrives   | Egg spawns near chicken |
| Player approaches | Egg moves toward player |
| Player gets close | Egg added to inventory  |
| New day           | Process repeats         |

## File System Structure

```
✅ CREATED BY ME:
Assets/Scripts/NPCs/ChickenController.cs
Assets/Scripts/Items/EggItem.cs
CHICKEN_EGG_SYSTEM_SETUP.md (this guide)

⚠️ YOU CREATE IN UNITY:
Assets/Items/Egg.asset (ScriptableObject)
Assets/Resources/Prefabs/Items/Egg.prefab (Prefab)

✅ ALREADY EXIST (Reused):
Assets/Scripts/Items/PickupComponent.cs
Assets/Scripts/Inventory/InventoryController.cs
Assets/Scripts/Systems/DayNightCycleNice2D.cs
```

## Key Features

✅ **Automatic egg laying** - No player interaction needed  
✅ **Time-based** - Configurable hour of day  
✅ **Auto pickup** - Like carrots and other items  
✅ **Inventory integration** - Uses existing system  
✅ **Daily reset** - One egg per day  
✅ **Spatial spawning** - Random offset from chicken

## Time Configuration Examples

```
Morning Hen:         Afternoon Hen:      Evening Hen:
Time: 8              Time: 14            Time: 18
Window: 1            Window: 1           Window: 2
(Lays 8-9 AM)        (Lays 2-3 PM)       (Lays 6-8 PM)
```

## Troubleshooting Quick Fix

| Problem                  | Fix                                                     |
| ------------------------ | ------------------------------------------------------- |
| Eggs not spawning        | Check prefab path, verify DayNightCycle assigned        |
| Eggs not being picked up | Ensure PickupComponent on prefab, check inventory space |
| Laying multiple times    | Verify OnDayAdvanced event is broadcasting              |
| Wrong spawn location     | Adjust Spawn Offset values                              |

---

**Full setup guide**: See CHICKEN_EGG_SYSTEM_SETUP.md for detailed instructions
