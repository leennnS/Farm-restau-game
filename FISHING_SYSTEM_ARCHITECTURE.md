# 🎣 LAKE FISHING SYSTEM - ARCHITECTURE & ADVANCED CUSTOMIZATION

## System Architecture

### Data Flow

```
LakeFishingTrigger
    ↓ (player in range)
    └─→ Show Prompt ("Press E")
        ↓ (on E key)
        └─→ Open FishingUIController
            └─→ Start FishingMiniGameController
                L CatchableSelector.SelectCatchable(zone)
                └─→ Return random CatchableDefinition

FishingMiniGameController (Update Loop)
    ├─→ UpdatePhase()
    │   └─→ Emit Events (OnBiteOccurred, OnTensionChanged, OnCatchComplete)
    │
    └─→ FishingUIController (Listens to Events)
        ├─→ HandleBiteOccurred() → Update UI visuals
        ├─→ HandleTensionChanged() → Update bar color/value
        └─→ HandleCatchComplete() → Show result screen
            └─→ AddCatchableToInventory()
```

### State Machine

```
START
  ↓
┌─────────────────────────────────┐
│    1. CASTING (~1 sec)          │
│    - Auto-cast after delay      │
│    - UI: "Getting ready..."     │
└─────────────────────────────────┘
  │ stateTimer >= castPhaseDuration
  ↓
┌─────────────────────────────────────────┐
│    2. ANTICIPATING (1-4 sec)            │
│    - Wait for bite with tension build   │
│    - Fake outs possible                 │
│    - UI: "Waiting for bite..."          │
│    - Subtle animation loop              │
└─────────────────────────────────────────┘
  │ Random delay or fake out → Real bite
  ↓
┌──────────────────────────────────────────────┐
│    3. BITING (~0.5 sec reaction window)      │
│    - TENSION SPIKE                          │
│    - Player MUST react with input           │
│    - UI: "BITE! React now!" (flashing)      │
│    - Haptic feedback available              │
└──────────────────────────────────────────────┘
  ├─ Player reacts in time → CATCHING
  ├─ Player misses → COMPLETE (MissedBite)
  └─ Timer expires → COMPLETE (MissedBite)
      ↓
┌─────────────────────────────────────────────────────┐
│    4. CATCHING (3-12 sec, tension management)      │
│    - Creature applies resistance (behavior-based)  │
│    - Player reels (Space/button) to pull           │
│    - Tension increases when reeling                │
│    - Tension decreases when releasing              │
│    - Line breaks if tension > 0.95                 │
│    - Creature escapes if tension too low too long  │
│    - UI: Creature image, bars, visual hints        │
└─────────────────────────────────────────────────────┘
  │ catchPhaseTimer >= duration
  ↓
Check Final Tension:
  ├─ In success zone (0.2-0.7) → SUCCESS
  ├─ Too high (>0.7) → TOO_MUCH_TENSION (line breaks implied)
  ├─ Too low (<0.2) → ESCAPED
  └─ Catch duration exceeded with bad tension → various failures
      ↓
┌─────────────────────────────────┐
│    5. COMPLETE                  │
│    - Show result screen         │
│    - Add item to inventory      │
│    - Credit sell value (later)  │
└─────────────────────────────────┘
      ↓
   CLOSE UI
   RESTORE PLAYER CONTROL
   LOOP (prompt ready again)
```

---

## Component Interaction Details

### FishingMiniGameController

**Primary Responsibilities:**

- Manages game state transitions
- Calculates creature behavior/resistance
- Applies tension physics
- Detects win/loss conditions

**Key Methods:**

```csharp
StartFishing(zone)              // Initialize session
UpdateFishing()                 // Called each frame
OnPlayerInput_Reel()            // Player pulls line
OnPlayerInput_Release()         // Player releases
GetCurrentState()       → FishingState          // Query state
GetCurrentCatchable()   → CatchableDefinition  // Current fish
GetCurrentTension()     → float (0-1)          // Tension value
GetCatchPhaseProgress() → float (0-1)          // Time remaining %
```

