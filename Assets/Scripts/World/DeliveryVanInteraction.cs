using System;
using System.Collections.Generic;
using UnityEngine;

public class DeliveryVanInteraction : MonoBehaviour
{
    private static readonly HashSet<string> s_arrivedTodayKeys = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetDailyArrivalMemory()
    {
        s_arrivedTodayKeys.Clear();
    }

    [Serializable]
    public class DeliveryReward
    {
        public ItemDefinition item;
        [Min(1)] public int amount = 1;
        public Sprite carriedSpriteOverride;
    }

    private enum DeliveryClaimMode
    {
        CarryVisualOnly,
        AddToInventory,
        CarryVisualAndInventory
    }

    private enum VanState
    {
        DrivingToEntrance,
        WaitingForPlayer,
        Claimed,
        Leaving
    }

    [Header("Van Visual")]
    [SerializeField] private SpriteRenderer vanSpriteRenderer;
    [SerializeField] private Sprite vanSprite;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 50;
    [SerializeField] private bool overrideVisualScale = false;
    [SerializeField] private Vector3 visualScale = Vector3.one;

    [Header("Route")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform entranceStopPoint;
    [SerializeField] private Transform exitPoint;
    [SerializeField, Min(0.1f)] private float moveSpeed = 4f;
    [SerializeField] private bool startDrivingOnAwake = true;
    [SerializeField] private bool destroyAfterLeaving = true;

    [Header("Delivery")]
    [SerializeField] private List<DeliveryReward> rewards = new List<DeliveryReward>();
    [SerializeField] private DeliveryClaimMode claimMode = DeliveryClaimMode.CarryVisualOnly;
    [SerializeField] private bool claimOnlyOnce = true;
    [SerializeField] private bool hideVanIfAlreadyClaimed = false;
    [SerializeField] private string vanId = "";

    [Header("Daily Schedule")]
    [SerializeField] private bool spawnOnlyOncePerDay = true;
    [SerializeField] private bool hideWhenAlreadyArrivedToday = true;

    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private string playerTag = "Player";
    [SerializeField, Min(0.25f)] private float interactionDistance = 6f;
    [SerializeField] private string arrivalMessage = "Delivery van arrived at the farm entrance!";
    [SerializeField] private string promptMessage = "Press E to take your delivery";
    [SerializeField] private string claimedMessage = "Delivery collected";
    [SerializeField] private string emptyMessage = "No delivery items assigned";
    [SerializeField] private string inventoryFullMessage = "Inventory full";

    [Header("Audio")]
    [SerializeField] private AudioClip honkSound;
    [SerializeField, Range(0f, 2f)] private float honkVolume = 1f;
    [SerializeField] private bool honkOnArrival = true;
    [SerializeField] private bool honkOnClaim = true;

    [Header("Player Carry Visual")]
    [SerializeField] private bool showCarryAfterClaim = true;
    [SerializeField, Min(0.1f)] private float carryVisualDuration = 3f;
    [SerializeField] private Sprite[] playerCarryDownSprites;
    [SerializeField] private Sprite[] playerCarryUpSprites;
    [SerializeField] private Sprite[] playerCarryLeftSprites;
    [SerializeField] private Sprite[] playerCarryRightSprites;
    [SerializeField, Min(1f)] private float playerCarryFramesPerSecond = 8f;

    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private InventoryController inventoryController;
    [SerializeField] private PickupToastUIToolkit pickupToast;
    [SerializeField] private PlayerCarryVisualController playerCarryVisual;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private VanState _state;
    private AudioSource _audioSource;
    private bool _arrivalAnnounced;
    private bool _promptShowing;

    public bool BlocksTraffic => isActiveAndEnabled && (_state == VanState.DrivingToEntrance || _state == VanState.WaitingForPlayer);
    public bool CanInteractNow => isActiveAndEnabled && _state == VanState.WaitingForPlayer;

    private void OnEnable()
    {
        DayNightCycleNice2D.OnDayAdvanced += HandleDayAdvanced;
    }

    private void OnDisable()
    {
        DayNightCycleNice2D.OnDayAdvanced -= HandleDayAdvanced;
    }

    private void Awake()
    {
        ResolveStaticReferences();
        ApplyVanSprite();
        EnsureAudioSource();

        if (claimOnlyOnce && HasBeenClaimed() && hideVanIfAlreadyClaimed)
        {
            if (debugLogs)
                Debug.Log($"[DeliveryVanInteraction] '{name}' is hidden because it was already claimed. Use Reset Delivery Van Claim or disable Hide Van If Already Claimed.");

            SetVanVisible(false);
            _state = VanState.Claimed;
            return;
        }

        if (startDrivingOnAwake && spawnOnlyOncePerDay && HasArrivedToday())
        {
            if (debugLogs)
                Debug.Log($"[DeliveryVanInteraction] '{name}' already arrived today, so it will not spawn again until the next day.");

            if (hideWhenAlreadyArrivedToday)
                SetVanVisible(false);

            _state = VanState.Claimed;
            return;
        }

        if (startDrivingOnAwake && spawnPoint != null)
        {
            MarkArrivedToday();
            transform.position = ResolveRoutePosition(spawnPoint);
            _state = VanState.DrivingToEntrance;

            if (debugLogs)
                Debug.Log($"[DeliveryVanInteraction] '{name}' started driving from {transform.position} to {(entranceStopPoint != null ? entranceStopPoint.position.ToString() : "no stop point")}.");
        }
        else
        {
            if (startDrivingOnAwake)
                MarkArrivedToday();

            if (entranceStopPoint != null)
                transform.position = ResolveRoutePosition(entranceStopPoint);

            _state = VanState.WaitingForPlayer;
            AnnounceArrival();

            if (debugLogs)
                Debug.Log($"[DeliveryVanInteraction] '{name}' is waiting at {transform.position}.");
        }
    }

    private void Update()
    {
        ResolveRuntimeReferences();

        switch (_state)
        {
            case VanState.DrivingToEntrance:
                DriveTowardEntrance();
                break;
            case VanState.WaitingForPlayer:
                UpdateWaitingInteraction();
                break;
            case VanState.Leaving:
                DriveAway();
                break;
        }
    }

    public void StartDelivery()
    {
        if (claimOnlyOnce && HasBeenClaimed() && hideVanIfAlreadyClaimed)
            return;

        if (spawnOnlyOncePerDay && HasArrivedToday())
        {
            if (debugLogs)
                Debug.Log($"[DeliveryVanInteraction] '{name}' cannot restart because it already arrived today.");

            return;
        }

        if (spawnPoint != null)
            transform.position = ResolveRoutePosition(spawnPoint);

        MarkArrivedToday();
        SetVanVisible(true);
        _arrivalAnnounced = false;
        _state = VanState.DrivingToEntrance;

        if (debugLogs)
            Debug.Log($"[DeliveryVanInteraction] '{name}' delivery restarted.");
    }

    private void DriveTowardEntrance()
    {
        if (entranceStopPoint == null)
        {
            _state = VanState.WaitingForPlayer;
            AnnounceArrival();

            if (debugLogs)
                Debug.LogWarning($"[DeliveryVanInteraction] '{name}' has no Entrance Stop Point, so it stopped at {transform.position}.");

            return;
        }

        Vector3 stopPosition = ResolveRoutePosition(entranceStopPoint);
        transform.position = Vector3.MoveTowards(transform.position, stopPosition, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, stopPosition) <= 0.03f)
        {
            transform.position = stopPosition;
            _state = VanState.WaitingForPlayer;
            AnnounceArrival();

            if (debugLogs)
                Debug.Log($"[DeliveryVanInteraction] '{name}' reached entrance stop at {transform.position}.");
        }
    }

