# Intro Scene Implementation - Quick Summary

## What Was Created

### 1. NarrativeManager.cs

**Location:** `Assets/Scripts/Systems/NarrativeManager.cs`

**What it does:**

- Displays narrative text with typewriter effect (character by character)
- Handles Space/Click input to skip or advance sentences
- Transitions to FarmScene when narrative ends
- Sets the "FromIntroScene" flag in PlayerPrefs

**Key Features:**

- `narrativeSequence[]` - Array of text strings to display
- `typewriterSpeed` - Controls typing speed (0.05 = default)
- Input detection for Space key and Mouse click
- Automatic scene transition

### 2. SpawnManager.cs

**Location:** `Assets/Scripts/Systems/SpawnManager.cs`

**What it does:**

- Checks if player came from Intro scene using PlayerPrefs flag
- Positions player at Shed Door spawn point if from Intro
- Positions player at default spawn point for normal loads
- Automatically finds and positions the player

**Key Features:**

- Reads "FromIntroScene" flag from PlayerPrefs
- Falls back to GameObject.Find() if references not set
- Clears the intro flag after spawning (only works once)
- Automatic player detection via CharacterController2D

---

## 3-Step Setup (Minimum)

### Step 1: Intro Scene Canvas

In **Intro.unity**:

1. Camera background: Black
2. Add Canvas named `NarrativeCanvas`
3. Add TextMeshPro Text child: `NarrativeText`
4. Add NarrativeManager component to Canvas
5. Assign TextMeshPro reference

### Step 2: FarmScene Spawn Points

In **FarmScene.unity**:

1. Create `ShedDoorSpawnPoint` empty GameObject (position at shed door)
2. Create `DefaultSpawnPoint` empty GameObject (position at default spawn)
3. Create SpawnManager empty GameObject
4. Add SpawnManager component
5. Assign both spawn point references

### Step 3: Build Settings

1. File → Build Settings
2. Add Intro scene (Index 0)
3. Add FarmScene (Index 1)
4. Save build settings

---

## GameObject Naming Convention

**These names are referenced by the scripts. Use EXACTLY as shown:**

| Name                 | Purpose                          | Scene     | Example Position |
| -------------------- | -------------------------------- | --------- | ---------------- |
| `ShedDoorSpawnPoint` | Where player appears after Intro | FarmScene | (10, 2, 0)       |
| `DefaultSpawnPoint`  | Default player spawn             | FarmScene | (0, 0, 0)        |

---

## Input System

**Intro Scene accepts:**

- **Space bar:** Skip text or advance sentence
- **Left mouse click:** Skip text or advance sentence

**Behavior:**

- While typing: Skip to end of current sentence
- After typing: Move to next sentence
- At end: Transition to FarmScene

---

## Data Flow

```
INTRO SCENE START
    ↓
NarrativeManager sets PlayerPrefs "FromIntroScene" = 1
    ↓
Player reads narrative (typewriter effect)
    ↓
Player presses Space/Click through all sentences
    ↓
Scene loads FarmScene
    ↓
SpawnManager reads PlayerPrefs flag
    ↓
Player positioned at Shed Door ← Intro-specific spawn
    ↓
PlayerPrefs flag cleared (won't repeat on reload)
    ↓
FARMSCENE ACTIVE
```

---

## Variables to Configure (Inspector)

### NarrativeManager

```
Narrative Text: [TextMeshProUGUI] ← Assign your text object
Narrative Sequence[]: [Array of strings] ← Your story text
Typewriter Speed: 0.05 ← Adjust typing speed (lower = slower)
Scene To Load After: "FarmScene" ← Next scene name
```

### SpawnManager

```
Shed Door Spawn Point: [Transform] ← Assign ShedDoorSpawnPoint
Default Spawn Point: [Transform] ← Assign DefaultSpawnPoint
Shed Door Spawn Point Name: "ShedDoorSpawnPoint" ← Fallback name
Default Spawn Point Name: "DefaultSpawnPoint" ← Fallback name
```

---

## Example Narrative Sequence

Default story (edit in Inspector):

```
1. "The sun rises over the horizon..."
2. "You wake up in front of your old shed."
3. "It's time to start your new life."
4. "Press Space or Click to continue..."
```

---

## Common Adjustments

### Speed up/slow down typing

NarrativeManager → Typewriter Speed

- 0.03 = fast
- 0.05 = default
- 0.10 = slow

### Change player spawn location

Move the GameObject in FarmScene:

- `ShedDoorSpawnPoint` to new incoming position
- `DefaultSpawnPoint` to new default position

### Use different next scene

NarrativeManager → Scene To Load After: `YourSceneName`

### Edit story text

NarrativeManager → Narrative Sequence array

---

## Verification Checklist

- [ ] Intro scene has black background
- [ ] Canvas exists with TextMeshPro Text
- [ ] NarrativeManager assigned to Canvas with TextMeshPro reference
- [ ] SpawnManager exists in FarmScene
- [ ] Both spawn points (ShedDoor, Default) exist in FarmScene
- [ ] Player has CharacterController2D component
- [ ] Build Settings: Intro (0), FarmScene (1)
- [ ] Tested: Run Intro → Text types → Press Space → Transitions to FarmScene
- [ ] Tested: Player appears at Shed Door on first load from Intro

---

## If Something Doesn't Work

**Check Console (Ctrl+Shift+C):**

- `[NarrativeManager]` messages
- `[SpawnManager]` messages

**Most common issue:** Build Settings doesn't have both scenes added
**Second most common:** Spawn points not named correctly or not assigned

---

## To Integrate with Existing NewGame()

Optional: Have your menu call the Intro scene instead of FarmScene directly.

In **GameManager.cs** → **NewGame()** method, change last line from:

```csharp
SceneManager.LoadScene("FarmScene");
```

To:

```csharp
SceneManager.LoadScene("Intro");
```

This ensures new games always see the narrative intro.
