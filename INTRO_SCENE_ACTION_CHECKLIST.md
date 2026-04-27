# Intro Scene Setup - Action Checklist

## ✅ PHASE 1: SCRIPTS READY (DONE)

- ✅ `NarrativeManager.cs` created at: `Assets/Scripts/Systems/NarrativeManager.cs`
- ✅ `SpawnManager.cs` created at: `Assets/Scripts/Systems/SpawnManager.cs`
- ✅ No modifications needed to these scripts

---

## ✅ PHASE 2: INTRO SCENE SETUP (DO THIS FIRST)

**Open: Intro.unity in Unity Editor**

### Step 1: Camera Setup

- [ ] Select **Main Camera** in Hierarchy
- [ ] In Inspector → Camera component → Background Type: **Solid Color**
- [ ] Set Color to Black (0, 0, 0, 1)

### Step 2: Create Canvas

- [ ] Right-click Hierarchy → 2D Object → **Canvas**
- [ ] Rename to: `NarrativeCanvas`
- [ ] Canvas settings:
  - [ ] Canvas Scaler → Reference Resolution: 1920 × 1080
  - [ ] Render Mode: "Screen Space - Overlay"

### Step 3: Add TextMeshPro Text

- [ ] Right-click **NarrativeCanvas** → TextMeshPro - **Text**
- [ ] Rename to: `NarrativeText`
- [ ] Configure TextMeshPro:
  - [ ] Font Size: 36
  - [ ] Alignment: Center, Middle
  - [ ] Color: White (1, 1, 1, 1)
- [ ] Set Rect Transform:
  - [ ] Width: ~1024, Height: ~256
  - [ ] Anchor: Center

### Step 4: Add NarrativeManager Script

- [ ] Select **NarrativeCanvas** (or create empty GameObject)
- [ ] Add Component → Search: **NarrativeManager**
- [ ] In Inspector, configure:
  - [ ] **Narrative Text**: Drag `NarrativeText` into this field
  - [ ] **Narrative Sequence**: Enter your story sentences (default has 4)
  - [ ] **Typewriter Speed**: 0.05 (adjust as needed)
  - [ ] **Scene To Load After**: `FarmScene`

### Step 5: Save Intro Scene

- [ ] File → Save Scene (or Ctrl+S)

---

## ✅ PHASE 3: FARMSCENE SPAWN SETUP (DO THIS SECOND)

**Open: FarmScene.unity in Unity Editor**

### Step 1: Create Shed Door Spawn Point

- [ ] Right-click Hierarchy → Create Empty
- [ ] Rename to: `ShedDoorSpawnPoint` (EXACT NAME)
- [ ] Set Position to where player should appear after intro
  - [ ] Example: X=10, Y=2, Z=0 (adjust to your shed door)

### Step 2: Create Default Spawn Point

- [ ] Right-click Hierarchy → Create Empty
- [ ] Rename to: `DefaultSpawnPoint` (EXACT NAME)
- [ ] Set Position to default spawn location
  - [ ] Example: X=0, Y=0, Z=0 (or your normal spawn)

### Step 3: Create SpawnManager GameObject

- [ ] Right-click Hierarchy → Create Empty
- [ ] Rename to: `SpawnManager`
- [ ] Keep at root level (not nested)

### Step 4: Add SpawnManager Component

- [ ] Select **SpawnManager** GameObject
- [ ] Add Component → Search: **SpawnManager**
- [ ] In Inspector, configure:
  - [ ] **Shed Door Spawn Point**: Drag `ShedDoorSpawnPoint` from Hierarchy
  - [ ] **Default Spawn Point**: Drag `DefaultSpawnPoint` from Hierarchy
  - [ ] **Shed Door Spawn Point Name**: `ShedDoorSpawnPoint`
  - [ ] **Default Spawn Point Name**: `DefaultSpawnPoint`

### Step 5: Verify Player Setup

- [ ] Find Player GameObject in Hierarchy
- [ ] Verify it has **CharacterController2D** component
- [ ] (SpawnManager will auto-find and position this)

### Step 6: Save FarmScene

- [ ] File → Save Scene (or Ctrl+S)

---

## ✅ PHASE 4: BUILD SETTINGS (DO THIS THIRD)

- [ ] File → Build Settings (Ctrl+Shift+B)
- [ ] Scenes in Build section:
  - [ ] If Intro not listed: Drag **Intro.unity** from Project into Scenes list
  - [ ] If FarmScene not listed: Drag **FarmScene.unity** into Scenes list
  - [ ] Reorder so: **Intro is Index 0**, **FarmScene is Index 1**
- [ ] Close Build Settings

---

## ✅ PHASE 5: TEST IT! (DO THIS FOURTH)

### Test 1: Play from Intro Scene

- [ ] Double-click **Intro.unity** to select it in Project
- [ ] Click Play button
- [ ] Verify:
  - [ ] Text appears with typewriter effect
  - [ ] Press Space → Text skips or advances
  - [ ] After all 4 sentences, scene transitions to FarmScene
  - [ ] Player appears in front of Shed Door

