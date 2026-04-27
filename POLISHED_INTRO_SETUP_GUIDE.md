# Polished Intro Scene - Complete Setup Guide

## Overview

This guide sets up a complete, cinematic intro sequence:

1. Player wakes in front of shed (frozen)
2. Narrative with typewriter effect unfolds
3. Lantern flickers to life as story event
4. Player gains movement
5. Lantern light is directional (follows facing direction)
6. Player finds and reads an inherited farm letter
7. Smooth transition to normal gameplay

---

## Scripts Created

| Script                         | Purpose                                                |
| ------------------------------ | ------------------------------------------------------ |
| `IntroSequenceManager.cs`      | Master controller for all intro states and progression |
| `IntroNarrativeController.cs`  | Typewriter narrative, hints, UI text display           |
| `ImprovedLanternController.cs` | Directional lantern light, pickup, activation          |
| `NoteInteraction.cs`           | Interactable note in world                             |
| `LetterPanel.cs`               | Beautiful parchment letter UI (UIToolkit)              |

**Location:** `Assets/Scripts/Systems/`

---

## Scene Setup (Intro.unity)

### Step 1: Scene Root Objects

Create these at the root level of your Intro scene hierarchy:

1. **IntroSequenceManager** (empty GameObject)
   - Add Component: `IntroSequenceManager`
   - This is the master controller

2. **Canvas** (for narrative text)
   - Create: Right-click Hierarchy → UI Toolkit → Panel
   - Name: `NarrativeCanvas`
   - This handles text display

3. **LetterUIDocument** (for letter display)
   - Create: Right-click Hierarchy → UI Toolkit → Panel
   - Name: `LetterUIDocument`
   - Add Component: `LetterPanel`

4. **Player** (from your farm prefab or existing)
   - Should already have: `CharacterController2D`, `Rigidbody2D`
   - Position: In front of shed, e.g., (0, -1, 0)

5. **Lantern** (visible world object)
   - Position: Off to side, e.g., (2, 0.5, 0)
   - See detailed setup below

6. **Note** (interactable object)
   - Position: Near shed, e.g., (1.5, -2, 0)
   - See detailed setup below

7. **ShedBackground** (optional visual)
   - Sprite showing shed interior
   - Position: Behind everything
   - Order in Layer: -10

### Step 2: Configure IntroSequenceManager

1. Select `IntroSequenceManager` in Hierarchy
2. In Inspector, assign:
   - **Player Controller**: Drag your Player GameObject
   - **Lantern**: Drag the Lantern GameObject
   - **Note**: Drag the Note GameObject
   - **Narrative Controller**: Drag NarrativeCanvas (see Step 3)

### Step 3: Setup Narrative Canvas & Text

1. Select **NarrativeCanvas** (the UI Toolkit Panel)
2. Add Component: `IntroNarrativeController`
3. In Inspector, assign:
   - **Narrative Text**: Create a TextMeshPro label in the panel or assign an existing one
   - **Hint Text**: Create another TextMeshPro label for hints
   - **Typewriter Speed**: 0.05 (or adjust for pacing)
   - **Fade In Duration**: 0.5
   - **Narrative Canvas Group**: Auto-finds CanvasGroup, or assign if exists

4. Position your text objects:
   - Main narrative text: Center-top of screen
   - Hint text: Center-bottom of screen

### Step 4: Setup Lantern

**Create Lantern GameObject:**

1. Create empty GameObject
2. Name: `Lantern`
3. Position: (2, 0.5, 0) ← Off to the side where player can find it

**Add Components:**

1. **SpriteRenderer**
   - Sprite: Your lantern sprite (any lantern image)
   - Color: (0.6, 0.6, 0.6, 1) ← Starts dim
   - Sorting Layer: Put above background but at same level as player

2. **BoxCollider2D**
   - Check: **Is Trigger** ✓
   - Size: Around lantern sprite
   - (No Rigidbody2D!)

