using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class RestaurantSpawnPoint : MonoBehaviour
{
    [Tooltip("Radius used when no trigger collider is present (world units)")]
    public float detectionRadius = 1.25f;

    [Tooltip("Seconds to wait while player remains in range before loading the target scene")]
    public float countdownSeconds = 4f;

    [Tooltip("Name of the scene to load after countdown")]
    public string targetSceneName = "FarmScene";

    [Tooltip("Scale to apply to the player on entry")]
    public Vector3 entryScale = new Vector3(0.5f, 0.5f, 1f);

    [Tooltip("Max search radius to find a non-overlapping spawn position")]
    public float safeSearchMax = 0.8f;
    [Tooltip("Search step for safe position (meters)")]
    public float safeSearchStep = 0.2f;

    bool timerRunning = false;
    Coroutine runningCoroutine;

    Collider2D localCollider;

    void Start()
    {
        // Only operate in RestaurantScene
        if (SceneManager.GetActiveScene().name != "RestaurantScene")
            return;

        localCollider = GetComponent<Collider2D>();

        // Handle entry: move existing player to spawn
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            MovePlayerToSpawn(player);
        }
    }

    void Update()
    {
        // If trigger collider exists and isTrigger, skip distance checks
        if (localCollider != null && localCollider.isTrigger)
            return;

        // Distance-based detection
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO == null)
            return;

        float d = Vector2.Distance(playerGO.transform.position, transform.position);
        if (d <= detectionRadius)
        {
            if (!timerRunning)
                StartTimer();
        }
        else
        {
            if (timerRunning)
                StopTimer();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (localCollider == null || !localCollider.isTrigger) return;
        if (!other.CompareTag("Player")) return;

        if (!timerRunning)
            StartTimer();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (localCollider == null || !localCollider.isTrigger) return;
        if (!other.CompareTag("Player")) return;

        if (timerRunning)
            StopTimer();
    }

    void StartTimer()
    {
        timerRunning = true;
        runningCoroutine = StartCoroutine(CountdownAndLoad());
    }

    void StopTimer()
    {
        timerRunning = false;
        if (runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
            runningCoroutine = null;
        }
    }

    IEnumerator CountdownAndLoad()
    {
        float t = 0f;
        while (t < countdownSeconds)
        {
            // If using distance checks, ensure player still in radius
            if (localCollider == null || !localCollider.isTrigger)
            {
                var p = GameObject.FindWithTag("Player");
                if (p == null) yield break;
                float d = Vector2.Distance(p.transform.position, transform.position);
                if (d > detectionRadius)
                {
                    timerRunning = false;
                    yield break;
                }
            }

            t += Time.deltaTime;
            yield return null;
        }

        var player = GameObject.FindWithTag("Player");
        if (player == null) yield break;

        RestaurantReturnContext.PendingReturnToFarm = true;
        SceneManager.LoadScene(targetSceneName);
    }

    void MovePlayerToSpawn(GameObject player)
    {
        // Ensure movement constraint & freeze rotation
        if (player.GetComponent<PlayerMovementConstraint>() == null)
            player.AddComponent<PlayerMovementConstraint>();
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.constraints |= RigidbodyConstraints2D.FreezeRotation;

        // Find a safe non-overlapping position near spawn
        Vector2 spawnPos = transform.position;
        Vector2 safePos = FindSafePosition(spawnPos, player);

        player.transform.position = safePos;
    }

    Vector2 FindSafePosition(Vector2 center, GameObject player)
    {
        float radius = 0.25f;
        var pcl = player.GetComponent<Collider2D>();
        if (pcl != null)
        {
            // approximate radius from collider bounds
            radius = Mathf.Max(pcl.bounds.extents.x, pcl.bounds.extents.y);
            radius = Mathf.Max(radius, 0.1f);
        }

        // get colliders under Grid to consider as blocking
        GameObject grid = GameObject.Find("Grid");
        Collider2D[] hits;

        // Try center first
        if (!IsOverlappingGrid(center, radius, player, grid))
            return center;

        // Spiral search
        for (float r = safeSearchStep; r <= safeSearchMax; r += safeSearchStep)
        {
            int steps = Mathf.CeilToInt(2 * Mathf.PI * r / safeSearchStep);
            steps = Mathf.Max(8, steps);
            for (int i = 0; i < steps; i++)
            {
                float ang = (i / (float)steps) * Mathf.PI * 2f;
                Vector2 candidate = center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
                if (!IsOverlappingGrid(candidate, radius, player, grid))
                    return candidate;
            }
        }

        // fallback: return center even if overlapping
        return center;
    }

    bool IsOverlappingGrid(Vector2 pos, float radius, GameObject player, GameObject grid)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = false;
        Collider2D[] results = new Collider2D[8];
        int count = Physics2D.OverlapCircle(pos, radius, filter, results);
        for (int i = 0; i < count; i++)
        {
            var c = results[i];
            if (c == null) continue;
            if (c.gameObject == player) continue;
            if (grid != null && c.transform.IsChildOf(grid.transform))
            {
                return true; // overlapping a grid collider
            }
            // Also consider other static colliders in scene as blocking
            if (grid == null)
                return true;
        }
        return false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