**Events:**

```csharp
OnBiteOccurred           // Fired when fish bites
OnTensionChanged(float)  // Fired every tension change
OnCatchComplete(result)  // Fired on success/failure
```

Behavior Calculation (simplified):

```csharp
switch (creature.behaviorType) {
    case StandardCatch:
        tension += baseResistance × 0.5
    case FastDarter:
        tension += Random(baseResistance × 0.3, max)
        // Changes every 0.3-0.8 seconds
    case SlowHeavy:
        tension += baseResistance × 1.2
    case Elusive:
        tension += baseResistance × 0.2
    case Aggressive:
        tension += Random(0, baseResistance × 1.5)
        // Spikes every 0.5-1.5 seconds
    case Cunning:
        tension += Random(0.2, 0.8) × baseResistance
        // Switches tactics every 0.4-1.0 seconds
}
```

### FishingUIController

**Primary Responsibilities:**

- Manages all UI panel visibility
- Updates UI values in real-time
- Captures player input (Space key, button clicks)
- Translates game events to visual feedback

**Key Methods:**

```csharp
OpenFishingUI()         // Show UI, disable movement
CloseFishingUI()        // Hide UI, enable movement
UpdatePhaseUI()         // Show correct phase panel
UpdateTensionDisplay()  // Update bar/color
ShowResultScreen()      // Final summary
AddCatchableToInventory() // Hook to your inventory system
```

**Event Listeners:**

```csharp
miniGameController.OnBiteOccurred        → HandleBiteOccurred()
miniGameController.OnTensionChanged      → HandleTensionChanged()
miniGameController.OnCatchComplete       → HandleCatchComplete()
```

### LakeFishingTrigger

**Primary Responsibilities:**

- Detect player entry/exit with physics triggers
- Show/hide interaction prompt
- Launch fishing session on E key
- Manage prompt lifecycle (fade in/out)

**Key Methods:**

```csharp
OnTriggerEnter2D()  // Player enters zone
OnTriggerExit2D()   // Player leaves zone
ShowPrompt()        // Display "[E] Fish" with fade-in
HidePrompt()        // Hide prompt with fade-out
StartFishingSession() // E key pressed
```

### CatchableSelector

**Static Utility** for random selection:

```csharp
SelectCatchable(zone)       // Weighted random from zone pool
GetCatchableByName(zone, name) // Debug helper
```

Uses weighted random sampling:

```
Probability = spawnWeight / sumOfAllWeights
CommonFish weight 50, BlueTrout weight 30, GoldenKoi weight 10
Total = 90
CommonFish = 50/90 = 55.5%
BlueTrout = 30/90 = 33.3%
GoldenKoi = 10/90 = 11.1%
```

---

## Behavior Types - Technical Deep Dive

### Standard

- **Personality**: Reliable, predictable
- **Tension Pattern**: Steady increase about 0.5× base resistance
- **Use For**: Tutorial fish, easy catch
- **Implementation**: Simple constant resistance

```csharp
tension += baseResistance × 0.5 × deltaTime
```

### Fast Darter

- **Personality**: Quick, nervous, erratic
- **Tension Pattern**: Random jumps between 0.3× and 1.0× base resistance
- **Timing**: Changes behavior every 0.3-0.8 seconds
- **Use For**: Medium difficulty, keeps player engaged
- **Implementation**: Randomized resistance with quick changes

```csharp
if (time >= nextChangeTime) {
    currentPull = Random.Range(baseResistance × 0.3f, baseResistance);
    nextChangeTime = time + Random.Range(0.3f, 0.8f);
}
tension += currentPull × deltaTime
```

### Slow Heavy

- **Personality**: Strong, massive, relentless
- **Tension Pattern**: Consistent strong pull at 1.2× base resistance
- **Use For**: High difficulty, requires steady hands
- **Implementation**: Always high resistance (no variation)

```csharp
tension += baseResistance × 1.2 × deltaTime
```

