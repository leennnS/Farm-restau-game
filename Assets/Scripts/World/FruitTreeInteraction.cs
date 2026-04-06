using UnityEngine;

/// <summary>
/// Simple fruit picking interaction for trees. Player presses a key near the tree and fruit pickups spawn on the ground.
/// Supports a per-day cooldown using the DayNightCycleNice2D.OnDayAdvanced event.
/// </summary>
public class FruitTreeInteraction : MonoBehaviour
{
    [Header("Growth Stages")]
    [SerializeField, Tooltip("4-stage growth cycle. Index 0-2 are growth sprites, index 3 is fruited tree.")]
    private Sprite[] growthSprites = new Sprite[4];
    [SerializeField, Tooltip("Days per growth stage. Each day, growth counter increases.")]
    private int daysPerStage = 1;
    [SerializeField, Tooltip("Seed item definition for planting this tree.")]
    private ItemDefinition seedItem;

    [Header("Interaction")]
    [SerializeField] private float interactionDistance = 4f;
    [SerializeField, Tooltip("Minimum distance from tree edge where player can interact (prevents climbing tree).")]
    private float minOutsideDistance = 0.5f;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private PickupToastUIToolkit toastUI;
    [SerializeField, Tooltip("Toast message to show when player is in range.")]
    private string interactionPrompt = "Press E to pick fruit";
    [SerializeField, Tooltip("Optional manual center override for interaction checks (e.g., a child Transform at trunk center).")]
    private Transform interactionCenterOverride;

    [Header("Fruit")]
    [SerializeField] private ItemDefinition fruitItem;
    [SerializeField] private int minFruit = 1;
    [SerializeField] private int maxFruit = 3;
    [SerializeField] private GameObject pickupPrefab; // prefab with PickupComponent
    [SerializeField, Tooltip("Scale multiplier for spawned fruit pickups (visual only).")]
    private float spawnScaleMultiplier = 1.4f;
    [SerializeField, Tooltip("Sprite with apples/fruit on the tree.")]
    private Sprite spriteWithFruit;
    [SerializeField, Tooltip("Sprite without apples/fruit (after picking).")]
    private Sprite spriteWithoutFruit;
    [SerializeField, Tooltip("Number of days before tree regrows fruit.")]
    private int spriteSwitchBackDays = 3;

    [Header("Drop Placement")]
    [SerializeField] private float dropRadius = 5.0f;
    [SerializeField, Tooltip("Offset above the tree's center from which fruit starts falling.")]
    private float dropHeightOffset = 2.0f;
    [SerializeField, Tooltip("How far down the fruit should fall (in units).")]
    private float fallDistance = 1.5f;
    [SerializeField] private float pickupTtlSeconds = 45f;
    [SerializeField, Tooltip("Seconds before magnet engages so fruit is visible on the ground.")]
    private float pickupMagnetDelaySeconds = 0.8f;
    [SerializeField, Tooltip("Horizontal launch force for fruit (requires Rigidbody2D on pickup prefab).")]
    private float launchForce = 0.6f;
    [SerializeField, Tooltip("Let physics/gravity handle fruit falling naturally instead of clamping.")]
    private bool allowPhysicsFall = true;
    [SerializeField, Tooltip("Safety cap: max vertical drop from spawn if no ground is hit.")]
    private float maxFallDistance = 3.0f;
    [SerializeField, Tooltip("If no collider is hit, snap to a plane below the tree (no ground setup needed).")]
    private bool useFixedLandingPlane = true;
    [SerializeField, Tooltip("How far below the tree base to place fruit when using fixed plane.")]
    private float fixedLandingOffset = 0.3f;
    [SerializeField, Tooltip("Layer mask used for ground raycast.")]
    private LayerMask groundMask = ~0; // default: everything
    [SerializeField, Tooltip("How far down to search for ground when clamping.")]
    private float groundRayDistance = 5f;
    [SerializeField, Tooltip("Vertical offset applied after clamping to avoid z-fighting.")]
    private float groundOffset = 0.05f;
    [SerializeField, Tooltip("Also try 3D raycast if 2D ray misses (useful if ground has 3D colliders).")]
    private bool try3DGroundRaycast = true;
    [Tooltip("Number of in-game days to wait before picking again. Set to 0 for no cooldown.")]
    [SerializeField] private int cooldownDays = 1;

    private Transform _player;
    private Bounds _treeBounds;
    private int _daysSinceLastPick = 99; // high so first pick is allowed
    private DayNightCycleNice2D _cycle;
    private bool _subscribed;
    private bool _inRangeLastFrame = false;
    private SpriteRenderer _spriteRenderer;
    private int _daysSinceSpriteChange = 99; // high so sprite starts with fruit
    private bool _spriteShowsFruit = true;
    private int _growthStage = 0; // 0-3, where 3 is fully mature fruited tree
    private int _daysSinceLastStageAdvance = 0;

