# Chicken Egg System - Troubleshooting Guide

## Issue: Eggs Are Not Spawning

### Symptom

You reach the egg laying time but no egg appears near the chicken.

### Diagnosis Steps

**Step 1: Check Debug Output**
Add this to ChickenController.cs at the beginning of Update():

```csharp
Debug.Log($"[Chicken Debug] TimeNormalized: {dayNightCycle.TimeNormalized}, " +
          $"CurrentHour: {dayNightCycle.TimeNormalized * 24f:F2}, " +
          $"HasLaidEggToday: {hasLaidEggToday}");
```

Look at console output - verify:

- TimeNormalized is increasing (0 → 1)
- CurrentHour displays correctly
- At laying time, hasLaidEggToday changes to true

**Step 2: Verify Prefab Path**
In ChickenController Inspector, you should see either:

- Egg Prefab Path assigned manually (assigned in Inspector), OR
- Empty and trying to load from Resources/Prefabs/Items/Egg

If getting error "Egg prefab not found in Resources":

- Create folder structure: Assets/Resources/Prefabs/Items/
- Move Egg.prefab there
- Restart scene

**Step 3: Verify Item Definition**

```csharp
[SerializeField] private ItemDefinition eggItem;
```

This should be assigned to your Egg ScriptableObject in Inspector.
If null, you'll see error in console.

### Solutions

**Solution A: DayNightCycleNice2D Not Found**

```csharp
// In ChickenController Start():
if (dayNightCycle == null)
{
    dayNightCycle = FindFirstObjectByType<DayNightCycleNice2D>();
    if (dayNightCycle == null)
        Debug.LogError("[ChickenController] DayNightCycleNice2D not found in scene!");
}
```

**Solution B: Egg Prefab Loading Issue**
Check the exact path your egg prefab is at:

```csharp
// Debugging code to add to Start()
GameObject testLoad = Resources.Load<GameObject>("Prefabs/Items/Egg");
if (testLoad == null)
    Debug.LogError("[Chicken] Cannot load Egg from Resources/Prefabs/Items/Egg");
else
    Debug.Log("[Chicken] Successfully loaded Egg prefab!");
```

**Solution C: Egg Item Definition Missing**

```csharp
// Add this check to TryLayEgg()
if (eggItem == null)
{
    Debug.LogError("[Chicken] Egg ItemDefinition not assigned in inspector!");
    return;
}
```

**Solution D: Wrong Laying Time**
Your game might use different time scale.
In Start(), add:

```csharp
Debug.Log("[Chicken] Game will be: 0=midnight, 0.25=6AM, 0.5=noon, 0.75=6PM, 1=midnight");
float testTime = dayNightCycle.TimeNormalized;
Debug.Log($"[Chicken] Current normalized time: {testTime}");
Debug.Log($"[Chicken] Current game hour: {testTime * 24f}");
```

---

## Issue: Eggs Not Picking Up / Not Moving to Player

### Symptom

Egg spawns but doesn't move toward player or get added to inventory.

### Diagnosis Steps

**Step 1: Check PickupComponent Assignment**
Egg prefab must have PickupComponent script attached.
In egg prefab Inspector:

- Component should be listed: "Pickup Component"
- If missing: Add Component > PickupComponent

**Step 2: Verify Player Tag**
PickupComponent looks for GameObject with tag "Player":

```csharp
// In your PlayerInfo:
// Make sure your player GameObject has tag "Player" set
```

To verify:

1. Select Player GameObject
2. In Inspector top-right, see "Tag" dropdown
3. Should be set to "Player"

**Step 3: Check Collider Settings**
Egg prefab BoxCollider2D must be trigger:

- Is Trigger: ✓ (checked)
- Collider size appropriate (e.g., 0.3 × 0.3)

If collider is too small, player won't trigger it.

**Step 4: Verify Inventory Found**
PickupComponent looks for InventoryController:

```csharp
// In PickupComponent Awake()
inv = FindFirstObjectByType<InventoryController>();
if (inv == null)
    Debug.LogError("[PickupComponent] InventoryController not found in scene!");
```

### Solutions

**Solution A: PickupComponent Missing**

```csharp
// Check egg prefab has this script
// Add Component > PickupComponent if missing
```

**Solution B: Player Not Found**

```csharp
// In PickupComponent, verify player GameObject tag:
var playerGO = GameObject.FindGameObjectWithTag("Player");
if (playerGO == null)
    Debug.LogError("[PickupComponent] Player with tag 'Player' not found!");
else
    Debug.Log($"[PickupComponent] Found player: {playerGO.name}");
```

**Solution C: Inventory Controller Not Found**

