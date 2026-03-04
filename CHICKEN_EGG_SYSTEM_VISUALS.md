# Chicken Egg System - Visual Setup & Data Flow

## System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    CHICKEN EGG SYSTEM                       │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  DayNightCycleNice2D                                        │
│  (Provides game time & broadcasts OnDayAdvanced)            │
│           │                                                  │
│           ├──────────► ChickenController (per chicken)       │
│           │                    │                            │
│           │                    ├─► Check current time        │
│           │                    ├─► Lay egg at specified time │
│           │                    └─► Spawn Egg Prefab          │
│           │                              │                  │
│           │                              ▼                  │
│           │                    Egg (GameObject)              │
│           │                    ├─ SpriteRenderer             │
│           │                    ├─ BoxCollider2D             │
│           │                    ├─ PickupComponent ◄──┐      │
│           │                    └─ EggItem               │   │
│           │                              │              │   │
│           │                              ▼              │   │
│           │                    [Egg moves to player]    │   │
│           │                              │              │   │
│           │                              ▼              │   │
│           │                    InventoryController      │   │
│           │                    .TryAdd(egg, 1)          │   │
│           │                                             │   │
│           └────────► OnDayAdvanced event ───────────────┘   │
│                    (Resets hasLaidEggToday)                 │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

## Data Flow: Egg Laying to Inventory

```
START OF DAY
    │
    ▼
DayNightCycle broadcasts OnDayAdvanced
    │
    ▼
ChickenController.OnNewDay()
    ├─ hasLaidEggToday = false
    └─ Reset ready for new day

DURING DAY (each frame)
    │
    ├─► Check game time
    │   (currentHour = TimeNormalized * 24)
    │
    ├─► Is time in laying window?
    │   (e.g., 8.0 - 9.0 AM)
    │
    ├─ YES ► TryLayEgg()
    │   │
    │   ├─ Instantiate egg prefab
    │   │   ├─ Position: chicken + random offset
    │   │   ├─ With PickupComponent ready
    │   │   └─ With EggItem metadata
    │   │
    │   └─ hasLaidEggToday = true
    │
    └─ NO ► Continue checking

EGG LIFECYCLE
    │
    ├─► Egg spawned near chicken
    │   │
    │   ├─ Player moves nearby
    │   │
    │   ├─► PickupComponent detects range
    │   │   └─ distance < pickupDistance (1.5)
    │   │
    │   ├─► Egg moves toward player
    │   │   └─ Vector3.MoveTowards()
    │   │
    │   ├─► Player gets close
    │   │   └─ distance < collectDistance (0.1)
    │   │
    │   ├─► InventoryController.TryAdd()
    │   │   ├─ Find existing egg slot
    │   │   ├─ Add to stack OR
    │   │   └─ Create new slot
    │   │
    │   └─► Egg destroyed
    │       └─ Destroy(gameObject)
    │
    └─► TTL expires (30 sec)
        └─ Egg disappears if not picked up

RESULT
    │
    └─► Inventory updated
        Player now has eggs!
```

## Component Connections

```
CHICKEN GAMEOBJECT
│
├─ Transform
│  └─ Position: Farm location
│
├─ Sprite Renderer
│  └─ Displays chicken sprite
│
├─ ChickenController ◄─────────┐
│  ├─ Egg Item: Egg asset       │ YOU ASSIGN
│  ├─ Egg Prefab Path: Egg.prefab
│  ├─ Day Night Cycle: Scene object
│  └─ Egg Laying Time: 8 (hours)
│
└─ (Optional BoxCollider2D)
   └─ For visual/interaction bounds


EGG PREFAB (at Resources/Prefabs/Items/Egg.prefab)
│
├─ Transform
│  └─ Position: Set by ChickenController
│
├─ Sprite Renderer
│  ├─ Sprite: Egg sprite          │
│  └─ Sorting Order: 0             │ YOU SET UP
│                                  │
├─ BoxCollider2D                  │
│  ├─ Is Trigger: ✓               │
│  └─ Size: 0.3 x 0.3             │
│
├─ PickupComponent (attached)
│  ├─ Item: null (set by Controller)
│  ├─ Count: 0 (set by Controller)
│  ├─ Speed: 5                    │ YOU CONFIGURE
│  ├─ Pickup Distance: 1.5         │ (But defaults work)
│  ├─ Collect Distance: 0.1        │
│  └─ TTL: 30                      │
│
└─ EggItem (optional metadata)
   ├─ Item Name: "Egg"
   ├─ Description: "Fresh chicken egg..."
   ├─ Icon: Egg sprite
   └─ Sell Price: 75
```

## Time Calculation