    private void OnEnable()
    {
        TryResolvePlayer();
        _cycle = DayNightCycleNice2D.Instance != null ? DayNightCycleNice2D.Instance : FindFirstObjectByType<DayNightCycleNice2D>();
        CacheTreeBounds();
        SubscribeDayEvents();
        if (toastUI == null)
            toastUI = FindFirstObjectByType<PickupToastUIToolkit>();
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        UpdateGrowthSprite();
    }

    private void OnDisable()
    {
        UnsubscribeDayEvents();
    }

    private void CacheTreeBounds()
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            _treeBounds = sr.bounds;
            return;
        }

        var col2d = GetComponentInChildren<Collider2D>();
        if (col2d != null)
        {
            _treeBounds = col2d.bounds;
            return;
        }

        var col3d = GetComponentInChildren<Collider>();
        if (col3d != null)
        {
            _treeBounds = col3d.bounds;
            return;
        }

        _treeBounds = new Bounds(transform.position, Vector3.zero);
    }

    private void Update()
    {
        if (_player == null)
            TryResolvePlayer();

        if (_player == null)
            return;

        Vector3 center = interactionCenterOverride != null
            ? interactionCenterOverride.position
            : (_treeBounds.size.sqrMagnitude > 0f ? _treeBounds.center : transform.position);

        float playerDist = Vector2.Distance(new Vector2(center.x, center.y), new Vector2(_player.position.x, _player.position.y));

        // Calculate tree radius (half of the bounds size)
        float treeRadius = 0f;
        if (_treeBounds.size.sqrMagnitude > 0f)
        {
            treeRadius = Mathf.Max(_treeBounds.extents.x, _treeBounds.extents.y);
        }

        // Player can interact from anywhere within interactionDistance from tree center
        float maxDistanceFromCenter = treeRadius + interactionDistance;

        bool inRange = playerDist <= maxDistanceFromCenter;

        // Show toast only when entering range
        if (inRange && !_inRangeLastFrame && toastUI != null)
        {
            string prompt = interactionPrompt;
            if (_growthStage < 3)
                prompt = $"Growing... ({_growthStage + 1}/4)";
            toastUI.Show(prompt);
        }
        _inRangeLastFrame = inRange;

        if (!inRange)
            return;

        if (Input.GetKeyDown(interactionKey))
        {
            if (_growthStage < 3)
            {

                return;
            }

            if (CanPick())
                DoPick();
        }
    }

    private bool CanPick()
    {
        return _daysSinceLastPick >= cooldownDays;
    }

    private void DoPick()
    {
        if (fruitItem == null || pickupPrefab == null)
        {

            return;
        }

        int fruitCount = Random.Range(minFruit, maxFruit + 1);

        // Spawn from the middle of the tree (bounds center) plus vertical offset.
        Vector3 basePos = (_treeBounds.size.sqrMagnitude > 0f ? _treeBounds.center : transform.position) + Vector3.up * dropHeightOffset;

        for (int i = 0; i < fruitCount; i++)
        {
            // Use X/Y for spread so fruit spawns around the tree in the visible plane (not behind on Z).
            Vector2 offset = Random.insideUnitCircle * dropRadius;
            Vector3 spawnPos = basePos + new Vector3(offset.x, offset.y, 0f);
            GameObject go = Instantiate(pickupPrefab, spawnPos, Quaternion.identity);

            // Scale up the fruit for visibility
            go.transform.localScale *= spawnScaleMultiplier;

            PickupComponent pickup = go.GetComponent<PickupComponent>();
            if (pickup != null)
            {
                pickup.Set(fruitItem, 1);
                pickup.SetTimeToLive(pickupTtlSeconds);
                pickup.SetMagnetDelay(pickupMagnetDelaySeconds);
            }

            // Add physics to make fruit fall and clamp to tree bounds
            var rb2d = go.GetComponent<Rigidbody2D>();
            if (rb2d != null)
            {
                // Enable gravity so fruit falls naturally
                rb2d.gravityScale = 1.5f; // slightly increased gravity for faster, visible fall

                // Add stronger horizontal toss and downward impulse for better separation
                float horizontal = Random.Range(-launchForce * 1.5f, launchForce * 1.5f);
                float downwardForce = Random.Range(fallDistance * 0.8f, fallDistance * 1.5f);
                rb2d.AddForce(new Vector2(horizontal, -downwardForce), ForceMode2D.Impulse);

                // Attach a component to clamp fruit to tree bounds with extra fall distance
                var fruitFallLimiter = go.AddComponent<FruitFallLimiter>();
                fruitFallLimiter.SetTreeBounds(_treeBounds, 0.8f); // Allow 0.8 units below the tree
            }
        }

        _daysSinceLastPick = 0;

        // Switch sprite to show no fruit
        if (spriteWithoutFruit != null && _spriteRenderer != null)
        {
            _spriteRenderer.sprite = spriteWithoutFruit;
            _spriteShowsFruit = false;
            _daysSinceSpriteChange = 0;
        }
    }

    private void OnDayAdvanced()
    {
        _daysSinceLastPick++;

        // Handle growth stage progression
        if (_growthStage < 3) // Not fully mature yet
        {
            _daysSinceLastStageAdvance++;
            if (_daysSinceLastStageAdvance >= daysPerStage)
            {
                _growthStage++;
                _daysSinceLastStageAdvance = 0;
                UpdateGrowthSprite();
            }
        }

        // Check if sprite needs to switch back to showing fruit
        if (_growthStage == 3 && !_spriteShowsFruit)
        {
            _daysSinceSpriteChange++;
            if (_daysSinceSpriteChange >= spriteSwitchBackDays)
            {
                if (spriteWithFruit != null && _spriteRenderer != null)
                {
                    _spriteRenderer.sprite = spriteWithFruit;
                    _spriteShowsFruit = true;

                }
            }
        }
    }

    private void UpdateGrowthSprite()
    {
        if (_spriteRenderer == null || growthSprites == null || growthSprites.Length < 4)
            return;

        // Use growth sprite unless fruits were picked (then show picked sprite until fruit regrows)
        if (_growthStage == 3 && !_spriteShowsFruit && spriteWithoutFruit != null)
        {
            _spriteRenderer.sprite = spriteWithoutFruit;
        }
        else if (_growthStage < growthSprites.Length && growthSprites[_growthStage] != null)
        {
            _spriteRenderer.sprite = growthSprites[_growthStage];
        }
    }

    private void SubscribeDayEvents()
    {
        if (_subscribed)
            return;

        DayNightCycleNice2D.OnDayAdvanced += OnDayAdvanced;
        _subscribed = true;
    }

    private void UnsubscribeDayEvents()
    {
        if (!_subscribed)
            return;

        DayNightCycleNice2D.OnDayAdvanced -= OnDayAdvanced;
        _subscribed = false;
    }

    /// <summary>
    /// Initializes a newly planted tree from a seed.
    /// Call this right after instantiating the tree via planting.
    /// </summary>
    public void InitializeAsNewSapling()
    {
        _growthStage = 0;
        _daysSinceLastStageAdvance = -1; // One full day delay before first growth
        _daysSinceLastPick = 0;
        _spriteShowsFruit = true;
        _daysSinceSpriteChange = 0;
        UpdateGrowthSprite();

    }

    /// <summary>
    /// Returns true if the tree is fully mature (growth stage 3) and can be picked.
    /// </summary>
    public bool IsFullyMature()
    {
        return _growthStage == 3;
    }

    /// <summary>
    /// Gets the seed item for this tree.
    /// </summary>
    public ItemDefinition GetSeedItem()
    {
        return seedItem;
    }

    private bool TryClamp2D(Vector3 spawnPos, Transform target, Rigidbody2D rb)
    {
        RaycastHit2D hit = Physics2D.Raycast(spawnPos + Vector3.up * 0.5f, Vector2.down, groundRayDistance, groundMask);
        if (hit.collider == null)
            return false;

        Vector3 landed = new Vector3(spawnPos.x, hit.point.y + groundOffset, spawnPos.z);
        target.position = landed;
        return true;
    }

    private bool TryClamp3D(Vector3 spawnPos, Transform target)
    {
        Ray ray = new Ray(spawnPos + Vector3.up * 0.5f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, groundRayDistance, groundMask))
        {
            Vector3 landed = new Vector3(spawnPos.x, hit.point.y + groundOffset, spawnPos.z);
            target.position = landed;
            return true;
        }

        return false;
    }

    private bool TryClampFallback(Vector3 spawnPos, Transform target)
    {
        float baseY = _treeBounds.size.sqrMagnitude > 0f ? _treeBounds.min.y : transform.position.y;
        float landedY;

        if (useFixedLandingPlane)
            landedY = baseY - fixedLandingOffset;
        else
            landedY = spawnPos.y - maxFallDistance;

        target.position = new Vector3(spawnPos.x, landedY, spawnPos.z);
        return true;
    }

    private void SuppressPhysics(Rigidbody2D rb)
    {
        if (rb == null) return;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = 0f;
    }

    private void TryResolvePlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            _player = player.transform;
    }
}
