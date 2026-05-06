using System;
using UnityEngine;

public enum AnimalPersonalityKind
{
    Cow,
    Chicken
}

[DisallowMultipleComponent]
public class AnimalPersonalityController : MonoBehaviour
{
    [Header("Animal")]
    [SerializeField] private AnimalPersonalityKind animalKind = AnimalPersonalityKind.Cow;
    [SerializeField] private Vector3 moodBubbleOffset = new Vector3(0f, 1.05f, 0f);

    [Header("Player Reactions")]
    [SerializeField] private float playerDetectRadius = 2.2f;
    [SerializeField] private float chickenRunRadius = 1.15f;
    [SerializeField] private float followStopDistance = 0.85f;
    [SerializeField] private float followSpeed = 0.65f;
    [SerializeField] private float chickenRunSpeed = 1.65f;
    [SerializeField] private float chickenRunDuration = 0.55f;
    [SerializeField] private float reactionCooldown = 2.0f;

    [Header("Food Follow")]
    [SerializeField] private bool followPlayerHoldingFood = true;
    [SerializeField] private float feedingSpotStopDistance = 0.22f;
    [SerializeField] private float fedCooldown = 8f;
    [SerializeField] private string[] foodKeywords =
    {
        "wheat",
        "corn",
        "seed",
        "carrot",
        "lettuce",
        "apple",
        "berry"
    };

    [Header("Sleep")]
    [SerializeField] private float sleepStartTime = 0.78f;
    [SerializeField] private float wakeTime = 0.24f;

    private SpriteRenderer _spriteRenderer;
    private Animator _animator;
    private AnimalMoodBubble _moodBubble;
    private Transform _player;
    private InventoryController _inventory;
    private DayNightCycleNice2D _dayNight;
    private MonoBehaviour _wanderComponent;
    private bool _wanderWasEnabled;
    private float _reactionTimer;
    private float _runTimer;
    private bool _isFollowingFood;
    private bool _isGoingToFeedPile;
    private bool _isRunningAway;
    private Vector3 _runDirection;
    private Vector3 _baseScale;
    private float _fedTimer;

    public AnimalPersonalityKind AnimalKind => animalKind;

    public void Configure(AnimalPersonalityKind kind)
    {
        animalKind = kind;
        moodBubbleOffset = kind == AnimalPersonalityKind.Cow
            ? new Vector3(0f, 1.35f, 0f)
            : new Vector3(0f, 0.78f, 0f);
        _wanderComponent = null;
        ResolveWanderComponent();

        if (_moodBubble != null)
            _moodBubble.SetLocalOffset(moodBubbleOffset);
    }

    private void Awake()
    {
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _animator = GetComponent<Animator>();
        _baseScale = transform.localScale;

        ResolveReferences();
        ResolveWanderComponent();
        EnsureMoodBubble();
    }

    private void OnEnable()
    {
        ResolveReferences();
    }

    private void Update()
    {
        ResolveReferences();
        UpdateSleepMood();

        if (IsSleeping())
        {
            StopPersonalityMovement();
            ApplySleepPose();
            return;
        }

        RestoreAwakePose();
        _fedTimer -= Time.deltaTime;
        UpdatePlayerReaction();
    }

    public void OnMilkedSuccessfully()
    {
        EnsureMoodBubble();
        _moodBubble.ShowTimed(AnimalMoodIcon.Heart, 1.6f);
        PlayTinyHop();
    }

    public void OnEggLaid()
    {
        EnsureMoodBubble();
        _moodBubble.ShowTimed(AnimalMoodIcon.Egg, 1.35f);
        PlayTinyHop();
    }

