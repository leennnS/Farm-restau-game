using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Behavior for a stray animal on the farm.
/// Wanders around, detects and destroys nearby crops, and can be killed by clicking on it.
/// </summary>
public class StrayAnimalController : MonoBehaviour
{
    [SerializeField] private StrayAnimalDefinition definition;
    [SerializeField] private FarmingManager farmingManager;

    // Movement state
    private Vector3 targetPosition;
    private float stateTimer;
    private bool isWalking;
    private Vector2 wanderBounds = new Vector2(15f, 15f);
    private float moveX = 1f;

    // Lifetime tracking
    private float lifetimeRemaining;

    // Crop destruction tracking
    private float destructionCooldown;

    private SpriteRenderer spriteRenderer;

    public void SetDefinition(StrayAnimalDefinition def, FarmingManager farmMgr)
    {
        definition = def;
        farmingManager = farmMgr;
    }

    private void Start()
    {
        if (definition == null)
        {
            Debug.LogError("StrayAnimalController: No definition assigned!");
            Destroy(gameObject);
            return;
        }

        if (farmingManager == null)
        {
            farmingManager = FindFirstObjectByType<FarmingManager>();
            if (farmingManager == null)
            {
                Debug.LogError("StrayAnimalController: FarmingManager not found!");
                Destroy(gameObject);
                return;
            }
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        // Set visuals
        if (definition.animalSprite != null)
            spriteRenderer.sprite = definition.animalSprite;
        spriteRenderer.color = definition.animalColor;

        // Initialize state
        lifetimeRemaining = definition.lifeTimeDuration;
        stateTimer = Random.Range(definition.minIdleTime, definition.maxIdleTime);
        isWalking = false;
        destructionCooldown = 0f;

        if (Random.value > 0.5f)
            PickWalkState();
        else
            PickIdleState();
    }

    private void Update()
    {
        // Countdown lifetime
        lifetimeRemaining -= Time.deltaTime;
        if (lifetimeRemaining <= 0f)
        {
            OnAnimalDespawn();
            return;
        }

        // Handle movement
        stateTimer -= Time.deltaTime;

        if (isWalking)
        {
            MoveTowardTarget();

            if (Vector3.Distance(transform.position, targetPosition) <= 0.1f || stateTimer <= 0f)
            {
                PickIdleState();
            }
        }
        else
        {
            if (stateTimer <= 0f)
            {
                PickWalkState();
            }
        }

        // Check for nearby crops to destroy
        DestroyNearbyCrops();
    }

    private void PickWalkState()
    {
        isWalking = true;
        stateTimer = Random.Range(definition.minWalkTime, definition.maxWalkTime);
        targetPosition = GetRandomWanderPoint();

        Vector3 direction = (targetPosition - transform.position).normalized;
        if (Mathf.Abs(direction.x) > 0.01f)
            moveX = direction.x;
    }

    private void PickIdleState()
    {
        isWalking = false;
        stateTimer = Random.Range(definition.minIdleTime, definition.maxIdleTime);
    }

    private void MoveTowardTarget()
    {
        Vector3 direction = (targetPosition - transform.position).normalized;

        if (Mathf.Abs(direction.x) > 0.01f)
            moveX = direction.x;

        // Update sprite facing
        if (spriteRenderer != null)
            spriteRenderer.flipX = moveX < 0;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            definition.moveSpeed * Time.deltaTime
        );
    }

    private Vector3 GetRandomWanderPoint()
    {
        Vector3 center = transform.position;
        float x = center.x + Random.Range(-wanderBounds.x, wanderBounds.x);
        float y = center.y + Random.Range(-wanderBounds.y, wanderBounds.y);
        return new Vector3(x, y, transform.position.z);
    }

    private void DestroyNearbyCrops()
    {
        destructionCooldown -= Time.deltaTime;
        if (destructionCooldown > 0f)
            return;

        destructionCooldown = 0.5f; // Check every 0.5 seconds

        var allCrops = farmingManager.GetPlantedCrops();

        foreach (var cropEntry in allCrops)
        {
            Vector3Int cropPos = cropEntry.Key;
            Vector3 cropWorldPos = farmingManager.CropTilemap.CellToWorld(cropPos);
            cropWorldPos.x += 0.5f;
            cropWorldPos.y += 0.5f;

            float distance = Vector3.Distance(transform.position, cropWorldPos);

            if (distance <= definition.cropDetectionRadius)
            {
                // Destroy with the defined chance
                if (Random.value < definition.destructionChancePerSecond)
                {
                    if (farmingManager.TryDestroyPlantedCrop(cropPos))
                    {
                        Debug.Log($"[StrayAnimal] DESTROYED crop at {cropPos}!");
                    }
                }
            }
        }
    }

    private void OnAnimalDespawn()
    {
        Debug.Log($"[StrayAnimal] {definition.animalName} escapes with a final cackle!");
        Destroy(gameObject);
    }

    /// <summary>
    /// Called when player clicks directly on this animal to kill it.
    /// </summary>
    private void OnMouseDown()
    {
        // Only respond to left mouse button (button 0)
        if (Input.GetMouseButtonDown(0))
        {
            OnPlayerKill();
        }
    }

    /// <summary>
    /// Called when player kills this animal.
    /// </summary>
    public void OnPlayerKill()
    {
        Debug.Log($"[StrayAnimal] Player killed the {definition.animalName}!");
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        // Show crop detection radius in editor
        if (definition != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, definition.cropDetectionRadius);
        }
    }
}