### Elusive

- **Personality**: Tries to escape, loose grip
- **Tension Pattern**: Weak resistance at 0.2× base (tries to get away)
- **Use For**: Medium-hard, requires active reeling
- **Implementation**: Very low resistance, player must keep pulling

```csharp
tension += baseResistance × 0.2 × deltaTime
// Player must actively reel to maintain tension
```

### Aggressive

- **Personality**: Territorial, fights hard periodically
- **Tension Pattern**: Alternates between calm (0.5×) and aggressive spikes (1.5×)
- **Timing**: Spikes every 0.5-1.5 seconds (simulates diving/rushing)
- **Use For**: Expert difficulty, high tension (pun intended)
- **Implementation**: Periodic tension spikes

```csharp
if (time >= nextChangeTime) {
    if (Random < 0.4f) // 40% chance of spike
        currentPull = baseResistance × 1.5f; // DIVE!
    else
        currentPull = baseResistance × 0.5f; // Calm
    nextChangeTime = time + Random.Range(0.5f, 1.5f);
}
tension += currentPull × deltaTime
```

### Cunning

- **Personality**: Tricks the player, unpredictable
- **Tension Pattern**: Random switching between pulling hard and going slack
- **Timing**: Changes every 0.4-1.0 seconds
- **Use For**: Legendary/expert difficulty
- **Implementation**: Highly variable, chaotic behavior

```csharp
if (time >= nextChangeTime) {
    currentPull = Random.value < 0.5f
        ? baseResistance × 0.8f  // Pulling
        : 0f;                     // Going slack (escape attempt!)
    nextChangeTime = time + Random.Range(0.4f, 1f);
}
tension += currentPull × deltaTime
```

---

## Extending the System

### Adding New Behavior Types

1. **Add enum value** in CatchableDefinition.cs:

```csharp
public enum FishBehaviorType {
    Standard,
    FastDarter,
    SlowHeavy,
    Elusive,
    Aggressive,
    Cunning,
    MyNewBehavior  // ← Add here
}
```

2. **Add case** in FishingMiniGameController.cs:

```csharp
case FishBehaviorType.MyNewBehavior:
    // Implement your logic
    if (creatureBehaviorTimer >= nextBehaviorChangeTime) {
        currentCreaturePullForce = /* your calculation */;
        nextBehaviorChangeTime = creatureBehaviorTimer + /* timing */;
    }
    break;
```

3. **Create catchable** using new behavior

### Adding Bait System

1. **Create BaitDefinition**:

```csharp
[CreateAssetMenu(menuName = "Fishing/Bait Definition")]
public class BaitDefinition : ScriptableObject {
    public string baitName;
    public int cost;
    public float attractiveness; // Multiplier for spawn weights
    public int uses; // Uses before consumed
    public Sprite icon;
}
```

2. **Modify FishingMiniGameController**:

```csharp
public void StartFishing(LakeZoneDefinition zone, BaitDefinition bait = null) {
    // Apply bait attractiveness multiplier to zone
    bakedZone = ModifyZoneByBait(zone, bait);
    selectedCatchable = CatchableSelector.SelectCatchable(bakedZone);
}
```

3. **Modify LakeFishingTrigger**:

```csharp
private BaitDefinition equippedBait;

public void EquipBait(BaitDefinition bait) {
    equippedBait = bait;
}

private void StartFishingSession() {
    miniGameController.StartFishing(lakeZone, equippedBait);
}
```

### Adding Fishing Rod Types

1. **Create RodDefinition**:

```csharp
[CreateAssetMenu(menuName = "Fishing/Rod Definition")]
public class RodDefinition : ScriptableObject {
    public string rodName;
    public float maxTensionCapacity; // Rod breaks at this tension
    public float reelSpeed; // Affects tension increase rate
    public float durability; // Can wear down
    public Sprite icon;
}
```

2. **Pass to MiniGameController**:

