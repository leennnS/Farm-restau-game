# 🎣 FISHING SYSTEM - QUICK REFERENCE

## Installation Checklist

- [ ] Scripts copied to `Assets/Scripts/Fishing/`
- [ ] UI files in `Assets/UI/Fishing/` (uxml + uss)
- [ ] Example assets created in `Assets/Resources/Fishing/`
- [ ] Read FISHING_SYSTEM_SETUP.md completely
- [ ] Create lake trigger zone in your scene
- [ ] Add FishingUIController to UI Canvas
- [ ] Add FishingMiniGameController to a persistent GameObject
- [ ] Assign all references in Inspector
- [ ] Set player tag to "Player"
- [ ] Test with debug mode enabled

---

## Common Tasks

### Create a New Fish

1. **Right-click in Project** → Create → Fishing → Catchable Definition
2. **Name it** (e.g., "Rainbow Trout")
3. **Fill basic info**:
   - catchableName: "Rainbow Trout"
   - description: "A shimmering rainbow-colored trout"
   - catchFlavor: "You caught a beautiful rainbow trout!"
   - sellPrice: 120
   - rarity: Common or Uncommon

4. **Set difficulty** (0-1 scale):
   - 0.2 = very easy
   - 0.5 = medium
   - 0.8 = very hard

5. **Choose behavior type**:
   - Standard = predictable
   - FastDarter = erratic, quick moves
   - SlowHeavy = strong, slow
   - Elusive = tries to escape
   - Aggressive = sudden rushes
   - Cunning = unpredictable tricks

6. **Assign sprites**:
   - Catchable Icon = small inventory sprite
   - Catch UI Catchable Image = large result screen sprite

7. **Add to a zone**:
   - Open LakeZoneDefinition asset
   - Increase catchablePool size
   - Assign your fish to a slot
   - Set spawn weight (higher = more common)
   - Check enabled ✓

### Create a New Lake Zone

1. **Right-click** → Create → Fishing → Lake Zone Definition
2. **Name it** (e.g., "Deep Water Zone")
3. **Add description** (shows in code/tooltips)
4. **Add catchables to pool**:
   - Set catchablePool size
   - Drag fish into each slot
   - Set spawn weights (relative probability)
   - Check enabled for each

5. **Adjust difficulty modifier**:
   - 0.8 = easier than base difficulty
   - 1.0 = normal
   - 1.2 = harder than base difficulty

### Change Difficulty

**For whole game** → Edit `FishingSettings.asset`:

- Increase `successTensionMax` = easier (more forgiving window)
- Decrease `successWindow` = harder (less reaction time)
- Lower creature `difficultyScore` = easier fish
- Higher `creaturePullStrength` = harder fish

**For specific fish** → Edit CatchableDefinition:

- Lower `difficultyScore`
- Longer `successWindow`
- Lower `creaturePullStrength`

**For specific zone** → Edit LakeZoneDefinition:

- Adjust `difficultyModifier` (0.8 = 20% easier, 1.2 = 20% harder)

### Add Sound Effects

1. **Create audio clip files** in `Assets/Audio/Fishing/`
2. **Modify FishingUIController.cs**:

```csharp
[SerializeField] private AudioClip biteSound;
[SerializeField] private AudioClip successSound;
[SerializeField] private AudioClip failureSound;

private void HandleBiteOccurred() {
    if (biteSound != null) {
        AudioSource.PlayClipAtPoint(biteSound, Camera.main.transform.position);
    }
}
```

3. **Subscribe to events**:

```csharp
miniGameController.OnBiteOccurred += () =>
    PlaySfx(biteSound);

miniGameController.OnCatchComplete += (result) => {
    if (result == FishingResultType.Success)
        PlaySfx(successSound);
    else
        PlaySfx(failureSound);
};
```

### Connect to Inventory (Example)

**Assuming your inventory has `AddItem(string itemName, int quantity)`**:

```csharp
// In FishingUIController.cs
private void AddCatchableToInventory(CatchableDefinition catchable) {
    if (inventoryController == null) return;

    // Add item by name (or modify to use ItemDefinition)
    int quantity = 1;
    inventoryController.AddItem(catchable.catchableName, quantity);

    Debug.Log($"Added {catchable.catchableName} to inventory!");
}
```

**If you have ItemDefinition in your system**:

```csharp
// Create a mapping or create temporary ItemDefinition
ItemDefinition item = new ItemDefinition {
    itemName = catchable.catchableName,
    description = catchable.description,
    icon = catchable.catchableIcon,
    sellPrice = catchable.sellPrice,
    stackable = catchable.stackable
};
inventoryController.AddItem(item, 1);
```

### Change UI Colors/Styling

Edit **Assets/UI/Fishing/fishing-style.uss**:

```css
/* Header colors */
.phase-label {
  color: rgb(150, 200, 255); /* ← Blue */
}

/* Tension bar safe/danger states */
.tension-bar {
  background-color: rgb(200, 50, 50); /* ← Red */
}

/* Result text colors */
.result-status-label {
  color: rgb(100, 255, 150); /* ← Green */
}

/* Button styling */
.reel-button {
  background-color: rgb(80, 180, 100); /* ← Green */
  border-color: rgb(120, 220, 140);
}
```

### Test Specific Scenarios

**To always catch**:

1. Open FishingSettings.asset
2. Set `autoSucceed` = ✓
3. Fishing will always succeed

**To skip waiting**:

1. Set `skipBiteWait` = ✓
2. Bite occurs instantly

**To see console logs**:

1. Set `debugMode` = ✓
2. Check Console window

---

## Inspector Quick Reference

### FishingSettings (Global Config)