    private void UpdatePlayerReaction()
    {
        if (_player == null)
            return;

        _reactionTimer -= Time.deltaTime;

        float distance = Vector2.Distance(transform.position, _player.position);
        if (TryMoveToFeedPile())
            return;

        bool hasFood = followPlayerHoldingFood && PlayerHasFoodInHotbar();
        if (hasFood && distance <= playerDetectRadius)
        {
            FollowFood(distance);
            return;
        }

        if (_isFollowingFood)
            StopPersonalityMovement();

        if (animalKind == AnimalPersonalityKind.Chicken)
        {
            UpdateChickenRunAway(distance);
        }
    }

    private bool TryMoveToFeedPile()
    {
        if (_fedTimer > 0f)
            return false;

        AnimalFeedingSpot spot = AnimalFeedingSpot.FindBestSpot(transform.position);
        if (spot == null)
        {
            if (_isGoingToFeedPile)
                StopPersonalityMovement();

            return false;
        }

        EnsureMoodBubble();
        _moodBubble.SetPersistent(AnimalMoodIcon.Food);

        float distance = Vector2.Distance(transform.position, spot.transform.position);
        if (distance <= feedingSpotStopDistance)
        {
            if (spot.TryTakeServing())
            {
                _fedTimer = fedCooldown;
                _moodBubble.ShowTimed(AnimalMoodIcon.Heart, 1.3f);
                PlayTinyHop();
            }

            StopPersonalityMovement();
            return true;
        }

        BeginPersonalityMovement();
        _isGoingToFeedPile = true;
        _isFollowingFood = false;
        _isRunningAway = false;

        Vector3 target = spot.transform.position;
        target.z = transform.position.z;
        transform.position = Vector3.MoveTowards(transform.position, target, followSpeed * Time.deltaTime);
        FaceDirection(target - transform.position);
        return true;
    }

    private void FollowFood(float distance)
    {
        EnsureMoodBubble();
        _moodBubble.SetPersistent(AnimalMoodIcon.Food);

        if (distance <= followStopDistance)
        {
            StopPersonalityMovement(keepFoodMood: true);
            return;
        }

        BeginPersonalityMovement();
        _isFollowingFood = true;
        _isRunningAway = false;

        Vector3 target = _player.position;
        target.z = transform.position.z;
        transform.position = Vector3.MoveTowards(transform.position, target, followSpeed * Time.deltaTime);
        FaceDirection(target - transform.position);
    }

    private void UpdateChickenRunAway(float distance)
    {
        if (_isRunningAway)
        {
            _runTimer -= Time.deltaTime;
            transform.position += _runDirection * (chickenRunSpeed * Time.deltaTime);
            FaceDirection(_runDirection);

            if (_runTimer <= 0f)
                StopPersonalityMovement();

            return;
        }

        if (distance > chickenRunRadius || _reactionTimer > 0f)
            return;

        Vector3 away = transform.position - _player.position;
        away.z = 0f;
        if (away.sqrMagnitude < 0.001f)
            away = UnityEngine.Random.insideUnitCircle.normalized;

        _runDirection = away.normalized;
        _runTimer = chickenRunDuration;
        _reactionTimer = reactionCooldown;
        _isRunningAway = true;

        BeginPersonalityMovement();
        EnsureMoodBubble();
        _moodBubble.ShowTimed(AnimalMoodIcon.Alert, 0.8f);
    }

    private void BeginPersonalityMovement()
    {
        ResolveWanderComponent();

        if (_wanderComponent != null && _wanderComponent.enabled)
        {
            _wanderWasEnabled = true;
            _wanderComponent.enabled = false;
        }

        if (_animator != null)
            _animator.SetBool("isWalking", true);
    }

    private void StopPersonalityMovement(bool keepFoodMood = false)
    {
        _isFollowingFood = false;
        _isGoingToFeedPile = false;
        _isRunningAway = false;
        _runTimer = 0f;

        if (_wanderComponent != null && _wanderWasEnabled)
        {
            _wanderComponent.enabled = true;
            _wanderWasEnabled = false;
        }

        if (_animator != null)
            _animator.SetBool("isWalking", false);

        if (!keepFoodMood && _moodBubble != null && !IsSleeping())
            _moodBubble.SetPersistent(AnimalMoodIcon.None);
    }