    private void UpdateWaitingInteraction()
    {
        bool playerClose = IsPlayerClose();

        if (playerClose)
        {
            ShowPrompt();

            if (Input.GetKeyDown(interactKey))
                ClaimDelivery();
        }
        else
        {
            HidePrompt();
        }
    }

    private void ClaimDelivery()
    {
        if (rewards == null || rewards.Count == 0)
        {
            pickupToast?.Show(emptyMessage);
            return;
        }

        if (claimMode != DeliveryClaimMode.CarryVisualOnly && inventoryController == null)
        {
            Debug.LogWarning("[DeliveryVanInteraction] Cannot claim delivery because InventoryController was not found.");
            return;
        }

        int validRewards = 0;
        int grantedRewards = 0;
        DeliveryReward firstValidReward = null;

        for (int i = 0; i < rewards.Count; i++)
        {
            DeliveryReward reward = rewards[i];
            if (reward == null || reward.item == null || reward.amount <= 0)
                continue;

            validRewards++;
            if (firstValidReward == null)
                firstValidReward = reward;

            if (claimMode == DeliveryClaimMode.CarryVisualOnly)
            {
                grantedRewards++;
                continue;
            }

            if (inventoryController.TryAdd(reward.item, reward.amount))
            {
                grantedRewards++;

                if (claimMode == DeliveryClaimMode.AddToInventory)
                    pickupToast?.Show($"+{reward.amount} {reward.item.displayName}");
            }
        }

        if (validRewards == 0)
        {
            pickupToast?.Show(emptyMessage);
            return;
        }

        if (grantedRewards != validRewards)
        {
            pickupToast?.Show(inventoryFullMessage);
            return;
        }

        if (claimOnlyOnce)
            MarkClaimed();

        if (honkOnClaim)
            PlayHonk();

        if (showCarryAfterClaim && claimMode != DeliveryClaimMode.AddToInventory)
            ShowPlayerCarryVisual(firstValidReward);

        pickupToast?.Show(claimedMessage);
        HidePrompt();
        _state = exitPoint != null ? VanState.Leaving : VanState.Claimed;
    }