### Test 2: Play Direct from FarmScene (no intro flag)

- [ ] Double-click **FarmScene.unity** to select it
- [ ] Click Play button
- [ ] Verify:
  - [ ] Player appears at DefaultSpawnPoint location
  - [ ] (Not at Shed Door, since no intro was played)

### Test 3: Build & Play from Build

- [ ] File → Build and Run
- [ ] Verify:
  - [ ] Intro plays first
  - [ ] Narrative text shows correctly
  - [ ] FarmScene loads correctly
  - [ ] Player spawns at Shed Door

---

## 🎯 CUSTOMIZATION (OPTIONAL)

### Want to edit the story?

- [ ] Open Intro.unity
- [ ] Select NarrativeCanvas
- [ ] In Inspector → NarrativeManager → **Narrative Sequence**
- [ ] Edit the text strings in the array

### Want to change typewriter speed?

- [ ] Open Intro.unity
- [ ] Select NarrativeCanvas
- [ ] In Inspector → NarrativeManager → **Typewriter Speed**
- [ ] Lower number = slower typing (0.03 = fast)

### Want different spawn locations?

- [ ] Open FarmScene.unity
- [ ] Move **ShedDoorSpawnPoint** to new location
- [ ] Move **DefaultSpawnPoint** to new location

### Want next scene different than FarmScene?

- [ ] Open Intro.unity
- [ ] Select NarrativeCanvas
- [ ] In Inspector → NarrativeManager → **Scene To Load After**: Type scene name

---

## 🛠 TROUBLESHOOTING

| Issue                           | Solution                                                                       |
| ------------------------------- | ------------------------------------------------------------------------------ |
| Text not showing                | Ensure TextMeshPro asset imported; Canvas Graphic Raycaster added              |
| Input not working               | Click in game window to focus; check Input Manager (Edit → Project Settings)   |
| Player not at correct position  | Check spawn point names match exactly; verify SpawnManager references assigned |
| Scene doesn't load              | Both scenes must be in Build Settings; exact scene name required               |
| Typewriter typing too fast/slow | Adjust Typewriter Speed value in NarrativeManager                              |
| Nothing happens on Space/Click  | Verify EventSystem exists on Canvas; check Console for errors                  |

---

## 📋 PRE-BUILD CHECKLIST

Before building/deploying, verify:

- [ ] Intro.unity has black camera background
- [ ] Intro.unity has Canvas with TextMeshPro Text
- [ ] NarrativeManager assigned to Canvas with TextMeshPro reference
- [ ] FarmScene has SpawnManager GameObject
- [ ] FarmScene has ShedDoorSpawnPoint (named exactly)
- [ ] FarmScene has DefaultSpawnPoint (named exactly)
- [ ] Both spawn points assigned in SpawnManager Inspector
- [ ] Player has CharacterController2D component
- [ ] Build Settings has both scenes: Intro (0), FarmScene (1)
- [ ] Tested: Intro → Narrative → FarmScene transition works
- [ ] Tested: Player appears at Shed Door after Intro

---

## 📝 FILE REFERENCE

**Scripts Created:**

- `Assets/Scripts/Systems/NarrativeManager.cs` (ready to use)
- `Assets/Scripts/Systems/SpawnManager.cs` (ready to use)

**Documentation Created:**

- `INTRO_SCENE_SETUP_GUIDE.md` (detailed walkthrough)
- `INTRO_SCENE_QUICK_REFERENCE.md` (quick lookup)
- `INTRO_SCENE_ARCHITECTURE.md` (visual diagrams)
- `INTRO_SCENE_ACTION_CHECKLIST.md` (this file)

---

## 🎮 EXAMPLE CUSTOM STORY

Edit in NarrativeManager (Intro.unity → NarrativeCanvas → Inspector):

```
Element 0: "Years of dreams had finally come true."
Element 1: "Your grandfather's farm. Yours now."
Element 2: "But first, you had to wake up from this dream..."
Element 3: "Press Space to begin your new life."
```

---

## ❌ COMMON MISTAKES TO AVOID

- ❌ **Don't** forget to add scripts to scenes (add components in Inspector)
- ❌ **Don't** rename spawn points without updating the script references
- ❌ **Don't** forget Build Settings for both scenes
- ❌ **Don't** use different scene name in NarrativeManager than actual scene name
- ❌ **Don't** forget that SpawnManager needs CharacterController2D to exist on Player
- ❌ **Don't** modify the NarrativeManager or SpawnManager scripts (they're ready to use)

---

## ✨ YOU'RE DONE!

Once all checkboxes are complete, your intro scene is fully functional:

- ✅ Typewriter narrative effect with input handling
- ✅ Automatic transition to FarmScene
- ✅ Custom player spawn at Shed Door door
- ✅ Normal gameplay continues after intro

Enjoy your narrative intro! 🚜
