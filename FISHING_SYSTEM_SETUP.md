# 🎣 LAKE FISHING SYSTEM - COMPLETE SETUP GUIDE

## System Overview

The fishing system is fully modular and uses UI Toolkit for all player-facing UI. It consists of:

### Core Components

1. **Data Structures (ScriptableObjects)**
   - `CatchableDefinition`: Defines individual fish/creatures
   - `LakeZoneDefinition`: Defines zones with catchable pools
   - `FishingSettings`: Global system configuration

2. **Game Logic**
   - `FishingMiniGameController`: State machine & gameplay logic
   - `CatchableSelector`: Weighted random selection of catchables

3. **User Interface**
   - `FishingUIController`: Manages UI Toolkit presentation & player input
   - `LakeFishingTrigger`: Detects player presence, shows prompt

4. **UI Files**
   - `/Assets/UI/Fishing/fishing-panel.uxml`: UI structure
   - `/Assets/UI/Fishing/fishing-style.uss`: UI styling

---

## 📁 File Structure

```
Assets/
├── Scripts/Fishing/
│   ├── CatchableDefinition.cs          [Data: Single catchable]
│   ├── CatchableSelector.cs            [Logic: Random selection]
│   ├── FishingSettings.cs              [Data: Global settings]
│   ├── FishingMiniGameController.cs    [Logic: Game state machine]
│   ├── FishingUIController.cs          [UI: UI Toolkit integration]
│   └── LakeFishingTrigger.cs           [Interaction: Lake trigger zone]
├── UI/Fishing/
│   ├── fishing-panel.uxml              [UI: Layout]
│   └── fishing-style.uss               [UI: Styling]
└── Resources/Fishing/
    ├── FishingSettings.asset           [Config example]
    ├── Example_CommonFish.asset        [Fish 1]
    ├── Example_BlueTrout.asset         [Fish 2]
    ├── Example_GoldenKoi.asset         [Fish 3]
    └── Example_MainLake.asset          [Zone example]
```

---

## 🎮 SETUP STEPS

### Step 1: Create the Lake Interaction Object in Your Scene

1. In your lake scene, create an **Empty GameObject** named `LakeInteractionZone`
2. Add a **CircleCollider2D** (or BoxCollider2D for custom shape)
   - Set as **Trigger** ✓
   - Adjust size to cover the fishing area (e.g., radius 5-8)
3. Add a **Canvas** for the UI (or use existing UI Root)
4. Add **UIDocument** component to the Canvas
   - Assign `Assets/UI/Fishing/fishing-panel.uxml` to the **Panel Asset** field
5. Attach **FishingUIController** script to the same GameObject with UIDocument

### Step 2: Create Fishing Prefab/GameObject

Create an empty GameObject as a child of your UI Canvas:

1. Name: `FishingSystem`
2. Add **FishingMiniGameController** script
3. In Inspector:
   - Assign **FishingSettings**: Load `FishingSettings.asset` from Resources
   - Assign **Lake Zone**: Example_MainLake.asset
   - Enable **Debug Logging** (optional, for testing)

### Step 3: Set Up Lake Trigger

On your **LakeInteractionZone** GameObject:

1. Add **LakeFishingTrigger** script
2. Configure in Inspector:
   - **Lake Zone**: Assign `Example_MainLake.asset`
   - **Fishing Settings**: Assign `FishingSettings.asset`
   - **Mini Game Controller**: Drag the FishingMiniGameController GameObject
   - **Fishing UI Controller**: Drag the FishingUIController component
   - **Fish Key**: KeyCode.E (or your preference)
   - **Player Tag**: "Player" (match your player's tag)

### Step 4: Create UI Document for Prompt

Add a **UIDocument** as a child of your UI Canvas:

1. Create empty GameObject: `FishingPrompt`
2. Add **UIDocument** component
3. Create a simple UXML file with:
   ```xml
   <ui:VisualElement>
       <ui:VisualElement name="fishing-prompt"
           style="position: absolute; bottom: 80px; left: 50%; padding: 10px;
           background-color: rgba(20,40,60,0.8); border-radius: 6px;">
           <ui:Label text="[E] Fish" />
       </ui:VisualElement>
   </ui:VisualElement>
   ```
   Or reference the prompt section from fishing-panel.uxml

### Step 5: Verify Player Setup

Ensure your player GameObject:

- Has tag "Player" ✓
- Has a Collider2D (not trigger) for physics ✓
- Has CharacterController2D or similar movement script ✓

---

## 🐟 Creating New Catchables

### Option A: Create Asset via Inspector

1. Right-click in Project → **Create → Fishing → Catchable Definition**
2. Name it (e.g., "RarePike")
3. Fill out all fields:

```
Name:                  RarePike
Description:           A shimmering rare pike
Sell Price:            150
Rarity:                Rare (2)
Difficulty Score:      0.7

Bite Delay:           2.0 - 5.0 seconds
Behavior Type:        Aggressive
Tension Increase:     0.12
Tension Decay:        0.04
Creature Pull:        0.6

Success Window:       0.4 seconds
Catch Duration:       5-10 seconds
Can Fake Out:         Yes (30% chance)
Can Dive:             Yes (0.5 intensity)
```

### Option B: Duplicate & Modify Example

1. Copy `Example_CommonFish.asset`
2. Rename to your fish name
3. Modify fields in Inspector

---

## 🌊 Creating Lake Zones

### Steps

1. Right-click → **Create → Fishing → Lake Zone Definition**
2. Name it (e.g., "DeepWater")
3. Fill in zone details:
   - **Zone Name**: "Deep Water"
   - **Description**: "A deep, cold part of the lake..."
   - **Difficulty Modifier**: 1.2 (makes fish harder)

4. **Add Catchables** to the Pool:
   - Size the array: `catchablePool [3]`
   - Set each element:
     ```
     [0] Catchable: Example_CommonFish, Weight: 50, Enabled: ✓
     [1] Catchable: Example_BlueTrout, Weight: 30, Enabled: ✓
     [2] Catchable: Example_GoldenKoi, Weight: 10, Enabled: ✓
     ```

**How Weights Work:**

- Total weight = 50 + 30 + 10 = 90
- CommonFish: 50/90 = 55% chance
- BlueTrout: 30/90 = 33% chance
- GoldenKoi: 10/90 = 11% chance

---

## 📊 Difficulty System

### Catchable Difficulty Formula

```
Final Difficulty = BaseDifficulty × ZoneDifficultyModifier

Affects:
- Bite Delay (lower difficulty = faster bite)
- Tension increase rate
- Creature pull strength
- Success window (harder = smaller)
- Catch phase duration (harder = longer)
```

### Behavior Types Explained

| Type           | Behavior                       | Best For              |
| -------------- | ------------------------------ | --------------------- |
| **Standard**   | Consistent resistance          | Easy starting fish    |
| **FastDarter** | Erratic, quick movements       | Medium challenge      |
| **SlowHeavy**  | Strong, constant pull          | High difficulty       |
| **Elusive**    | Tries to escape, loose tension | Tricky medium fish    |
| **Aggressive** | Sudden dives, rushes           | Dynamic challenge     |
| **Cunning**    | Switches behavior randomly     | Unpredictable, expert |

---

## 🎨 Adding Your Own Sprites

### For Catchable Images

1. Create/import your fish sprites into `Assets/Art/Fish/` (or preferred folder)
2. Select sprite, ensure settings are correct
3. In your CatchableDefinition asset:
   - Drag sprite into **Catchable Icon** field (small icon for inventory)
   - Drag sprite into **Catch UI Catchable Image** field (large display in result screen)
   - Set **UI Accent Color** to match the sprite's dominant color

### Example Colors

```
Blue Fish:     R:0.2, G:0.6, B:1.0
Gold Fish:     R:1.0, G:0.84, B:0
Red Fish:      R:1.0, G:0.2, B:0.2
Green Fish:    R:0.2, G:1.0, B:0.4
```

---

## 🎮 Gameplay Loop - What Happens

### 1. Player Approaches Lake

- LakeFishingTrigger detects player enter zone
- Fishing prompt appears: "[E] Fish"

### 2. Player Presses E

- FishingUIController opens fishing panel
- PlayerMovement disabled
- FishingMiniGameController starts session

### 3. Game Phases

#### Phase 1: Casting

- Duration: `castPhaseDuration` (default 1 second)
- UI shows "Getting ready..."
- Player watches indicator

#### Phase 2: Anticipating

- Wait for bite occurs
- Random delay: `biteDelayMin` to `biteDelayMax`
- Fish might fake out (if enabled)
- UI shows "Waiting for a bite..."

#### Phase 3: Biting (REACTION REQUIRED!)

- Fish bites - sudden event
- Tension jumps by `biteTensionIncrease`
- Player has `successWindow` seconds to react
- Missing = failure (MissedBite)
- UI shows "BITE! React now!"

#### Phase 4: Catching (TENSION MANAGEMENT)

- Player must manage line tension
- Hold Space / click button to reel (increases tension)
- Release to let line relax (decreases tension)
- UI shows creature image, tension bar
- Creature applies resistance based on behavior

#### Phase 5: Result

- Success if final tension in `successTensionMin` to `successTensionMax`
- Too high = line breaks
- Too low = creature escapes
- Show result screen with creature image

### 6. Return to World

- Continue button clicked
- FishingUIController closes
- Player movement re-enabled
- Item added to inventory
- Prompt reappears if still in zone

---

## ⚙️ Tweaking Settings

### FishingSettings.asset Configuration

**Tension Values (0-1 scale)**

```
Safe Zone:              0 - 0.4  (Green, relaxed)
Caution Zone:           0.4 - 0.6  (Yellow, okay)
Danger Zone:            0.6 - 0.95  (Red, risky)
Line Breaking Point:    > 0.95  (Game over)
```

**Timing (seconds)**

```
Cast Duration:         0.5-2.0  (How long to prepare)
Anticipation:          1.0-4.0  (Wait for bite range)
Reel Input:            0.1-0.3  (Input detection window)
Bite Tension Bump:     0.2-0.4  (Tension spike on bite)
```

**Player Control**

```
Reel Increment:        0.03-0.08  (Tension gain per reel)
Reel Tension Max:      0.7-0.9  (Max tension when pulling)
Tension Decay:         0.04-0.10  (How fast line relaxes)
```

### Quick Difficulty Adjustments

**Make Everything Easier:**

```
Difficulty Scores:        Lower (e.g., 0.2 instead of 0.6)
Success Window:           Wider (e.g., 0.8 instead of 0.4)
Bite Delay:              Faster (e.g., 1.0 - 2.0 instead of 3.0 - 6.0)
Success Tension Zone:     Wider (0.1 - 0.8 instead of 0.2 - 0.7)
```

**Make Everything Harder:**

```
Difficulty Scores:        Higher (0.8-0.95)
Success Window:           Narrower (0.2-0.3)
Creature Pull:            Stronger (0.7-1.0)
Tension Decay:            Slower (0.01-0.03)
```

---

## 🐛 Debugging

### Enable Debug Mode

In `FishingSettings.asset`:

```
Debug Mode:            ✓ Enabled
Skip Bite Wait:        ✓ (for quick testing)
Auto Succeed:          ✓ (always catches)
```

### Console Logs

All scripts log to console with `[FishingUI]` prefix. Check Console window for:

- Phase transitions
- Tension changes
- Success/failure reasons
- Catchable selections

### Testing Checklist

- [ ] Prompt appears when entering zone
- [ ] Prompt disappears when leaving zone
- [ ] E key opens fishing UI
- [ ] Phases transition correctly (watch console)
- [ ] Tension bar updates visually
- [ ] Fish behaviors vary (different creatures feel different)
- [ ] Tension decay works (line relaxes over time)
- [ ] Result screen shows correct creature
- [ ] Continuing exits UI and re-enables movement
- [ ] Item added to inventory

---

## 🚀 Future Enhancement Ideas

### Immediate Additions

1. **Bait System**: Different baits attract different fish
   - `BaitDefinition` ScriptableObject
   - Apply attractiveness multiplier to spawn weights

2. **Fishing Rod Types**: Different rods have different characteristics
   - Rod strength affects max line tension
   - Rod speed affects reel increment
3. **Skill Progression**: Player fishing level affects difficulty
   - Higher level = lower difficulty modifier
   - Unlock better rods as you level

### Advanced Features

1. **Daily Catch Limits**: Track caught fish per day
2. **Weather Effects**: Rain/sun affect which fish bite
3. **Time of Day**: Morning/evening attract different fish
4. **Multiple Lake Zones**: Each zone has unique creatures
5. **Achievements**: "First catch", "Legendary fish", etc.
6. **Sound Effects**: Bite alert, reel tension, success fanfare
7. **Fish Animation**: Creature movement during catch phase
8. **Market Integration**: Sell caught fish to NPCs

### Implementation Hooks

These features fit with minimal changes:

- Add time/weather checks in `CatchableSelector`
- Add rod parameter to `FishingMiniGameController`
- Add skill level modifier to difficulty calculation
- Create `BaitDefinition` + `FishingRodDefinition`
- Use events in `FishingUIController` for sound/animation triggers

---

## 🎯 Customization Checklist

Before considering "done", customize:

- [ ] Create your own fish/creature definitions
- [ ] Create your own lake zone(s)
- [ ] Add sprite references to catchables
- [ ] Adjust difficulty settings to feel right for your game
- [ ] Tweak UI colors and layout in USS
- [ ] Add sound effects
- [ ] Connect inventory system (modify `AddCatchableToInventory`)
- [ ] Test with your player controller
- [ ] Playtest catching difficulty
- [ ] Playtest success rates (feel rewarding?)

---

## 📝 Notes

- **Inventory Integration**: Modify `FishingUIController.AddCatchableToInventory()` to match your inventory API
- **Player Movement**: The system disables `CharacterController2D` during fishing - adjust if your player class has different name
- **UI Scaling**: The USS uses relative units; adjust font sizes if needed for your target resolution
- **Sound**: Hook audio events to `FishingMiniGameController` events (OnBiteOccurred, OnCatchComplete)

---

## 📧 Questions/Issues?

If you encounter issues:

1. Check Console for [FishingUI] logs
2. Enable Debug Mode in FishingSettings
3. Verify all component references are assigned
4. Ensure Player has correct tag ("Player")
5. Check that catchables have valid data (not null references)

---

**Happy Fishing! 🎣**