    private void DriveAway()
    {
        if (exitPoint == null)
        {
            _state = VanState.Claimed;
            return;
        }

        Vector3 targetPosition = ResolveRoutePosition(exitPoint);
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, targetPosition) <= 0.03f)
        {
            _state = VanState.Claimed;
            if (destroyAfterLeaving)
                SetVanVisible(false);
        }
    }

    private void HandleDayAdvanced()
    {
        if (!startDrivingOnAwake || !spawnOnlyOncePerDay)
            return;

        if (HasArrivedToday())
            return;

        if (claimOnlyOnce && HasBeenClaimed() && hideVanIfAlreadyClaimed)
            ResetClaimState();

        StartDeliveryForNewDay();
    }

    private void StartDeliveryForNewDay()
    {
        if (spawnPoint != null)
            transform.position = ResolveRoutePosition(spawnPoint);
        else if (entranceStopPoint != null)
            transform.position = ResolveRoutePosition(entranceStopPoint);

        MarkArrivedToday();
        SetVanVisible(true);
        _arrivalAnnounced = false;
        _promptShowing = false;
        _state = spawnPoint != null ? VanState.DrivingToEntrance : VanState.WaitingForPlayer;

        if (_state == VanState.WaitingForPlayer)
            AnnounceArrival();

        if (debugLogs)
            Debug.Log($"[DeliveryVanInteraction] '{name}' started for new day {GetCurrentDayIndex()}.");
    }

    private void AnnounceArrival()
    {
        if (_arrivalAnnounced)
            return;

        _arrivalAnnounced = true;

        if (honkOnArrival)
            PlayHonk();

        pickupToast?.Show(arrivalMessage, 2.5f, 22);
    }

    private void ShowPrompt()
    {
        if (_promptShowing)
            return;

        _promptShowing = true;
        pickupToast?.ShowPersistent(promptMessage, 24);
    }

    private void HidePrompt()
    {
        if (!_promptShowing)
            return;

        _promptShowing = false;
        pickupToast?.Hide();
    }

    private void ShowPlayerCarryVisual(DeliveryReward reward)
    {
        if (playerCarryVisual == null && playerTransform != null)
            playerCarryVisual = playerTransform.GetComponent<PlayerCarryVisualController>();

        if (playerCarryVisual == null && playerTransform != null)
            playerCarryVisual = playerTransform.gameObject.AddComponent<PlayerCarryVisualController>();

        if (playerCarryVisual != null)
        {
            playerCarryVisual.ConfigureCarrySprites(
                playerCarryDownSprites,
                playerCarryUpSprites,
                playerCarryLeftSprites,
                playerCarryRightSprites,
                playerCarryFramesPerSecond);

            Sprite carriedSprite = ResolveCarriedSprite(reward);
            string carriedName = ResolveRewardName(reward);
            if (carriedSprite != null)
                playerCarryVisual.StartCarryingItem(carriedSprite, carriedName, keepUntilCleared: true);
            else
                playerCarryVisual.ShowCarry(carryVisualDuration);
        }
    }

    private Sprite ResolveCarriedSprite(DeliveryReward reward)
    {
        if (reward == null)
            return null;

        if (reward.carriedSpriteOverride != null)
            return reward.carriedSpriteOverride;

        return reward.item != null ? reward.item.icon : null;
    }

    private string ResolveRewardName(DeliveryReward reward)
    {
        if (reward == null || reward.item == null)
            return "delivery";

        if (!string.IsNullOrWhiteSpace(reward.item.displayName))
            return reward.item.displayName;

        return reward.item.name;
    }

    private bool IsPlayerClose()
    {
        if (playerTransform == null)
            return false;

        Collider2D playerCollider = playerTransform.GetComponent<Collider2D>();
        Vector2 playerPoint = playerCollider != null ? playerCollider.bounds.center : playerTransform.position;
        Vector2 vanPoint = GetClosestPoint(playerPoint);
        Vector2 closestPlayerPoint = playerCollider != null ? playerCollider.ClosestPoint(vanPoint) : playerPoint;

        return Vector2.Distance(closestPlayerPoint, vanPoint) <= interactionDistance;
    }

    public Bounds GetTrafficBounds()
    {
        Collider2D vanCollider = GetComponent<Collider2D>();
        if (vanCollider != null)
            return vanCollider.bounds;

        if (vanSpriteRenderer == null)
            vanSpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (vanSpriteRenderer != null)
            return vanSpriteRenderer.bounds;

        return new Bounds(transform.position, Vector3.one);
    }

    public Vector2 GetClosestPoint(Vector2 target)
    {
        Collider2D vanCollider = GetComponent<Collider2D>();
        if (vanCollider != null)
            return vanCollider.ClosestPoint(target);

        Bounds bounds = GetTrafficBounds();
        return bounds.ClosestPoint(target);
    }

    private void ApplyVanSprite()
    {
        if (vanSpriteRenderer == null)
            vanSpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (vanSpriteRenderer == null)
            vanSpriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        if (vanSpriteRenderer != null && vanSprite != null)
        {
            vanSpriteRenderer.sprite = vanSprite;
            vanSpriteRenderer.sortingLayerName = sortingLayerName;
            vanSpriteRenderer.sortingOrder = sortingOrder;
            if (overrideVisualScale)
                vanSpriteRenderer.transform.localScale = visualScale;
        }
    }

    private void SetVanVisible(bool visible)
    {
        if (vanSpriteRenderer == null)
            vanSpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (vanSpriteRenderer != null)
            vanSpriteRenderer.enabled = visible;

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(includeInactive: true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = visible;
        }
    }

    private Vector3 ResolveRoutePosition(Transform routePoint)
    {
        Vector3 position = routePoint != null ? routePoint.position : transform.position;
        position.z = transform.position.z;
        return position;
    }

    private void EnsureAudioSource()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        _audioSource.spatialBlend = 0f;
    }

    private void PlayHonk()
    {
        if (_audioSource == null)
            return;

        AudioClip clip = honkSound != null ? honkSound : CreateFallbackHonk();
        _audioSource.PlayOneShot(clip, honkVolume);
    }

    private static AudioClip CreateFallbackHonk()
    {
        const int sampleRate = 44100;
        const float duration = 0.35f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);
            float tone = Mathf.Sin(360f * Mathf.PI * 2f * t) * 0.7f + Mathf.Sin(480f * Mathf.PI * 2f * t) * 0.3f;
            samples[i] = tone * envelope * 0.65f;
        }

        AudioClip clip = AudioClip.Create("RuntimeDeliveryVanHonk", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void ResolveStaticReferences()
    {
        if (playerTransform == null)
            playerTransform = FindPlayerTransform();

        if (inventoryController == null)
            inventoryController = InventoryController.Instance != null ? InventoryController.Instance : FindFirstObjectByType<InventoryController>();

        if (pickupToast == null)
            pickupToast = FindFirstObjectByType<PickupToastUIToolkit>();

        if (playerCarryVisual == null && playerTransform != null)
            playerCarryVisual = playerTransform.GetComponent<PlayerCarryVisualController>();
    }

    private void ResolveRuntimeReferences()
    {
        if (playerTransform == null)
            playerTransform = FindPlayerTransform();

        if (inventoryController == null)
            inventoryController = InventoryController.Instance != null ? InventoryController.Instance : FindFirstObjectByType<InventoryController>();

        if (pickupToast == null)
            pickupToast = FindFirstObjectByType<PickupToastUIToolkit>();
    }

    private Transform FindPlayerTransform()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        return player != null ? player.transform : null;
    }

    private string SaveKey => $"delivery_van_claimed_{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}_{GetResolvedVanId()}";
    private string ArrivedDayMemoryKey => $"delivery_van_arrived_day_{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}_{GetResolvedVanId()}_{GetCurrentDayIndex()}";
    private string LegacyArrivedDaySaveKey => $"delivery_van_arrived_day_{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}_{GetResolvedVanId()}";

    private string GetResolvedVanId()
    {
        if (!string.IsNullOrWhiteSpace(vanId))
            return vanId;

        return gameObject.scene.name + "_" + name;
    }

    private bool HasBeenClaimed()
    {
        return PlayerPrefs.GetInt(SaveKey, 0) == 1;
    }

    private void MarkClaimed()
    {
        PlayerPrefs.SetInt(SaveKey, 1);
        PlayerPrefs.Save();
    }

    private bool HasArrivedToday()
    {
        return s_arrivedTodayKeys.Contains(ArrivedDayMemoryKey);
    }

    private void MarkArrivedToday()
    {
        s_arrivedTodayKeys.Add(ArrivedDayMemoryKey);
    }

    private int GetCurrentDayIndex()
    {
        if (DayNightCycleNice2D.Instance != null)
            return DayNightCycleNice2D.Instance.CurrentDayIndex;

        return Mathf.Max(0, PlayerPrefs.GetInt("DayNight_DayIndex", 0));
    }

    [ContextMenu("Reset Delivery Van Claim")]
    public void ResetClaimState()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.DeleteKey(LegacyArrivedDaySaveKey);
        PlayerPrefs.Save();
        s_arrivedTodayKeys.Remove(ArrivedDayMemoryKey);

        if (debugLogs)
            Debug.Log($"[DeliveryVanInteraction] Reset claim state for '{name}'. Save key: {SaveKey}");
    }

    [ContextMenu("Reset Claim And Restart Delivery")]
    public void ResetClaimAndRestartDelivery()
    {
        ResetClaimState();
        StartDeliveryForNewDay();
    }

    [ContextMenu("Jump To Entrance Stop")]
    public void JumpToEntranceStop()
    {
        if (entranceStopPoint != null)
            transform.position = ResolveRoutePosition(entranceStopPoint);

        SetVanVisible(true);
        _state = VanState.WaitingForPlayer;
        _arrivalAnnounced = false;
        AnnounceArrival();
    }

    private void OnValidate()
    {
        ApplyVanSprite();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        DrawRoutePoint(spawnPoint, 0.35f);

        Gizmos.color = Color.yellow;
        DrawRoutePoint(entranceStopPoint, 0.45f);

        Gizmos.color = Color.green;
        DrawRoutePoint(exitPoint, 0.35f);

        if (spawnPoint != null && entranceStopPoint != null)
            Gizmos.DrawLine(spawnPoint.position, entranceStopPoint.position);

        if (entranceStopPoint != null && exitPoint != null)
            Gizmos.DrawLine(entranceStopPoint.position, exitPoint.position);
    }

    private static void DrawRoutePoint(Transform point, float radius)
    {
        if (point == null)
            return;

        Gizmos.DrawWireSphere(point.position, radius);
    }
}
