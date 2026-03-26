using UnityEngine;
using System;

public class ChickenController : MonoBehaviour
{
    [Header("Egg Laying Settings")]
    [SerializeField] private ItemDefinition eggItem;
    [SerializeField] private int eggCount = 1;
    [SerializeField] private float eggLayingTime = 8f; // 8 AM
    [SerializeField] private float eggLayingTimeWindow = 1f; // 8-9 AM

    [Header("Egg Prefab")]
    [SerializeField] private GameObject eggPrefabPath;

    [Header("References")]
    [SerializeField] private DayNightCycleNice2D dayNightCycle;

    [Header("Spawn Settings")]
    [SerializeField] private float extraSideSpacing = 0.2f;   // extra distance outside chicken width
    [SerializeField] private float verticalOffset = -0.1f;    // slight downward offset so egg looks on ground
    [SerializeField] private float spawnRandomRadius = 0.05f; // small random variation
    [SerializeField] private float eggTimeToLive = 120f;

    private bool hasLaidEggToday = false;
    private float lastCheckedTime = -1f;
    private bool _warnedMissingCycle;

    private SpriteRenderer chickenSpriteRenderer;

    private void Start()
    {
        Debug.Log("[Chicken] Starting up...");

        chickenSpriteRenderer = GetComponent<SpriteRenderer>();
        if (chickenSpriteRenderer == null)
        {
            Debug.LogWarning("[Chicken] No SpriteRenderer found on chicken. Dynamic egg spacing will fall back to transform position.");
        }

        if (dayNightCycle == null)
        {
            // First try the public static accessor (preferred and more reliable)
            dayNightCycle = DayNightCycleNice2D.Instance;

            if (dayNightCycle == null)
            {
                // Fallback to FindFirstObjectByType if static accessor not available
                dayNightCycle = FindFirstObjectByType<DayNightCycleNice2D>();
            }

            if (dayNightCycle == null)
                Debug.LogError("[Chicken] ERROR: DayNightCycleNice2D not found in scene!");
            else
                Debug.Log("[Chicken] Found DayNightCycleNice2D");
        }
        else
        {
            Debug.Log("[Chicken] DayNightCycleNice2D was pre-assigned");
        }

        if (eggPrefabPath == null)
        {
            eggPrefabPath = Resources.Load<GameObject>("Prefabs/Items/Egg");
            if (eggPrefabPath == null)
                Debug.LogError("[Chicken] ERROR: Egg prefab not found! Path: Resources/Prefabs/Items/Egg");
            else
                Debug.Log("[Chicken] Loaded Egg prefab from Resources");
        }
        else
        {
            Debug.Log("[Chicken] Egg prefab was pre-assigned in Inspector");
        }

        if (eggItem == null)
            Debug.LogError("[Chicken] ERROR: Egg Item Definition not assigned in Inspector!");
        else
            Debug.Log($"[Chicken] Egg item assigned: {eggItem.displayName}");

        DayNightCycleNice2D.OnDayAdvanced += OnNewDay;
        Debug.Log("[Chicken] Subscribed to OnDayAdvanced event");
    }

    private void OnDestroy()
    {
        DayNightCycleNice2D.OnDayAdvanced -= OnNewDay;
    }

    private void OnNewDay()
    {
        hasLaidEggToday = false;
        lastCheckedTime = -1f;
        Debug.Log("[Chicken] New day started. Egg laying reset.");
    }

    private void Update()
    {
        if (dayNightCycle == null)
        {
            // Try to rebind each frame until we find it
            dayNightCycle = DayNightCycleNice2D.Instance;
            if (dayNightCycle == null)
                dayNightCycle = FindFirstObjectByType<DayNightCycleNice2D>();

            if (dayNightCycle == null)
            {
                if (!_warnedMissingCycle)
                {
                    Debug.LogError("[Chicken] ERROR: dayNightCycle is NULL! Cannot check time.");
                    _warnedMissingCycle = true;
                }
                return;
            }
            else
            {
                _warnedMissingCycle = false;
                Debug.Log("[Chicken] Rebound DayNightCycleNice2D at runtime");
            }
        }

        if (eggItem == null)
        {
            Debug.LogError("[Chicken] ERROR: eggItem is NULL! Assign it in Inspector.");
            return;
        }

        if (hasLaidEggToday)
            return;

        float currentTimeNormalized = dayNightCycle.TimeNormalized;
        float currentHour = currentTimeNormalized * 24f;

        float windowStart = eggLayingTime;
        float windowEnd = eggLayingTime + eggLayingTimeWindow;

        bool inWindow = false;

        if (windowEnd >= 24f)
        {
            windowEnd -= 24f;
            if (currentHour >= windowStart || currentHour < windowEnd)
                inWindow = true;
        }
        else if (currentHour >= windowStart && currentHour < windowEnd)
        {
            inWindow = true;
        }

        if (inWindow)
        {
            Debug.Log($"[Chicken] IN LAYING WINDOW! Current hour: {currentHour:F1}, Window: {windowStart}-{windowStart + eggLayingTimeWindow}");
            TryLayEgg();
        }

        lastCheckedTime = currentTimeNormalized;
    }