3. **Light 2D** (from Universal Rendering)
   - Type: **Spot** (initially; ImprovedLanternController will control it)
   - Intensity: 1.8
   - Outer Radius: 6
   - Color: Orange/warm (1, 0.8, 0.4) or (1, 0.9, 0.5)
   - ✅ UNCHECK **Enabled** ← Starts dark

4. **ImprovedLanternController** script
   - **Lantern Light**: Drag the Light 2D component
   - **Lantern Sprite**: Drag the SpriteRenderer component
   - **Light Anchor**: Leave empty (script creates it)
   - **Pickup Distance**: 2
   - **Directional Light Arc Angle**: 120
   - **Light Intensity**: 1.8
   - **Light Range**: 6

### Step 5: Setup Note

**Create Note GameObject:**

1. Create empty GameObject
2. Name: `Note`
3. Position: (1.5, -2, 0) ← On the ground near shed

**Add Components:**

1. **SpriteRenderer**
   - Sprite: A simple note/letter sprite
   - Color: (1, 1, 1, 0.3) ← Starts very dim
   - Sorting Layer: Same as player

2. **BoxCollider2D**
   - Size: Around note sprite
   - ✅ Check **Is Trigger**
   - This is for mouse click detection

3. **NoteInteraction** script
   - **Discovery Distance**: 3 (how close player must get)
   - **Note Sprite**: Assign the SpriteRenderer
   - **Note Collider**: Assign the BoxCollider2D
   - **Letter Panel**: Drag the LetterUIDocument GameObject

### Step 6: Configure Letter UI

1. Select **LetterUIDocument** (the UI Toolkit Panel)
2. Add Component: `LetterPanel`
3. In Inspector:
   - **UI Document**: Auto-assigns if component exists
   - **Letter Title**: "An Unexpected Inheritance"
   - **Letter Content**: Use the default or edit your own

The letter creates its own UI automatically at runtime using UIToolkit Code.

### Step 7: Lighting Setup

1. Go to **Window → Rendering → Lighting**
2. Set **Global Light 2D Intensity** to **0.1** or **0.15**
   - This creates darkness so lantern light is essential
3. You can adjust based on desired atmosphere

### Step 8: Camera Setup

1. Select **Main Camera**
2. Set Background Color to **black**
3. Verify orthographic view (2D)
4. Adjust size/position as needed for intro scene framing

---

## Wiring References in Inspector

### IntroSequenceManager (complete checklist)

- [ ] Player Controller: Player GameObject with CharacterController2D
- [ ] Lantern: Lantern GameObject with ImprovedLanternController
- [ ] Note: Note GameObject with NoteInteraction
- [ ] Narrative Controller: NarrativeCanvas GameObject with IntroNarrativeController

### IntroNarrativeController (complete checklist)

- [ ] Narrative Text: TextMeshProUGUI for main narrative
- [ ] Hint Text: TextMeshProUGUI for hints
- [ ] Typewriter Speed: 0.05
- [ ] Fade In Duration: 0.5

### ImprovedLanternController (complete checklist)

- [ ] Lantern Light: Light 2D component on Lantern
- [ ] Lantern Sprite: SpriteRenderer component on Lantern
- [ ] Light Anchor: (leave empty, auto-created)
- [ ] Pickup Key: E
- [ ] Toggle Key: E

### NoteInteraction (complete checklist)

- [ ] Note Sprite: SpriteRenderer component
- [ ] Note Collider: BoxCollider2D component
- [ ] Letter Panel: LetterUIDocument GameObject with LetterPanel
- [ ] Discovery Distance: 3

### LetterPanel (complete checklist)

- [ ] UI Document: auto-finds
- [ ] Letter Title: Set your title
- [ ] Letter Content: Set your letter text

---

## Testing the Intro Sequence

### Full Playthrough Test

