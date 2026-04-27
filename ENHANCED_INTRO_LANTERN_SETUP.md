# Enhanced Intro Scene Setup - Immersive Lantern Wakeup

## Overview

The new intro scene has:

- ✅ Player character visible (waking up in shed)
- ✅ Dark atmosphere (you're in a dark shed)
- ✅ Interactive lantern to pick up and turn on
- ✅ Lighting that changes when lantern is lit
- ✅ Persistent lantern that carries to FarmScene
- ✅ Narrative guides player through discovery

---

## PART 1: SCENE STRUCTURE

### Before You Start

Delete or disable the old **NarrativeManager** from your Intro scene. We're replacing it with **NarrativeManagerEnhanced**.

---

## PART 2: SETUP STEPS (IN UNITY)

### Step 1: Player Character Sprite

1. In **Intro.unity**, create a new GameObject
2. Name it: `Player`
3. Add Component: **SpriteRenderer**
4. Assign your character sprite (same as in FarmScene)
5. Position: Around center of screen, e.g., (0, -1, 0)
6. Add Component: **CharacterController2D** (so it can be found by scripts)
7. Add a 2D Collider (BoxCollider2D) for the script to find

### Step 2: Create Shed Background

1. Create a new GameObject
2. Name it: `ShedBackground`
3. Add Component: **SpriteRenderer**
4. Assign a dark shed/interior sprite (or solid dark color)
5. Set to back layer (Order in Layer: -10)
6. Scale to fill screen

### Step 3: Lantern GameObject

1. Create a new GameObject
   - Name: `Lantern`
   - Position: (2, -0.5, 0) ← On a table or shelf, visible but not on player
2. Add Component: **SpriteRenderer**
   - Assign lantern sprite (any lantern image)
   - Color: Dim gray to start (0.5, 0.5, 0.5, 1) ← Will brighten when lit
3. Add Component: **BoxCollider2D**
   - Check "Is Trigger"
4. Add Component: **Point Light 2D**
   - Intensity: 1.5
   - Range: 5-8
   - Color: Warm yellow/orange (1, 0.8, 0.4)
   - **IMPORTANT: Uncheck enabled** ← Light starts OFF
5. Add Component: **LanternController** script
   - Lantern Light: Assign the Point Light 2D from this GameObject
   - Lantern Sprite: Assign the SpriteRenderer from this GameObject
   - Pickup Key: E (or your choice)
   - Toggle Key: E (or your choice)
   - Pickup Distance: 2

### Step 4: Ambient Lighting (Dark Shed)

1. Go to **Window → Rendering → Lighting**
2. Set Global Light 2D to **very dim** (like 0.1 or 0.2 intensity)
   - Or drag a weak ambient light into the scene
3. This creates darkness so the lantern becomes vital

### Step 5: Update Canvas Narrative

1. Select **NarrativeCanvas**
2. **Delete the old component:** NarrativeManager
3. Add Component: **NarrativeManagerEnhanced**
4. Configure:
   - **Narrative Text:** Drag `NarrativeText` from Hierarchy
   - **Wake Up Narrative:** Default array is good (5 sentences about waking up)
   - **Lantern Lit Narrative:** Default array is good (4 sentences about finding light)
   - **Typewriter Speed:** 0.05
   - **Scene To Load After:** `FarmScene`
   - **Lantern Pickup Key:** E
5. Find the **Lantern** in Hierarchy and drag into the Lantern field if needed

### Step 6: Test Intro Scene

1. Play the scene
2. You should see:
   - Text typing out: "Your eyes flutter open..."
   - Player character visible in dark scene
   - Lantern visible on a shelf/table
   - DARK ambient lighting
3. Press Space/Click through narrative
4. When it says "Press E to pick up the lantern"
   - Press E near the lantern
   - Lantern light should turn ON
   - Scene brightens
5. New narrative plays about light
6. Press Space/Click to transition to FarmScene
7. Player appears at Shed Door with lantern in hand

---

## PART 3: FARMSCENE LANTERN PLACEMENT

### Option A: Auto-Place Lantern on Spawn

The lantern persists (DontDestroyOnLoad), but you'll want to place it somewhere:

1. In FarmScene, create an empty GameObject
   - Name: `LanternSpawnPoint`
   - Position: Near the shed door or a natural spot
2. In **SpawnManager.cs**, add after player spawning:
   ```csharp
   LanternController lantern = FindFirstObjectByType<LanternController>();
   if (lantern != null && lanternSpawnPoint != null)
   {
       lantern.PlaceInFarmScene(lanternSpawnPoint.position);
   }
   ```

### Option B: Manual Placement

1. Player carries lantern from intro
2. Player presses E in farm to place it down
3. Modify **LanternController.cs** to add:
   ```csharp
   if (isHeldByPlayer && Input.GetKeyDown(KeyCode.E))
   {
       DropLantern();
   }
   ```

---

## PART 4: CUSTOMIZATION

### Edit the Waking Narrative

In **NarrativeManagerEnhanced** Inspector:

```
Wake Up Narrative[]:
0: "Your eyes flutter open..."
1: "The darkness is overwhelming."
2: "A shed. Your shed. But you can barely see."
3: "There... a lantern on the table."
4: "Press E to pick it up."
```

Change to your own story!

### Edit the Lantern-Lit Narrative

```
Lantern Lit Narrative[]:
0: "Light floods the darkness."
1: "You can see clearly now."
2: "It's time to face the day."
3: "Press Space or Click to continue outside..."
```

### Change Lantern Color

In **LanternController.cs**, find `lanternLight.color`:

```csharp
Color: Warm yellow (1, 0.8, 0.4)
// Try: (1, 0.7, 0.3) red/orange
// Or: (0.5, 0.5, 1) cold blue
```

### Change Lantern Brightness

In **Point Light 2D** component:

- **Intensity:** Higher = brighter (1.5 default)
- **Range:** Higher = spreads further (8 default)

---

## PART 5: LIGHTING TIPS

### For Dramatic Waking Up:

1. Very dim global light (0.1-0.15)
2. Lantern light range: 6-8
3. Makes lantern feel ESSENTIAL

### For Cozy Shed Feel:

1. Soft warm color on lantern
2. Medium ambient light (0.3-0.4)
3. Still creates nice shadow effects

### Better Visuals:

- Add a semi-transparent dark overlay (black quad) with low alpha
- Place it between camera and scene for deeper darkness
- Lantern light will cut through it

---

## PART 6: BUILD SETTINGS

Already done, but verify:

- [ ] Build Settings has Scenes In Build:
  - [0] Intro.unity
  - [1] FarmScene.unity

---

## PART 7: TROUBLESHOOTING

| Issue                         | Solution                                                      |
| ----------------------------- | ------------------------------------------------------------- |
| Lantern not glowing           | Ensure Point Light 2D enabled is checked AFTER lantern pickup |
| Player can't pick up lantern  | Verify BoxCollider2D on lantern, "Is Trigger" checked         |
| Scene too bright/too dark     | Adjust Point Light 2D Intensity or Global Light 2D            |
| Text doesn't wait for lantern | Ensure LanternController is found (same scene)                |
| Lantern disappears in farm    | Use Option A or B to explicitly place it                      |

---

## FILE LOCATIONS

- **LanternController.cs:** `Assets/Scripts/Systems/LanternController.cs`
- **NarrativeManagerEnhanced.cs:** `Assets/Scripts/Systems/NarrativeManagerEnhanced.cs`

---

## QUICK CHECKLIST

- [ ] Intro scene has dim ambient lighting
- [ ] Player character sprite visible in scene
- [ ] Lantern sprite created with Point Light 2D (disabled)
- [ ] LanternController component on Lantern GameObject
- [ ] NarrativeManagerEnhanced on Canvas (not old NarrativeManager)
- [ ] PlayersChain complete text assignment
- [ ] Test: Press E to pick up → Light turns on → Narrative continues
- [ ] Test: Complete intro → Transitions to FarmScene
- [ ] Test: Player appears with lantern at Shed Door in FarmScene

---

## EXAMPLE SCENE HIERARCHY (Intro)

```
Intro.unity
├─ Main Camera
│  └─ Background: Black
├─ Canvas (NarrativeCanvas)
│  ├─ TextMeshPro Text (NarrativeText)
│  └─ NarrativeManagerEnhanced [Component]
├─ ShedBackground [Sprite]
├─ Player [Character + Sprite]
└─ Lantern [Sprite, Light 2D, Trigger Collider]
   └─ LanternController [Component]
```

---

## NEXT STEPS

1. Set up the visual elements above
2. Test in-editor
3. Play through the full intro → farm sequence
4. Customize narrative text to your story
5. Adjust lighting for desired atmosphere

Enjoy your immersive intro! 🔦
