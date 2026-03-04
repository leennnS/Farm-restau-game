# Chicken Egg System - Optional Enhancements

## Enhancement 1: Add Pickup Toast Notification

Show "+1 Egg" message when eggs are picked up.

**File to modify:** `Assets/Scripts/Items/PickupComponent.cs`

Find the line that destroys the egg:

```csharp
if (distance <= collectDistance)
{
    bool added = inv.TryAdd(item, count);

    if (added)
        Destroy(gameObject);
```

Replace with:

```csharp
if (distance <= collectDistance)
{
    bool added = inv.TryAdd(item, count);

    if (added)
    {
        // Show pickup notification
        PickupToastUIToolkit pickupToast = FindFirstObjectByType<PickupToastUIToolkit>();
        if (pickupToast != null && item != null)
            pickupToast.Show($"+{count} {item.displayName}");

        Destroy(gameObject);
    }
    else
        Debug.Log("Inventory full (could not add).");
}
```

---

## Enhancement 2: Add Visual/Sound Effect on Egg Laying

Make the chicken do something when laying an egg.

**Create new file:** `Assets/Scripts/NPCs/ChickenLayingEffect.cs`

```csharp
using UnityEngine;

public class ChickenLayingEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem layingParticleFX;
    [SerializeField] private AudioClip layingSound;
    [SerializeField] private float effectDuration = 1f;

    public void PlayLayingEffect()
    {
        // Play particle effect
        if (layingParticleFX != null)
        {
            layingParticleFX.Play();
        }

        // Play sound
        if (layingSound != null)
        {
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.PlayOneShot(layingSound);
        }

        // Optional: Add animation
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("Lay"); // Requires "Lay" trigger in animator
        }
    }
}
```

Then add to `ChickenController.cs` in the `TryLayEgg()` method:

```csharp
private void TryLayEgg()
{
    if (hasLaidEggToday || eggPrefabPath == null)
        return;

    // Play effect before spawning
    ChickenLayingEffect effect = GetComponent<ChickenLayingEffect>();
    if (effect != null)
        effect.PlayLayingEffect();

    // ... rest of egg spawning code ...
}
```

---

## Enhancement 3: Chicken Movement & Animation

Make chickens wander around the farm.

**Create new file:** `Assets/Scripts/NPCs/ChickenWanderer.cs`

```csharp
using UnityEngine;

public class ChickenWanderer : MonoBehaviour
{
    [SerializeField] private float wanderSpeed = 1.5f;
    [SerializeField] private float wanderRadius = 3f;
    [SerializeField] private float changeDirectionTime = 5f;

    private Vector3 wanderCenter;
    private Vector3 wanderTarget;
    private float timeUntilDirectionChange;
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        wanderCenter = transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();
        ChooseNewTarget();
    }

    private void Update()
    {
        // Move toward target
        Vector3 direction = (wanderTarget - transform.position).normalized;
        transform.position += direction * wanderSpeed * Time.deltaTime;

        // Flip sprite based on direction
        if (spriteRenderer != null && direction.x != 0)
        {
            spriteRenderer.flipX = direction.x < 0;
        }

        // Pick new target if reached current one
        timeUntilDirectionChange -= Time.deltaTime;
        if (timeUntilDirectionChange <= 0)
        {
            ChooseNewTarget();
        }
    }

    private void ChooseNewTarget()
    {
        Vector2 randomOffset = Random.insideUnitCircle * wanderRadius;
        wanderTarget = wanderCenter + new Vector3(randomOffset.x, randomOffset.y, 0);
        timeUntilDirectionChange = changeDirectionTime;
    }
}
```

**Add to Chicken GameObject:**

1. Add Component > ChickenWanderer
2. Set Wander Speed: 1.5
3. Set Wander Radius: 3
4. Set Change Direction Time: 5

---

## Enhancement 4: Multiple Eggs Per Chicken

Allow chickens to lay more than one egg per day.