```csharp
public void StartFishing(LakeZoneDefinition zone, RodDefinition rod) {
    equippedRod = rod;
    // Adjust max tension based on rod
    fishingSettings.lineBreakerThreshold = rod.maxTensionCapacity;
}
```

### Adding Skill Progression

1. **Create FishingProfile**:

```csharp
[System.Serializable]
public class FishingProfile {
    public int level = 1;
    public float experience = 0f;
    public float experienceToNextLevel = 100f;
    public List<string> caughtSpecies = new();
    public int totalCaught = 0;
}
```

2. **Apply difficulty modifier**:

```csharp
float skillModifier = 1f - (fishingProfile.level * 0.05f); // 5% easier per level
zone.difficultyModifier *= skillModifier;
```

3. **Award XP on success**:

```csharp
private void HandleCatchComplete(FishingResultType result) {
    if (result == FishingResultType.Success) {
        float xpReward = currentCatchable.difficultyScore * 100f; // Harder fish = more XP
        fishingProfile.GainExperience(xpReward);
    }
}
```

---

## Tension Physics Deep Dive

### Tension Value Range: 0.0 - 1.0

```
0.0 ─────────────────────────────────────────────────────── 1.0
│     │      │      │      │      │               │      │
0.0  0.2    0.4    0.6    0.8    0.9             0.95   1.0
│                   │                              │       │
└─ Too Low      Success Zone                  Danger    BREAK
   (Escape)     (0.2-0.7)                    (0.6+)
```

### Tension Sources

**Increases:**

1. Creature resistance - automatic based on behavior

   ```
   tension += creaturePullForce × deltaTime
   ```

2. Player reeling - active input

   ```
   tension += reelTensionIncrement (0.05 per frame at 60 FPS ≈ 3 per second)
   ```

3. Bite event - sudden spike
   ```
   tension += biteTensionIncrease (0.3 typical)
   ```

**Decreases:**

1. Natural relaxation - when not reeling
   ```
   tension -= tensionDecayRate × deltaTime (0.05 typical)
   ```

### Tension Management Strategy (for player)

**Safe Zone (0.0 - 0.4): Green**

- Creature is loosely hooked
- Can reel freely without risk
- Might escape if stay too long

**Caution Zone (0.4 - 0.6): Yellow**

- Creature is solidly hooked
- Good tension balance
- Success range ends at 0.7

**Danger Zone (0.6 - 0.95): Red**

- High risk of line breaking
- Should release presses to let line relax
- Still reachable from 0.7

**Critical (> 0.95): Dark Red**

- Line breaks immediately
- Instant failure

### Difficulty by Creature

| Creature    | Difficulty | Tension Increase | Success Window | Catch Time | Best Strategy                          |
| ----------- | ---------- | ---------------- | -------------- | ---------- | -------------------------------------- |
| Common Fish | 0.3        | 0.08             | 0.6s           | 3-5s       | Reel steady, relax when high           |
| Blue Trout  | 0.6        | 0.12             | 0.45s          | 5-8s       | React quickly, handle erratic pulls    |
| Golden Koi  | 0.85       | 0.15             | 0.35s          | 8-12s      | Maintain steady tension, expert timing |

---

## Advanced Customization Examples

### Example 1: Make Easy Fishing Experience

**Goal**: New player-friendly fishing

**Settings**:

```csharp
// FishingSettings
successTensionMin = 0.1f;         // ← Lower requirement
successTensionMax = 0.8f;         // ← More forgiving
successWindow = 0.8f;             // ← Bigger reaction window
reelTensionIncrement = 0.08f;     // ← Slower tension build
```

**Catchables**:

```csharp
// Create easy fish
difficultyScore = 0.2f;           // ← Very easy
biteDelayMin = 0.5f;              // ← Quick bites
biteDelayMax = 1.5f;
behaviorType = Standard;          // ← Predictable
creaturePullStrength = 0.1f;      // ← Weak resistance
```

**Zone**:

```csharp
difficultyModifier = 0.8f;        // ← Easier than normal
```

