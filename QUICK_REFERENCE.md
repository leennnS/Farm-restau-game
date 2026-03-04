# CHICKEN EGG SYSTEM - QUICK REFERENCE CARD

## 3-STEP SETUP

### ① Create Egg ItemDefinition

```
Right-click Assets/Items → Create > Inventory > Item
Name: Egg
Display Name: Egg
Icon: [egg sprite]
Max Stack: 99
```

### ② Create Egg Prefab

```
GameObject "Egg"
  ├─ Sprite Renderer [egg sprite]
  ├─ BoxCollider2D [Is Trigger ✓, Size 0.3x0.3]
  ├─ PickupComponent [Speed: 5, Pickup: 1.5, Collect: 0.1, TTL: 30]
  └─ EggItem [optional]

Save: Assets/Resources/Prefabs/Items/Egg.prefab
```

### ③ Add To Chickens

```
Select Chicken → Add Component > ChickenController
Assign:
  • Egg Item: [Egg asset]
  • Egg Count: 1
  • Egg Laying Time: 8
  • Egg Laying Time Window: 1
  • Egg Prefab Path: [Egg prefab]
  • Day Night Cycle: [Scene object]
```

---

## TIME REFERENCE

```
Hour  │ Time     │ TimeNormalized
──────┼──────────┼────────────────
  0   │ Midnight │ 0.000
  6   │ 6 AM     │ 0.250
  8   │ 8 AM     │ 0.333 ← DEFAULT EGG TIME
 12   │ Noon     │ 0.500
 14   │ 2 PM     │ 0.583
 18   │ 6 PM     │ 0.750
 24   │ Midnight │ 1.000
```

---

## INSPECTOR FIELD REFERENCE

### ChickenController

```
Egg Laying Settings:
  ├─ Egg Item: ItemDefinition asset
  ├─ Egg Count: int (usually 1)
  ├─ Egg Laying Time: 0-24 (hour)
  └─ Egg Laying Time Window: 0-24 (hours)

Egg Prefab:
  └─ Egg Prefab Path: GameObject prefab

References:
  └─ Day Night Cycle: DayNightCycleNice2D

Spawn Settings:
  ├─ Spawn Offset: Vector3 (default: 0, -0.5, 0 - spawns below chicken)
  └─ Spawn Random Radius: float (default: 0.2)
```

### PickupComponent (on Egg Prefab)

```
Magnet Settings:
  ├─ Speed: 1-10 (movement speed)
  ├─ Pickup Distance: 0.5-3 (detection)
  └─ Collect Distance: 0.05-1 (collection)

Lifetime:
  └─ TTL: 10-60 (seconds)

Item:
  ├─ Item: ItemDefinition (set by controller)
  └─ Count: int (set by controller)
```

---

## COMMON VALUES

```
Morning Egg Laying:
  Time: 8
  Window: 1
  → Lays 8-9 AM

Afternoon Egg Laying:
  Time: 14
  Window: 1
  → Lays 2-3 PM

Evening Egg Laying:
  Time: 18
  Window: 2
  → Lays 6-8 PM

Multiple Eggs:
  Controller 1: Time 8
  Controller 2: Time 16
  → Lays at 8 AM & 4 PM
```

---

## FOLDER STRUCTURE

```
✅ CREATED:
  Assets/Scripts/NPCs/ChickenController.cs
  Assets/Scripts/Items/EggItem.cs

⚠️ YOU CREATE:
  Assets/Items/Egg.asset
  Assets/Resources/Prefabs/Items/Egg.prefab

✅ REUSED:
  Assets/Scripts/Items/PickupComponent.cs
  Assets/Scripts/Inventory/InventoryController.cs
  Assets/Scripts/Systems/DayNightCycleNice2D.cs
```

---

## TROUBLESHOOTING FLOWCHART