    private void TryLayEgg()
    {
        Debug.Log("[Chicken] TryLayEgg() called");

        if (hasLaidEggToday)
        {
            Debug.LogWarning("[Chicken] Already laid egg today, skipping");
            return;
        }

        if (eggPrefabPath == null)
        {
            Debug.LogError("[Chicken] ERROR: Egg prefab is NULL! Cannot spawn egg.");
            return;
        }

        Vector3 spawnPos = GetEggSpawnPosition();

        Debug.Log($"[Chicken] Spawning egg at position: {spawnPos}");

        GameObject eggGO = Instantiate(eggPrefabPath, spawnPos, Quaternion.identity);
        if (eggGO == null)
        {
            Debug.LogError("[Chicken] ERROR: Failed to instantiate egg prefab!");
            return;
        }

        Debug.Log("[Chicken] Egg instantiated successfully");

        Rigidbody2D rb = eggGO.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            Debug.Log("[Chicken] Rigidbody physics disabled (kinematic)");
        }
        else
        {
            Debug.Log("[Chicken] No Rigidbody2D on egg (okay if using trigger pickup only)");
        }

        Collider2D collider = eggGO.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;
            Debug.Log($"[Chicken] Collider set to trigger (type: {collider.GetType().Name})");
        }
        else
        {
            Debug.LogError("[Chicken] ERROR: No collider on egg prefab! Pickup won't work.");
        }

        PickupComponent pickup = eggGO.GetComponent<PickupComponent>();
        if (pickup != null)
        {
            pickup.Set(eggItem, eggCount);
            pickup.SetTimeToLive(eggTimeToLive);
            Debug.Log($"[Chicken] ✓ Egg spawned successfully at {spawnPos}! Item: {eggItem.displayName}, Count: {eggCount}, TTL: {eggTimeToLive}s");
        }
        else
        {
            Debug.LogError("[Chicken] ERROR: Egg prefab missing PickupComponent!");
            Destroy(eggGO);
            return;
        }

        MatchEggSortingToChicken(eggGO);

        hasLaidEggToday = true;
        Debug.Log("[Chicken] hasLaidEggToday set to true");
    }

    private Vector3 GetEggSpawnPosition()
    {
        Vector3 spawnPos = transform.position;

        if (chickenSpriteRenderer != null)
        {
            // Half width of current chicken in world space
            float sideDistance = chickenSpriteRenderer.bounds.extents.x + extraSideSpacing;

            // If chicken flips left/right, egg appears on that side
            float direction = transform.localScale.x >= 0f ? 1f : -1f;

            spawnPos += new Vector3(sideDistance * direction, verticalOffset, 0f);
        }
        else
        {
            // Fallback if no SpriteRenderer
            spawnPos += new Vector3(0.5f, verticalOffset, 0f);
        }

        // Small random variation
        spawnPos += new Vector3(
            UnityEngine.Random.Range(-spawnRandomRadius, spawnRandomRadius),
            UnityEngine.Random.Range(-spawnRandomRadius, spawnRandomRadius),
            0f
        );

        return spawnPos;
    }

    private void MatchEggSortingToChicken(GameObject eggGO)
    {
        SpriteRenderer eggRenderer = eggGO.GetComponent<SpriteRenderer>();
        if (eggRenderer == null || chickenSpriteRenderer == null)
            return;

        eggRenderer.sortingLayerID = chickenSpriteRenderer.sortingLayerID;
        eggRenderer.sortingOrder = chickenSpriteRenderer.sortingOrder + 1;
    }
}