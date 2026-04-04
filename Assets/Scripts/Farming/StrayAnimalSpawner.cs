using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns random stray animals on the farm periodically to destroy crops.
/// Manages active stray animals and their spawning.
/// </summary>
public class StrayAnimalSpawner : MonoBehaviour
{
    [SerializeField] private StrayAnimalDefinition[] availableAnimals;
    [SerializeField] private FarmingManager farmingManager;

    [Header("Spawning")]
    [SerializeField, Range(10f, 120f)] private float minSpawnIntervalSeconds = 30f;
    [SerializeField, Range(10f, 120f)] private float maxSpawnIntervalSeconds = 60f;
    [SerializeField] private int maxActiveAnimals = 2;

    [Header("Spawn Area")]
    [SerializeField] private Transform spawnAreaCenter;
    [SerializeField] private Vector2 spawnAreaSize = new Vector2(20f, 20f);

    [Header("Prefab")]
    [SerializeField] private GameObject strayAnimalPrefab; // Prefab with StrayAnimalController

    private List<StrayAnimalController> activeAnimals = new List<StrayAnimalController>();
    private float spawnTimer;
    private float nextSpawnTime;

    private void Start()
    {
        if (farmingManager == null)
        {
            farmingManager = FindFirstObjectByType<FarmingManager>();
        }

        // Auto-find spawn area center if not assigned
        if (spawnAreaCenter == null)
        {
            // Try to find a transform named "FarmCenter" or similar
            spawnAreaCenter = GameObject.Find("FarmCenter")?.transform;

            // If still not found, use FarmingManager's transform
            if (spawnAreaCenter == null && farmingManager != null)
                spawnAreaCenter = farmingManager.transform;

            // If still null, use origin
            if (spawnAreaCenter == null)
                Debug.LogWarning("[StrayAnimalSpawner] No spawn area center found. Using world origin (0,0)");
        }

        if (availableAnimals == null || availableAnimals.Length == 0)
        {
            Debug.LogWarning("[StrayAnimalSpawner] No animal definitions assigned!");
            enabled = false;
            return;
        }

        ResetSpawnTimer();
    }

    private void Update()
    {
        // Clean up destroyed animals
        activeAnimals.RemoveAll(a => a == null);

        // Check if it's time to spawn
        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f && activeAnimals.Count < maxActiveAnimals)
        {
            SpawnRandomAnimal();
            ResetSpawnTimer();
        }
    }

    private void SpawnRandomAnimal()
    {
        // Pick a random animal definition
        StrayAnimalDefinition animalDef = availableAnimals[Random.Range(0, availableAnimals.Length)];

        // Get spawn position
        Vector3 spawnPos = GetRandomSpawnPosition();

        // Create the animal
        GameObject animalGO;
        if (strayAnimalPrefab != null)
        {
            animalGO = Instantiate(strayAnimalPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            // Create dynamically
            animalGO = new GameObject($"StrayAnimal_{animalDef.animalName}");
            animalGO.transform.position = spawnPos;

            // Add sprite renderer
            SpriteRenderer sr = animalGO.AddComponent<SpriteRenderer>();
            if (animalDef.animalSprite != null)
                sr.sprite = animalDef.animalSprite;
            sr.color = animalDef.animalColor;
            sr.sortingOrder = 1;

            // Add collider for clicking
            CircleCollider2D collider = animalGO.AddComponent<CircleCollider2D>();
            collider.radius = 0.3f;

            // Add Rigidbody2D so physics doesn't interfere
            Rigidbody2D rb = animalGO.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        // Put on "Enemies" layer so it doesn't block farming raycasts
        int enemyLayer = LayerMask.NameToLayer("Default");
        animalGO.layer = enemyLayer;

        // Add controller if not already present
        StrayAnimalController controller = animalGO.GetComponent<StrayAnimalController>();
        if (controller == null)
            controller = animalGO.AddComponent<StrayAnimalController>();

        // Set definition
        controller.SetDefinition(animalDef, farmingManager);

        activeAnimals.Add(controller);
        Debug.Log($"[StrayAnimalSpawner] Spawned {animalDef.animalName} at {spawnPos}. Active: {activeAnimals.Count}/{maxActiveAnimals}");
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 center = spawnAreaCenter != null ? spawnAreaCenter.position : Vector3.zero;
        float x = center.x + Random.Range(-spawnAreaSize.x * 0.5f, spawnAreaSize.x * 0.5f);
        float y = center.y + Random.Range(-spawnAreaSize.y * 0.5f, spawnAreaSize.y * 0.5f);
        return new Vector3(x, y, 0f);
    }

    private void ResetSpawnTimer()
    {
        nextSpawnTime = Random.Range(minSpawnIntervalSeconds, maxSpawnIntervalSeconds);
        spawnTimer = nextSpawnTime;
    }

    public void ClearAllAnimals()
    {
        foreach (var animal in activeAnimals)
        {
            if (animal != null)
                Destroy(animal.gameObject);
        }
        activeAnimals.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        // Show spawn area
        if (spawnAreaCenter != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawCube(spawnAreaCenter.position, new Vector3(spawnAreaSize.x, spawnAreaSize.y, 1f));
        }
    }
}