| Field                   | Range    | Purpose                    |
| ----------------------- | -------- | -------------------------- |
| `promptFadeInDuration`  | 0.1-0.5s | How fast prompt appears    |
| `promptFadeOutDuration` | 0.1-0.3s | How fast prompt disappears |
| `castPhaseDuration`     | 0.5-2s   | How long before auto-cast  |
| `successTensionMin`     | 0-0.5    | Lower bound of catch zone  |
| `successTensionMax`     | 0.5-1.0  | Upper bound of catch zone  |
| `lineBreakerThreshold`  | 0.9-0.99 | When line breaks           |
| `reelTensionIncrement`  | 0.03-0.1 | How much per reel input    |
| `reelTensionMax`        | 0.6-0.9  | Max when pulling hard      |

### CatchableDefinition (Fish Data)

| Field                  | Range  | Purpose                       |
| ---------------------- | ------ | ----------------------------- |
| `difficultyScore`      | 0-1    | Overall difficulty multiplier |
| `biteDelayMin/Max`     | 0.5-6s | How long before bite          |
| `successWindow`        | 0.2-1s | Reaction time window          |
| `catchDurationMin/Max` | 1-15s  | How long catch phase lasts    |
| `creaturePullStrength` | 0-1    | Base resistance               |
| `tensionIncreaseRate`  | 0-0.3  | How fast it climbs            |
| `canFakeOut`           | bool   | Fake bite before real         |
| `canDive`              | bool   | Sudden intensity spike        |
| `behaviorType`         | enum   | Behavior pattern              |

### LakeZoneDefinition (Zone Data)

| Field                | Range   | Purpose                    |
| -------------------- | ------- | -------------------------- |
| `catchablePool`      | array   | Fish types available here  |
| `spawnWeight`        | 0.1-100 | Relative probability       |
| `difficultyModifier` | 0.5-2.0 | Scales creature difficulty |

---

## Preset Difficulties

### Very Easy (Tutorial)

```
Catchables: difficultyScore = 0.1-0.2
Zone: difficultyModifier = 0.7
Settings: successWindow = 1.0, successTension = 0.1-0.9
```

### Easy (Beginners)

```
Catchables: difficultyScore = 0.3-0.4
Zone: difficultyModifier = 0.9
Settings: successWindow = 0.7, successTension = 0.15-0.85
```

### Normal (Standard)

```
Catchables: difficultyScore = 0.5
Zone: difficultyModifier = 1.0
Settings: successWindow = 0.5, successTension = 0.2-0.7
```

### Hard (Veterans)

```
Catchables: difficultyScore = 0.7-0.8
Zone: difficultyModifier = 1.2
Settings: successWindow = 0.35, successTension = 0.25-0.65
```

### Expert (Hardcore)

```
Catchables: difficultyScore = 0.85-1.0, Behavior = Aggressive/Cunning
Zone: difficultyModifier = 1.3
Settings: successWindow = 0.25, successTension = 0.35-0.65
```

---

## File Locations

```
Scripts:
  ✓ Assets/Scripts/Fishing/*.cs

UI:
  ✓ Assets/UI/Fishing/fishing-panel.uxml
  ✓ Assets/UI/Fishing/fishing-style.uss

Data:
  ✓ Assets/Resources/Fishing/*.asset

Documentation:
  ✓ FISHING_SYSTEM_SETUP.md (Start here!)
  ✓ FISHING_SYSTEM_ARCHITECTURE.md (Deep dive)
  ✓ FISHING_SYSTEM_QUICK_REFERENCE.md (This file)
```

---

## Troubleshooting Quick Fixes

| Problem                                          | Quick Fix                                                        |
| ------------------------------------------------ | ---------------------------------------------------------------- |
| Prompt doesn't show                              | Enable LakeFishingTrigger debug logs, check collider is trigger  |
| Can't react to bite                              | Increase `successWindow` in FishingSettings or catchable         |
| Always lose                                      | Decrease `creaturePullStrength` or `difficultyScore`             |
| Tension changes too fast                         | Lower `tensionIncreaseRate`, lower `creaturePullStrength`        |
| UI looks wrong                                   | Check USS file exists and is assigned to UIDocument              |
| Inventory not updated                            | Implement `AddCatchableToInventory()` method                     |
| Console error: "MiniGameController not assigned" | Drag FishingMiniGameController into LakeFishingTrigger inspector |

---

## Key Keybinds

| Key              | Action                    |
| ---------------- | ------------------------- |
| **E**            | Fish (in range)           |
| **Space**        | Reel (during catch phase) |
| **Button Click** | Reel (alternative)        |

---

## Important Concepts

**Tension (0-1 scale)**:

- 0.0-0.4 = Safe (green)
- 0.4-0.6 = Okay (yellow)
- 0.6-0.95 = Risky (red)
- > 0.95 = Line breaks (fail)

**Difficulty Score**:

- Affects bite delay
- Affects reaction window
- Affects catch duration
- Affects creature resistance

**Spawn Weight**:

- Higher = more likely
- Relative probability
- Sum of all weights = 100%

**Behavior Type**:

- Affects tension curve
- Affects player experience
- Different strategies needed
- Can combine in one zone

---

## Next Steps

1. ✓ Read FISHING_SYSTEM_SETUP.md
2. ✓ Set up lake zone in scene
3. ✓ Create 3-5 different fish
4. ✓ Test with debug mode
5. ✓ Adjust difficulty to feel right
6. ✓ Add your own sprites
7. ✓ Connect inventory system
8. ✓ Add sound effects (optional)
9. ✓ Playtest thoroughly
10. ✓ Deploy and enjoy! 🎣

---

**Happy fishing!**