**In ChickenController.cs**, modify to support multiple laying times:

```csharp
[System.Serializable]
public struct EggLayingSchedule
{
    public float layingTime;
    public float timeWindow;
}

[SerializeField] private EggLayingSchedule[] layingSchedules;
private bool[] hasLaidEgg;

private void Start()
{
    hasLaidEgg = new bool[layingSchedules.Length];
    DayNightCycleNice2D.OnDayAdvanced += OnNewDay;
}

private void OnNewDay()
{
    for (int i = 0; i < hasLaidEgg.Length; i++)
        hasLaidEgg[i] = false;
}

private void Update()
{
    if (dayNightCycle == null || eggItem == null)
        return;

    float currentTimeNormalized = dayNightCycle.TimeNormalized;
    float currentHour = currentTimeNormalized * 24f;

    // Check all laying schedules
    for (int i = 0; i < layingSchedules.Length; i++)
    {
        if (!hasLaidEgg[i])
        {
            float windowStart = layingSchedules[i].layingTime;
            float windowEnd = layingSchedules[i].layingTime + layingSchedules[i].timeWindow;

            if (windowEnd >= 24f)
            {
                windowEnd -= 24f;
                if (currentHour >= windowStart || currentHour < windowEnd)
                    TryLayEgg(i);
            }
            else if (currentHour >= windowStart && currentHour < windowEnd)
            {
                TryLayEgg(i);
            }
        }
    }
}

private void TryLayEgg(int scheduleIndex)
{
    // ... same spawning code ...
    hasLaidEgg[scheduleIndex] = true;
}
```

**In Inspector:**

1. Expand "Laying Schedules" array
2. Add multiple entries
3. Set different times for each

Example:

```
Laying Schedules: Array Size 2
  [0] Laying Time: 8, Time Window: 1
  [1] Laying Time: 16, Time Window: 1
```

---

## Enhancement 5: Chicken Happiness/Health

Track chicken health to affect egg production.

**Create file:** `Assets/Scripts/NPCs/ChickenHappiness.cs`

```csharp
using UnityEngine;

public class ChickenHappiness : MonoBehaviour
{
    [SerializeField] private float maxHappiness = 100f;
    private float currentHappiness;

    [SerializeField] private float happinessDecayPerDay = 5f;
    [SerializeField] private float happinessGainOnFeed = 10f;

    public float CurrentHappiness => currentHappiness;
    public float MaxHappiness => maxHappiness;

    private void Start()
    {
        currentHappiness = maxHappiness;
        DayNightCycleNice2D.OnDayAdvanced += OnNewDay;
    }

    private void OnDestroy()
    {
        DayNightCycleNice2D.OnDayAdvanced -= OnNewDay;
    }

    private void OnNewDay()
    {
        // Chickens get less happy each day without care
        AdjustHappiness(-happinessDecayPerDay);
    }

    public void Feed()
    {
        AdjustHappiness(happinessGainOnFeed);
        Debug.Log($"[Chicken] Happy! Happiness: {currentHappiness}/{maxHappiness}");
    }

    private void AdjustHappiness(float amount)
    {
        currentHappiness = Mathf.Clamp(currentHappiness + amount, 0, maxHappiness);
    }

    public bool WillLayEgg()
    {
        // Only lay eggs if happy enough (>50%)
        return currentHappiness >= (maxHappiness * 0.5f);
    }
}
```

Then modify ChickenController to check happiness:

```csharp
private void TryLayEgg()
{
    ChickenHappiness happiness = GetComponent<ChickenHappiness>();
    if (happiness != null && !happiness.WillLayEgg())
    {
        Debug.Log("[Chicken] Too sad to lay eggs!");
        return;
    }

    // ... rest of laying code ...
}
```

---

## Enhancement 6: Egg Quality Levels

Eggs can be Normal, Large, or Golden based on chicken conditions.

**Create file:** `Assets/Scripts/Items/EggQuality.cs`