```csharp
// Make sure InventoryController exists in scene
inv = FindFirstObjectByType<InventoryController>();
if (inv == null)
    Debug.LogError("[PickupComponent] InventoryController not in scene!");
```

**Solution D: Egg Item Not Set**
In ChickenController.TryLayEgg():

```csharp
PickupComponent pickup = eggGO.GetComponent<PickupComponent>();
if (pickup != null)
{
    pickup.Set(eggItem, eggCount);
    Debug.Log($"[Chicken] Set egg item: {eggItem.displayName}, count: {eggCount}");
}
```

**Solution E: Inventory Full**
PickupComponent checks if inventory has space:

```csharp
bool added = inv.TryAdd(item, count);
if (!added)
    Debug.Log("[PickupComponent] Inventory full - egg not collected");
```

→ Clear some inventory space

**Solution F: Pickup Distance Too Small**
In PickupComponent Inspector:

- Pickup Distance: Should be 1.5+ (distance from player to detect)
- Collect Distance: Should be 0.1 (distance to actually collect)

---

## Issue: Eggs Spawning Multiple Times Per Day

### Symptom

Chicken lays more than 1 egg per day.

### Diagnosis

Usually caused by:

1. Multiple ChickenController components on same chicken
2. OnDayAdvanced event not firing
3. hasLaidEggToday not persisting

### Solutions

**Solution A: Check for Duplicate Components**

1. Select Chicken GameObject
2. In Inspector, look for "ChickenController" in components
3. If listed twice, delete one

**Solution B: Verify OnDayAdvanced Event**
Add to ChickenController:

```csharp
private void Start()
{
    DayNightCycleNice2D.OnDayAdvanced += OnNewDay;
    Debug.Log("[Chicken] Subscribed to OnDayAdvanced event");
}

private void OnNewDay()
{
    Debug.Log("[Chicken] OnNewDay called - resetting hasLaidEggToday");
    hasLaidEggToday = false;
    lastCheckedTime = -1f;
}
```

If "OnNewDay called" never prints, event isn't firing.
Check DayNightCycleNice2D:

```csharp
// In DayNightCycleNice2D, find where it broadcasts:
public static event Action OnDayAdvanced;

// It should call this somewhere:
OnDayAdvanced?.Invoke();
```

**Solution C: Add Safety Check**
Modify TryLayEgg() to prevent double-laying:

```csharp
private void TryLayEgg()
{
    if (hasLaidEggToday)
    {
        Debug.Log("[Chicken] Already laid egg today, skipping");
        return;
    }

    // ... rest of code ...
    hasLaidEggToday = true;
    Debug.Log("[Chicken] Laid egg, hasLaidEggToday = true");
}
```

---

## Issue: Eggs Spawning At Wrong Location

### Symptom

Eggs appear far from chicken or in random/unwanted locations.

### Solutions

**Solution A: Adjust Spawn Offset**
In ChickenController Inspector:

```
Spawn Offset: Controls egg spawn location relative to chicken
├─ X: 0.5 = right of chicken
├─ Y: 0.3 = above chicken
└─ Z: 0 = same depth
```

Example offsets:

```
Behind chicken: (-0.3, 0, 0)
Below chicken: (0, -0.5, 0)
Slightly forward: (0.3, 0.2, 0)
```

**Solution B: Adjust Spawn Random Radius**

```
Spawn Random Radius controls scatter distance
Low value (0.1): Eggs spawn very close to chicken
High value (1.0): Eggs spawn up to 1 unit away randomly
Good default: 0.3
```

**Solution C: Check for Physics**
If Egg GameObject has Rigidbody2D:

```csharp
Rigidbody2D rb = eggGO.GetComponent<Rigidbody2D>();
if (rb != null)
{
    rb.isKinematic = true;  // Prevent physics interference
    rb.gravityScale = 0;
}
```

---

## Issue: Inventory Says It's Full But It Isn't

### Symptom

Egg doesn't get picked up, error says inventory full.

### Solutions

**Solution A: Check Inventory Size**
In InventoryController Inspector:

```
Inventory Size should be reasonable (default 36)
Max Stack for Egg should be high enough
```

**Solution B: Debug Inventory Status**
Add to PickupComponent:

```csharp
bool added = inv.TryAdd(item, count);
if (!added)
{
    Debug.Log($"[PickupComponent] Failed to add {item.displayName}. " +
              "Inventory state:");
    // Count occupied slots
    int occupied = 0;
    for (int i = 0; i < inv.SlotsData.Length; i++)
    {
        if (inv.SlotsData[i].item != null)
            occupied++;
    }
    Debug.Log($"[PickupComponent] Occupied slots: {occupied}/{inv.SlotsData.Length}");
}
```

**Solution C: Clear Some Inventory**
Drop some items to free up space, then try again.

