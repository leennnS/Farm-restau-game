# Intro Scene & Spawn System Setup Guide

## Overview

This guide walks you through setting up:

1. The **Intro Scene** with narrative typewriter effect
2. The **SpawnManager** system that positions the player correctly when transitioning from Intro to FarmScene

---

## PART 1: INTRO SCENE SETUP

### Step 1: Prepare Your Intro Scene in Unity Editor

1. Open the **Intro.unity** scene in your project
2. Set the **Main Camera** background to **solid black**
   - Select Main Camera in the Hierarchy
   - In Inspector → Camera component → Background Type: Solid Color
   - Color: Black (0, 0, 0, 1)

### Step 2: Add the Canvas (if not already present)

1. Right-click in Hierarchy → **2D Object → Canvas**
2. Name it: `NarrativeCanvas`
3. Set Canvas properties:
   - **Canvas Scaler → Reference Resolution:** 1920, 1080
   - **Render Mode:** Screen Space - Overlay

### Step 3: Add TextMeshPro Text to Canvas

1. Right-click on **NarrativeCanvas** in Hierarchy
2. Select **TextMeshPro - Text**
3. Name it: `NarrativeText`
4. Configure Text properties:
   - **Rect Transform:** Anchor to center, set width ~1024, height ~256
   - **TextMeshPro Text component:**
     - Font Size: 36
     - Alignment: Center, Middle
     - Color: White (1, 1, 1, 1)
   - **Layout Element (optional):** Set Preferred Width/Height to match Rect Transform

### Step 4: Add NarrativeManager Script to Canvas

1. Select **NarrativeCanvas** (or create empty GameObject at root level)
2. Add Component: **NarrativeManager**
3. In Inspector, configure:
   - **Narrative Text:** Drag the `NarrativeText` object into this field
   - **Narrative Sequence:** Edit the text array with your story
     - Default has 4 sentences, you can add/remove as needed
   - **Typewriter Speed:** Keep at 0.05 (adjust for faster/slower typing)
   - **Scene To Load After:** `FarmScene`

### Step 5: Test Intro Scene

1. In Build Settings (File → Build Settings):
   - Make sure **Intro** appears in Scenes
   - Move Intro to index 0 if starting new game from Intro
   - Keep FarmScene in the list
2. Play the scene
   - Text should type out character by character
   - Press Space or click mouse to skip/advance
   - Scene should transition to FarmScene when complete

---

## PART 2: FARMSCENE SPAWN SETUP

### Step 1: Create Spawn Point GameObjects in FarmScene

1. Open **FarmScene.unity**
2. Create two empty GameObjects as spawn points:

**Option A: For Intro Entry (Shed Door)**

- Create empty GameObject
- Name: `ShedDoorSpawnPoint`
- Position it in front of your Shed Door (where you want player to appear after intro)
- Example position: (10, 2, 0)

**Option B: Default Spawn Point**

- Create another empty GameObject
- Name: `DefaultSpawnPoint`
- Position it at your default spawn location (where player normally appears)
- Example position: (0, 0, 0)

### Step 2: Add SpawnManager to FarmScene

1. Create a new empty GameObject at scene root
   - Name: `SpawnManager`
2. Add Component: **SpawnManager**
3. In Inspector, assign:
   - **Shed Door Spawn Point:** Drag `ShedDoorSpawnPoint` into this field
   - **Default Spawn Point:** Drag `DefaultSpawnPoint` into this field
   - **Shed Door Spawn Point Name:** `ShedDoorSpawnPoint` (must match GameObject name)
   - **Default Spawn Point Name:** `DefaultSpawnPoint` (must match GameObject name)

### Step 3: Ensure Player Has CharacterController2D

1. Find your Player GameObject in FarmScene Hierarchy
2. Verify it has the **CharacterController2D** component
3. SpawnManager will automatically find and position this player

### Step 4: Test the Spawn System

1. In Build Settings:
   - Add both Intro and FarmScene
   - Scenes In Build: [0] Intro, [1] FarmScene
2. Play from Intro Scene:
   - Complete the narrative (Press Space/Click through)
   - Should transition to FarmScene
   - Player should appear in front of Shed Door
3. Play directly from FarmScene:
   - Player should appear at Default Spawn Point
   - (FromIntroScene flag is only set when coming from Intro)

---

## PART 3: GAMEMANAGER INTEGRATION (Optional)

If using your existing **NewGame()** system:

### Modify NewGame to Load Intro Instead

In **GameManager.cs**, change the `NewGame()` method:

```csharp
public void NewGame()
{
    Debug.Log("[GameManager] Starting new game - clearing player data");

    ClearAllGameData();

    if (MoneyManager.HasInstance)
    {
        MoneyManager.Instance.ResetToDefault();
    }

    if (InventoryController.HasInstance)
    {
        InventoryController.Instance.ClearAllItems();
    }

    if (DayNightCycleNice2D.Instance != null)
    {
        DayNightCycleNice2D.Instance.ResetToDefault();
    }

    PlayerPrefs.SetInt(GameStateKey, 1);
    PlayerPrefs.Save();

    // Load Intro scene instead of FarmScene directly
    SceneManager.LoadScene("Intro");
}
```

---

## PART 4: BUILD SETTINGS CHECKLIST

☐ File → Build Settings
☐ Drag scenes into build in this order:

1. **Intro.unity** (Index 0)
2. **FarmScene.unity** (Index 1)
3. Add any other scenes you use
   ☐ Click Build

---

## TROUBLESHOOTING

### Problem: Text not appearing

- **Solution:** Make sure TextMeshPro default asset is imported (usually auto-imports on first use)
- Check Canvas → Graphic Raycaster is present

### Problem: Player not spawning at correct position

- **Solution:** Verify spawn point GameObjects exist and are named correctly
- Check Console for "[SpawnManager]" debug messages
- Ensure CharacterController2D exists on Player

### Problem: Scene doesn't transition

- **Solution:** Check Build Settings - both Intro and FarmScene must be added
- Ensure scene name in NarrativeManager matches exactly: `FarmScene`

### Problem: Input not working

- **Solution:** Verify EventSystem in Canvas (should auto-create or already exist)
- Try clicking on game window to ensure it has focus
- Check Input Manager (Edit → Project Settings → Input Manager)

---

## CUSTOMIZATION

### Change Narrative Text

Edit **NarrativeManager** in Inspector:

- **Narrative Sequence** array
- Add or remove strings as needed
- Typewriter Speed: Lower = slower typing

### Change Spawn Locations

Move the spawn point GameObjects in the Scene:

- `ShedDoorSpawnPoint`: Move to new location for intro entry
- `DefaultSpawnPoint`: Move to new default location

### Change Next Scene

In **NarrativeManager** Inspector:

- **Scene To Load After:** Change from `FarmScene` to your target scene

---

## QUICK REFERENCE

| Component          | Location       | Key Settings                                                    |
| ------------------ | -------------- | --------------------------------------------------------------- |
| NarrativeManager   | Intro scene    | Narrative Text (ref), Narrative Sequence (array), Scene To Load |
| SpawnManager       | FarmScene root | Shed Door Spawn Point (ref), Default Spawn Point (ref)          |
| ShedDoorSpawnPoint | FarmScene      | Position where player appears after Intro                       |
| DefaultSpawnPoint  | FarmScene      | Default player spawn when playing direct from FarmScene         |

---

## FILE LOCATIONS

- **NarrativeManager.cs:** `Assets/Scripts/Systems/NarrativeManager.cs`
- **SpawnManager.cs:** `Assets/Scripts/Systems/SpawnManager.cs`

Both scripts are ready to use. No modifications needed.
