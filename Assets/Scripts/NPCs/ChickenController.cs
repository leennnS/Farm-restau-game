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
        chickenSpriteRenderer = GetComponent<SpriteRenderer>();

        if (dayNightCycle == null)
        {
            dayNightCycle = DayNightCycleNice2D.Instance;

            if (dayNightCycle == null)
            {
                dayNightCycle = FindFirstObjectByType<DayNightCycleNice2D>();
            }
        }

        if (eggPrefabPath == null)
        {
            eggPrefabPath = Resources.Load<GameObject>("Prefabs/Items/Egg");
        }

        if (eggItem == null)
        {
            return;
        }

        DayNightCycleNice2D.OnDayAdvanced += OnNewDay;
    }

    private void OnDestroy()
    {
        DayNightCycleNice2D.OnDayAdvanced -= OnNewDay;
    }

    private void OnNewDay()
    {
        hasLaidEggToday = false;
        lastCheckedTime = -1f;

    }

    private void Update()
    {
        if (dayNightCycle == null)
        {
            dayNightCycle = DayNightCycleNice2D.Instance;
            if (dayNightCycle == null)
                dayNightCycle = FindFirstObjectByType<DayNightCycleNice2D>();

            if (dayNightCycle == null)
            {
                _warnedMissingCycle = true;
                return;
            }
            else
            {
                _warnedMissingCycle = false;
            }
        }

        if (eggItem == null)
        {
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
            TryLayEgg();
        }

        lastCheckedTime = currentTimeNormalized;
    }

    private void TryLayEgg()
    {
        if (hasLaidEggToday)
        {
            return;
        }

        if (eggPrefabPath == null)
        {

            return;
        }

        Vector3 spawnPos = GetEggSpawnPosition();



        GameObject eggGO = Instantiate(eggPrefabPath, spawnPos, Quaternion.identity);
        if (eggGO == null)
        {

            return;
        }



        Rigidbody2D rb = eggGO.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

        }
        else
        {

        }

        Collider2D collider = eggGO.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;

        }
        else
        {

        }

        PickupComponent pickup = eggGO.GetComponent<PickupComponent>();
        if (pickup != null)
        {
            pickup.Set(eggItem, eggCount);
            pickup.SetTimeToLive(eggTimeToLive);

        }
        else
        {

            Destroy(eggGO);
            return;
        }

        MatchEggSortingToChicken(eggGO);

        hasLaidEggToday = true;

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