---

## Issue: Time Not Advancing

### Symptom

Game time stays at same value, eggs never lay.

### Solutions

**Solution A: Check dayLengthSeconds**
In DayNightCycleNice2D Inspector:

```
dayLengthSeconds controls day speed
Too high (300+): Day passes very slowly
Too low (10): Day passes instantly
Good testing value: 60 seconds = full day
```

**Solution B: Verify Script Is Running**
Add to DayNightCycleNice2D:

```csharp
private void Update()
{
    Debug.Log($"[DayNight] TimeNormalized: {TimeNormalized:F3}");
    // ... rest of code ...
}
```

Check console - should see values increasing each frame.

**Solution C: Check if Scene is Playing**

- Make sure you pressed Play button
- Make sure you're not paused (press Space to unpause)

---

## Issue: Egg Prefab Errors

### Symptom

Console shows errors relating to egg prefab loading/instantiation.

### Errors & Fixes

**Error: "The prefab you want to instantiate is null"**

```
Cause: eggPrefabPath is null and Resources path is wrong
Fix: Copy egg to correct folder:
     Assets/Resources/Prefabs/Items/Egg.prefab
```

**Error: "PickupComponent not found on egg"**

```
Cause: Egg prefab missing PickupComponent
Fix: Edit Egg prefab
     Add Component > PickupComponent
```

**Error: "player GameObject with tag 'Player' was not found"**

```
Cause: Player doesn't have tag "Player"
Fix: Select Player GameObject
     Set Tag dropdown to "Player"
```

**Error: "InventoryController not found"**

```
Cause: InventoryController not in scene
Fix: Make sure your inventory UI GameObject
     has InventoryController component
```

---

## Issue: Egg Disappears Immediately

### Symptom

Egg appears but vanishes after a second or two.

### Solutions

**Solution A: Increase TTL**
In egg prefab PickupComponent:

```
TTL (Time To Live): 30 seconds (default)
If eggs disappear too fast, increase to 60+
```

**Solution B: Check if Being Destroyed Elsewhere**
Search your code for:

```csharp
Destroy(eggGO)
DestroyImmediate(eggGO)
```

Make sure nothing else is destroying eggs.

**Solution C: Check Collision**
If egg has Rigidbody2D not set to kinematic:

```csharp
Rigidbody2D rb = eggGO.GetComponent<Rigidbody2D>();
if (rb != null && !rb.isKinematic)
{
    // Physics might destroy it
    rb.isKinematic = true;
}
```

---

## Performance: Eggs Impacting FPS

### Symptom

Game slows down when many eggs are on screen.

### Solutions

**Solution A: Reduce Spawn Random Radius**
Instead of spawning far away, spawn close:

```
Spawn Random Radius: 0.1 (not 1.0)
Limits how many eggs can be visible at once
```

**Solution B: Reduce TTL**
Eggs disappear faster if not picked up:

```
TTL: 15 seconds (instead of 30)
Eggs cleaned up quicker
```

**Solution C: Limit Max Eggs**
In ChickenController:

```csharp
private int maxActiveEggs = 3;

private int CountActiveEggs()
{
    return FindObjectsByType<PickupComponent>(
        FindObjectsSortMode.None
    ).Length;
}

private void TryLayEgg()
{
    if (CountActiveEggs() >= maxActiveEggs)
    {
        Debug.Log("[Chicken] Too many eggs on ground, skipping spawn");
        return;
    }
    // ... spawn egg ...
}
```

---

## Quick Debug Checklist

Before asking for help, add these to see what's happening:

```csharp
// In ChickenController.Update()
if (gameObject.name.Contains("Chicken")) // Only your test chicken
{
    float hour = dayNightCycle.TimeNormalized * 24f;
    Debug.Log($"Hour: {hour:F1} | HasLaid: {hasLaidEggToday} | " +
              $"Item: {eggItem?.displayName ?? "NULL"}");
}
```

```csharp
// In PickupComponent.Update()
float dist = Vector3.Distance(transform.position, player.position);
if (dist < pickupDistance + 0.5f)
{
    Debug.Log($"Egg distance to player: {dist:F2} " +
              $"(detect: {pickupDistance}, collect: {collectDistance})");
}
```

Check console output - this tells you exactly what's happening frame by frame!

---

## Still Having Issues?

Check these in order:

1. Review CHICKEN_EGG_SYSTEM_SETUP.md - Make sure you followed every step
2. Check console for errors - Copy exact error message
3. Compare your setup with CHICKEN_EGG_SYSTEM_VISUALS.md architecture
4. Use the debug code snippets above to trace execution
5. Verify all Inspector assignments match expected types

Most issues are setup mistakes rather than code bugs - double-check your configuration!