    private void UpdateSleepMood()
    {
        EnsureMoodBubble();
        _moodBubble.SetPersistent(IsSleeping() ? AnimalMoodIcon.Sleep : AnimalMoodIcon.None);
    }

    private bool IsSleeping()
    {
        if (_dayNight == null)
            return false;

        float time = _dayNight.TimeNormalized;
        return time >= sleepStartTime || time < wakeTime;
    }

    private void ApplySleepPose()
    {
        if (_animator != null)
            _animator.SetBool("isWalking", false);

        float breathe = 1f + Mathf.Sin(Time.time * 2.2f) * 0.025f;
        transform.localScale = new Vector3(_baseScale.x * breathe, _baseScale.y * (1f / breathe), _baseScale.z);
    }

    private void RestoreAwakePose()
    {
        if ((transform.localScale - _baseScale).sqrMagnitude > 0.0001f)
            transform.localScale = _baseScale;
    }

    private bool PlayerHasFoodInHotbar()
    {
        if (_inventory == null)
            return false;

        for (int i = 0; i < InventoryController.HotbarSize; i++)
        {
            ItemDefinition item = _inventory.GetHotbarItem(i);
            if (IsFoodItem(item))
                return true;
        }

        return false;
    }

    private bool IsFoodItem(ItemDefinition item)
    {
        if (item == null)
            return false;

        string name = $"{item.displayName} {item.name}".ToLowerInvariant();
        for (int i = 0; i < foodKeywords.Length; i++)
        {
            string keyword = foodKeywords[i];
            if (!string.IsNullOrWhiteSpace(keyword) && name.Contains(keyword.ToLowerInvariant()))
                return true;
        }

        return false;
    }

    private void FaceDirection(Vector3 direction)
    {
        if (_animator != null)
        {
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                _animator.SetFloat("moveX", Math.Sign(direction.x));
                _animator.SetFloat("moveY", 0f);
            }
            else
            {
                _animator.SetFloat("moveX", 0f);
                _animator.SetFloat("moveY", Math.Sign(direction.y));
            }
        }

        if (_spriteRenderer != null && animalKind == AnimalPersonalityKind.Chicken && Mathf.Abs(direction.x) > 0.01f)
            _spriteRenderer.flipX = direction.x < 0f;
    }

    private void PlayTinyHop()
    {
        if (!isActiveAndEnabled)
            return;

        StartCoroutine(TinyHopRoutine());
    }

    private System.Collections.IEnumerator TinyHopRoutine()
    {
        Vector3 start = transform.localPosition;
        float duration = 0.22f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            float y = Mathf.Sin(u * Mathf.PI) * 0.08f;
            transform.localPosition = start + Vector3.up * y;
            yield return null;
        }

        transform.localPosition = start;
    }

    private void ResolveReferences()
    {
        if (_player == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                _player = player.transform;
        }

        if (_inventory == null)
            _inventory = InventoryController.Instance != null ? InventoryController.Instance : FindFirstObjectByType<InventoryController>();

        if (_dayNight == null)
            _dayNight = DayNightCycleNice2D.Instance != null ? DayNightCycleNice2D.Instance : FindFirstObjectByType<DayNightCycleNice2D>();
    }

    private void ResolveWanderComponent()
    {
        if (_wanderComponent != null)
            return;

        _wanderComponent = animalKind == AnimalPersonalityKind.Cow
            ? GetComponent<CowWander>()
            : GetComponent<AnimalWander>();
    }

    private void EnsureMoodBubble()
    {
        if (_moodBubble != null)
            return;

        _moodBubble = GetComponent<AnimalMoodBubble>();
        if (_moodBubble == null)
            _moodBubble = gameObject.AddComponent<AnimalMoodBubble>();

        _moodBubble.Initialize(_spriteRenderer, moodBubbleOffset);
    }
}