```
DayNightCycleNice2D.TimeNormalized
│
├─ Range: 0.0 to 1.0
│  ├─ 0.0 = Midnight (00:00)
│  ├─ 0.25 = Morning (6:00)
│  ├─ 0.5 = Noon (12:00)
│  ├─ 0.75 = Evening (18:00)
│  └─ 1.0 = Next midnight
│
CONVERSION TO 24-HOUR:
│
currentHour = TimeNormalized * 24
│
├─ TimeNormalized = 0.333... ► currentHour = 8.0 (8 AM) ✓
├─ TimeNormalized = 0.583... ► currentHour = 14.0 (2 PM)
└─ TimeNormalized = 0.75 ► currentHour = 18.0 (6 PM)

LAYING WINDOW CHECK:
│
if (currentHour >= layingTime && currentHour < layingTime + window)
    TryLayEgg()

Example: layingTime=8, window=1
├─ Window = 8.0 to 9.0
├─ At 8:15 AM ► currentHour = 8.25 ✓ Lay egg!
├─ At 8:45 AM ► currentHour = 8.75 ✓ Already laid today
└─ At 10:00 AM ► currentHour = 10.0 ✗ Outside window
```

## Inventory Integration

```
InventoryController.TryAdd(ItemDefinition item, int amount)
│
├─ INPUT:
│  ├─ item = Egg (ItemDefinition)
│  └─ amount = 1
│
├─ PROCESS:
│  │
│  ├─ PHASE 1: Stack into existing slots
│  │  └─ For each slot with egg < maxStack
│  │     └─ Add to stack
│  │
│  └─ PHASE 2: Fill empty slots
│     └─ For each empty slot
│        └─ Create new stack
│
├─ OUTPUT: bool
│  ├─ true = All 1 eggs added ✓
│  └─ false = Inventory full ✗
│
└─ UI UPDATE:
   └─ RefreshSlot(index) updates UI display
```

## Setup Order (What to do first)

```
STEP 1: Register Egg Item
┌─────────────────────────────────────┐
│ Create Egg ItemDefinition           │
│ Asset > Create > Inventory > Item   │
│ Assets/Items/Egg.asset              │
└─────────────────────────────────────┘
              │
              ▼
STEP 2: Create Egg Prefab
┌─────────────────────────────────────┐
│ New GameObject with components:     │
│ • SpriteRenderer                    │
│ • BoxCollider2D (Trigger)           │
│ • PickupComponent                   │
│ • EggItem (optional)                │
│                                     │
│ Save: Resources/Prefabs/Items/      │
│ Filename: Egg.prefab                │
└─────────────────────────────────────┘
              │
              ▼
STEP 3: Add ChickenController to Chicken
┌─────────────────────────────────────┐
│ Select Chicken GameObject           │
│ Add Component > ChickenController    │
│                                     │
│ Assign in Inspector:                │
│ • Egg Item: [Egg asset]             │
│ • Egg Prefab Path: [Egg prefab]     │
│ • Day Night Cycle: [Scene object]   │
│ • Egg Laying Time: 8                │
└─────────────────────────────────────┘
              │
              ▼
STEP 4: Test
┌─────────────────────────────────────┐
│ Play scene                          │
│ Fast-forward to 8 AM                │
│ Verify egg spawns near chicken      │
│ Walk to egg, it auto-pickups        │
│ Check inventory                     │
└─────────────────────────────────────┘
```

## Multiple Chickens Setup

```
CHICKEN 1 (Morning Layers)
├─ ChickenController
│  ├─ Egg Laying Time: 8
│  ├─ Egg Laying Time Window: 1
│  └─ (Lays 8-9 AM)
└─ Spawns 1 egg per day at 8 AM

CHICKEN 2 (Afternoon Layers)
├─ ChickenController
│  ├─ Egg Laying Time: 14
│  ├─ Egg Laying Time Window: 1
│  └─ (Lays 2-3 PM)
└─ Spawns 1 egg per day at 2 PM

CHICKEN 3 (Dual Eggs - Two Controllers)
├─ ChickenController #1
│  ├─ Egg Laying Time: 8
│  └─ (Lays 8 AM)
├─ ChickenController #2
│  ├─ Egg Laying Time: 17
│  └─ (Lays 5 PM)
└─ Spawns 2 eggs per day
```

## State Transitions

```
                        START
                          │
                          ▼
                  hasLaidEggToday: false
                    timeWindow: wait
                          │
          ┌───────────────┼───────────────┐
          │               │               │
          ▼               ▼               ▼
      NOT TIME        IN WINDOW       AFTER WINDOW
    Continue wait     Try lay egg     Already laid
          │               │               │
          │               ├─► hasLaidEggToday: true
          │               ├─► Spawn egg prefab
          │               └─► Egg in world
          │
          └─────────────────────────────┐
                                        │
                                        ▼
                                  PickupComponent
                                  activates when
                                  player near
                                        │
                                        ▼
                                  Egg added to
                                  inventory
                                        │
                                        ▼
                                   Egg destroyed
                                        │
          ┌─────────────────────────────┘
          │
    NEXT DAY
          │
          ▼
    OnDayAdvanced()
    Reset: hasLaidEggToday = false
          │
          └──► Back to NOT TIME state
```

---

This visual guide shows exactly how all the pieces connect together!