```csharp
using UnityEngine;

public enum EggQuality { Normal, Large, Golden }

public class EggQuality : MonoBehaviour
{
    [SerializeField] private EggQuality quality = EggQuality.Normal;
    [SerializeField] private Sprite normalIcon;
    [SerializeField] private Sprite largeIcon;
    [SerializeField] private Sprite goldenIcon;

    public EggQuality Quality => quality;

    public int GetSellPrice()
    {
        return quality switch
        {
            EggQuality.Normal => 75,
            EggQuality.Large => 150,
            EggQuality.Golden => 500,
            _ => 75
        };
    }

    public Sprite GetIcon()
    {
        return quality switch
        {
            EggQuality.Normal => normalIcon,
            EggQuality.Large => largeIcon,
            EggQuality.Golden => goldenIcon,
            _ => normalIcon
        };
    }

    public void SetQuality(EggQuality newQuality)
    {
        quality = newQuality;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.sprite = GetIcon();
    }
}
```

Modify ChickenController to assign quality:

```csharp
private void TryLayEgg()
{
    // ... spawning code ...

    EggQuality eggQuality = eggGO.GetComponent<EggQuality>();
    if (eggQuality != null)
    {
        // Random quality based on chicken happiness
        float rand = Random.value;
        if (rand < 0.7f)
            eggQuality.SetQuality(EggQuality.Normal);
        else if (rand < 0.95f)
            eggQuality.SetQuality(EggQuality.Large);
        else
            eggQuality.SetQuality(EggQuality.Golden);
    }
}
```

---

## Enhancement 7: Egg Despawn Timer Visual

Show particle effect or glow as egg is about to disappear.

Modify `PickupComponent.cs`:

```csharp
[SerializeField] private Color despawnStartColor = Color.white;
[SerializeField] private Color despawnEndColor = Color.gray;
[SerializeField] private float despawnWarningTime = 5f;

private void Update()
{
    // ... existing pickup code ...

    // Visual warning when about to disappear
    if (ttl < despawnWarningTime)
    {
        float alpha = ttl / despawnWarningTime;
        Color newColor = despawnStartColor;
        newColor.a = alpha;

        if (sr != null)
            sr.color = newColor;
    }

    ttl -= Time.deltaTime;
    if (ttl <= 0f)
    {
        Destroy(gameObject);
        return;
    }
}
```

---

## Enhancement 8: Chicken Preferences

Different colored chickens lay different colored eggs with different uses.

```csharp
public enum ChickenBreed { WhiteHen, BrownHen, GoldenHen }

[SerializeField] private ChickenBreed breed = ChickenBreed.WhiteHen;

public ItemDefinition GetEggItem()
{
    return breed switch
    {
        ChickenBreed.WhiteHen => Resources.Load<ItemDefinition>("Items/WhiteEgg"),
        ChickenBreed.BrownHen => Resources.Load<ItemDefinition>("Items/BrownEgg"),
        ChickenBreed.GoldenHen => Resources.Load<ItemDefinition>("Items/GoldenEgg"),
        _ => eggItem
    };
}
```

---

## Summary of Enhancements

| Enhancement           | Complexity     | Time to Implement |
| --------------------- | -------------- | ----------------- |
| 1. Toast Notification | ⭐ Very Easy   | 5 min             |
| 2. Laying Effects     | ⭐ Easy        | 10 min            |
| 3. Wandering AI       | ⭐⭐ Easy      | 15 min            |
| 4. Multiple Eggs      | ⭐⭐ Moderate  | 20 min            |
| 5. Happiness System   | ⭐⭐ Moderate  | 25 min            |
| 6. Egg Quality Levels | ⭐⭐⭐ Complex | 30 min            |
| 7. Despawn Visual     | ⭐ Easy        | 10 min            |
| 8. Chicken Breeds     | ⭐⭐⭐ Complex | 30 min            |

Start with **Enhancement 1 (Toast)** then work your way up!