1. Play Intro scene
2. Expected sequence:
   - [ ] Text appears with typewriter: "You wake in front of your old shed"
   - [ ] Fade in, text lines appear
   - [ ] Text fades out
   - [ ] Lantern flickers 3 times
   - [ ] Lantern stays lit
   - [ ] Player can now move (try arrow keys)
   - [ ] Hint appears: "Pick up the lantern"
   - [ ] Walk near lantern and press E to pick up
   - [ ] Light follows player and rotates in facing direction
   - [ ] Walk away from note, then towards it
   - [ ] Note sprite pulses/brightens when nearby
   - [ ] Mouse over and click on note
   - [ ] Beautiful parchment letter appears
   - [ ] Read letter, click Close
   - [ ] Player regains control
   - [ ] Scene transitions to FarmScene

### Individual Component Tests

**Test Player Freeze:**

- Play scene, press arrow keys
- Player should NOT move during opening narrative
- After lantern activation and narrative end, press arrow keys
- Player should move

**Test Lantern:**

- Watch for 3 flickers at activation
- Lantern light should turn on after flickers
- Pick up with E
- Light rotates with player facing direction
- Press E again to toggle off/on

**Test Note:**

- Once player is unfrozen, approach note
- Note sprite should pulse/brighten
- Click on note (mouse click)
- Letter should zoom in
- Parchment visual looks rustic and aged

---

## Customization

### Edit Opening Narrative

In `IntroNarrativeController.cs`, change the `openingLines` array:

```csharp
private string[] openingLines = new string[]
{
    "Your opening line here.",
    "Second line.",
    "Third line."
};
```

### Edit Letter Content

In `LetterPanel` Inspector:

- **Letter Title**: Your custom title
- **Letter Content**: Your custom letter text

Or in code in `LetterPanel.cs`:

```csharp
private string letterContent =
    "Your custom letter text here.\n\n" +
    "Can span multiple paragraphs.";
```

### Adjust Lighting

- Lantern intensity: `ImprovedLanternController` → **Light Intensity**
- Lantern range: `ImprovedLanternController` → **Light Range**
- Global darkness: Window → Rendering → Lighting → Global Light 2D Intensity

### Adjust Timing

- Typewriter speed: `IntroNarrativeController` → **Typewriter Speed**
- Fade duration: `IntroNarrativeController` → **Fade In Duration**
- Lantern flicker timing: `ImprovedLanternController.PlayActivationSequence()`

---

## Build Settings

Ensure Build Settings contains (File → Build Settings):

- [0] Intro.unity
- [1] FarmScene.unity
- (other scenes as needed)

---

## Troubleshooting

| Issue                       | Solution                                                          |
| --------------------------- | ----------------------------------------------------------------- |
| Player moves during opening | Check IntroSequenceManager assigned to correct Player             |
| Lantern not picking up      | Ensure BoxCollider2D has "Is Trigger" checked                     |
| No lantern light            | Light 2D must have "Enabled" UNCHECKED to start, verify intensity |
| Letter won't open           | Ensure LetterPanel assigned in NoteInteraction                    |
| Text not appearing          | Check TextMeshPro asset is imported, CanvasGroup exists           |
| Note not appearing          | Ensure NoteInteraction is assigned in IntroSequenceManager        |
| Scene too dark/bright       | Adjust Global Light 2D Intensity in Lighting window               |

---

## Code Integration Points

The system integrates with your existing:

- `CharacterController2D` - disabled/enabled for freeze/unfreeze
- `GameManager` - signals intro completion
- `SceneManager` - transitions to FarmScene
- UIToolkit - for letter display

No breaking changes to existing systems.

---

## Scene Hints to Show

During gameplay, the system shows these hints:

- "Pick up the lantern." ← After movement unlock
- (Custom hints via `narrativeController.ShowHint(text)`)

---

## Optional Enhancements

- Add ambient music when lantern lights
- Add particle effects for lantern flicker
- Add footstep sounds as player moves
- Add subtle fade/vignette effect during scenes
- Animate letter opening with page flip effect

---

This completes the polished intro sequence! Test thoroughly before building.