### Example 2: Expert/Hardcore Fishing

**Goal**: Challenging but fair experience

**Settings**:

```csharp
successTensionMin = 0.35f;        // ← Narrow band
successTensionMax = 0.65f;        // ← Must be precise
successWindow = 0.25f;            // ← Quick reaction
reelTensionMax = 0.6f;            // ← Can't pull too hard
tensionDecayRate = 0.02f;         // ← Slow recovery
```

**Catchables**:

```csharp
// All expert fish
difficultyScore = 0.85f;
biteDelayMin = 2.5f;
biteDelayMax = 5f;
behaviorType = Aggressive + Cunning;
creaturePullStrength = 0.7f;
canFakeOut = true;
canDive = true;
diveIntensity = 0.8f;
```

**Zone**:

```csharp
difficultyModifier = 1.3f;        // ← Harder overall
```

### Example 3: Sushi Restaurant Quality Control

**Goal**: Specialized fishing for sushi ingredients

**Catchables**:

```csharp
// Tuna - sleek and strong
"Bluefin Tuna"
  behaviorType: SlowHeavy
  creaturePullStrength: 0.9f
  sellPrice: 500

// Salmon - moderate
"Wild Salmon"
  behaviorType: Standard
  creaturePullStrength: 0.4f
  sellPrice: 200

// Squid - weird and tricky
"Giant Squid"
  behaviorType: Cunning
  canDive: true
  sellPrice: 300
```

**Zone**:

```csharp
"Sushi Bar Special Zone"
  catchablePool: [Bluefin, Salmon, Squid]
  difficultyModifier: 1.1f
```

---

## Performance Considerations

### Memory Usage

- Each CatchableDefinition: ~500 bytes (minimal)
- UI Elements: Reused each session, ~5MB total
- FishingSettings: Singleton, ~2KB

### CPU Usage

- Update loop: ~0.5ms per frame (physics calculations)
- State machine checks: ~0.1ms per frame
- UI updates: ~0.5ms per frame (tension bar, labels)
- **Total**: ~1.1ms per frame (negligible)

### Optimization Tips

1. **Reuse UI panels**: System reuses same panels for each phase
2. **Lazy loading**: Catchables loaded from Resources on-demand
3. **No animations**: Pure UI Toolkit, no animation overhead
4. **Single state machine**: Efficient phase transitions

---

## Troubleshooting Guide

### Problem: "Fish caught" not added to inventory

**Solution**: Implement `AddCatchableToInventory()` in FishingUIController

```csharp
private void AddCatchableToInventory(CatchableDefinition catchable) {
    // Replace with your inventory API
    // Example:
    if (inventoryController != null) {
        inventoryController.AddItem(catchable.catchableName, 1);
    }
}
```

### Problem: Player can move during fishing

**Cause**: `CharacterController2D` not being disabled
**Solution**: In FishingUIController, ensure:

```csharp
private void DisablePlayerMovement() {
    CharacterController2D playerController = FindObjectOfType<CharacterController2D>();
    if (playerController != null) {
        playerController.enabled = false;
    }
}
```

### Problem: Tension bar not changing color

**Solution**: Check USS file, ensure selector names match:

```uss
.tension-bar {
    background-color: rgb(50, 200, 50); /* Green base */
}
```

### Problem: Creatures always same type

**Cause**: CatchableSelector pool is empty or weights all zero
**Solution**: Verify LakeZoneDefinition:

```
catchablePool[0].enabled = true ✓
catchablePool[0].spawnWeight > 0 ✓
catchablePool[0].catchable != null ✓
```

### Problem: Prompt never appears

**Cause**: LakeFishingTrigger not finding UIDocument
**Solution**:

1. Check LakeFishingTrigger has LakeFishingTrigger script
2. Verify fishing-panel.uxml is assigned to UIDocument
3. Check `<VisualElement name="fishing-prompt">` exists in UXML

---

**End of Architecture Guide**

For quick setup, see FISHING_SYSTEM_SETUP.md