```
Eggs not spawning?
├─ Is DayNightCycle assigned? ✓
├─ Is egg prefab path correct? ✓
├─ Is ItemDefinition assigned? ✓
└─ Check Console for errors

Eggs not picking up?
├─ Does prefab have PickupComponent? ✓
├─ Does player have "Player" tag? ✓
├─ Is collider set as trigger? ✓
└─ Is inventory not full? ✓

Multiple eggs same day?
├─ Check for duplicate controllers
├─ Verify OnDayAdvanced fires
└─ Confirm hasLaidEggToday resets

Wrong position?
├─ Adjust Spawn Offset
└─ Adjust Spawn Random Radius
```

---

## WHAT EACH COMPONENT DOES

```
ChickenController
  → Checks game time
  → Triggers egg spawn
  → Resets daily

PickupComponent
  → Detects player distance
  → Moves to player
  → Adds to inventory

EggItem
  → Optional metadata
  → Name/description/price
  → Can be extended

DayNightCycleNice2D (existing)
  → Provides game time
  → Broadcasts day changes
  → Integrates automatically
```

---

## SCRIPT FILES REFERENCE

### ChickenController.cs Location

```
Assets/Scripts/NPCs/ChickenController.cs
Lines: 1-143
(Already created for you)
```

### EggItem.cs Location

```
Assets/Scripts/Items/EggItem.cs
Lines: 1-18
(Already created for you)
```

### PickupComponent.cs Location (existing, not modified)

```
Assets/Scripts/Items/PickupComponent.cs
(Reused from your existing system)
```

---

## TESTING CHECKLIST

```
Setup Complete?
  ☐ Egg ItemDefinition created
  ☐ Egg Prefab created
  ☐ ChickenController added

Inspector Fields?
  ☐ All fields assigned (no nulls)
  ☐ Time format correct (0-24)
  ☐ References correct type

Gameplay?
  ☐ Scene plays without errors
  ☐ Time advances
  ☐ Egg appears at right time
  ☐ Egg moves to player
  ☐ Egg picked up
  ☐ Inventory updated
  ☐ Can stack eggs
  ☐ New day = new egg
```

---

## FILE LOCATIONS FOR QUICK ACCESS

```
Game Scripts:
  ChickenController: Assets/Scripts/NPCs/ChickenController.cs
  EggItem: Assets/Scripts/Items/EggItem.cs
  PickupComponent: Assets/Scripts/Items/PickupComponent.cs (existing)

Assets to Create:
  Egg Asset: Assets/Items/Egg.asset
  Egg Prefab: Assets/Resources/Prefabs/Items/Egg.prefab

Documentation:
  Quick Start: CHICKEN_EGG_QUICK_START.md (root)
  Setup Guide: CHICKEN_EGG_SYSTEM_SETUP.md (root)
  Visuals: CHICKEN_EGG_SYSTEM_VISUALS.md (root)
  Troubleshooting: CHICKEN_EGG_TROUBLESHOOTING.md (root)
  Enhancements: CHICKEN_EGG_ENHANCEMENTS.md (root)
```

---

## QUICK COMMANDS

Print or memorize these for quick setup:

```
Time = Hour:
  0 = Midnight
  8 = 8 AM (default egg time)
  12 = Noon
  16 = 4 PM
  20 = 8 PM
  24 = Next Midnight

Prefab Path:
  Resources/Prefabs/Items/Egg

Folder to Create:
  Assets/Resources/Prefabs/Items/

Component to Add:
  ChickenController

Default Pickup Settings:
  Speed: 5
  Pickup Distance: 1.5
  Collect Distance: 0.1
  TTL: 30

Default Spawn Settings:
  Offset: (0.5, 0, 0) - spawns egg beside chicken for easy visibility
  Random Radius: 0.2 - small scatter radius
  Physics: Auto-disabled (kinematic, no gravity)
  Time To Live: 120 seconds - eggs stay for 2 minutes if not picked up
```

---

## REMEMBER

✅ Script is ready - no code changes needed
✅ Just configure in Inspector
✅ Setup takes ~20 minutes
✅ Fully integrated with existing systems
✅ Works like carrots/other pickups
✅ One egg per day per chicken (configurable)
✅ Full documentation provided

🎯 Goal: Eggs spawn daily, player picks them up automatically, inventory updates.

Good luck! 🐔🥚
