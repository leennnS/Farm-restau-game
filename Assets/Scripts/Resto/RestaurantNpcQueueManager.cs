using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RestaurantNpcQueueManager : MonoBehaviour
{
    public struct QueueOrderView
    {
        public int queueIndex;
        public string recipeName;
        public int rewardMoney;
        public float remainingTime;
    }

    [Header("Queue Spots (Q0 front -> Q5 back)")]
    [SerializeField] private Transform[] queueSpots;

    [Header("NPC Setup")]
    [SerializeField] private NPCWalker npcPrefab;
    [SerializeField] private NPCWalker[] npcPrefabs;
    [SerializeField] private Transform npcSpawnPoint;
    [SerializeField] private Transform npcTurnPoint;
    [SerializeField] private Transform npcExitPoint;

    [Header("Spawning")]
    [SerializeField] private float respawnDelaySeconds = 60f;
    [SerializeField] private bool spawnFirstNpcImmediately = true;

    [Header("Scene Trigger")]
    [SerializeField] private bool runOnlyInRestaurantScene = true;
    [SerializeField] private string restaurantSceneName = "RestaurantScene";

    [Header("Order Timing")]
    [SerializeField] private bool assignOrdersToAllWaitingCustomers = true;
    [SerializeField] private float fallbackOrderTimeSeconds = 45f;
    [SerializeField] private float timeoutPenaltyPercent = 0.5f;
    [SerializeField] private int minimumTimeoutPenalty = 10;

    [Header("Debug")]
    [SerializeField] private bool logQueueEvents = true;

    [Header("Audio")]
    [SerializeField] private AudioClip orderSound;
    [SerializeField] private AudioClip timerLoopSound;
    [SerializeField] private AudioClip failureSound;

    private AudioSource _audioSource;
    private AudioSource _sfxAudioSource;
    private bool _isTimerLoopPlaying;

    private readonly List<NPCWalker> queue = new List<NPCWalker>(1);
    private readonly HashSet<NPCWalker> activeManagedNpcs = new HashSet<NPCWalker>();
    private readonly Dictionary<NPCWalker, RecipeDefinition> npcOrders = new Dictionary<NPCWalker, RecipeDefinition>();
    private readonly Dictionary<NPCWalker, float> npcRemainingTimes = new Dictionary<NPCWalker, float>();

    private InventoryController inventory;
    private float respawnTimer;
    private bool queueActive;
    private bool waitingForNextNpcSpawn;
    private bool hasWarnedInventoryMissing;
    private bool hasWarnedMissingNpcPrefab;
    private bool hasWarnedMissingSpawnPoint;
    private bool hasWarnedLegacyNpcPrefabIgnored;
    private RecipeDefinition pendingCookedRecipe;
    private NPCWalker frontNpcWithActiveOrder;
    private int nextNpcPrefabIndex;

    public event System.Action<IReadOnlyList<QueueOrderView>> OnQueueOrdersChanged;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        StopTimerLoopSound();
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (inventory != null)
            inventory.OnRecipeCooked -= HandleRecipeCooked;

        inventory = null;
        activeManagedNpcs.Clear();
    }

    private void Start()
    {
        queueActive = IsSceneAllowed();
        respawnTimer = 0f;
        waitingForNextNpcSpawn = false;
        EnsureAudioSource();
        TryBindInventory();

        if (queueSpots == null || queueSpots.Length == 0)
            Debug.LogWarning("[RestaurantNpcQueueManager] Queue spots are not assigned. Assign Q0..Q5 in Inspector.");
        else if (queueSpots[0] == null)
            Debug.LogWarning("[RestaurantNpcQueueManager] Q0 is null. Front-of-queue ordering cannot work.");

        if (npcTurnPoint == null)
            Debug.LogWarning("[RestaurantNpcQueueManager] NPC Turn Point is not assigned. NPCs will walk straight to the queue slot.");

        if (queueActive)
        {
            if (spawnFirstNpcImmediately)
                SpawnNextNpc();
            else
                ScheduleNextNpcSpawn();
        }

        NotifyQueueOrdersChanged();
    }

    private void Update()
    {
        if (!queueActive)
            return;

        if (!IsSceneAllowed())
            return;

        TryBindInventory();
        CleanupQueueReferences();
        EnsureOrdersForWaitingNpcs();
        TickOrderTimers(Time.deltaTime);
        EnsureFrontOrder();
        TickRespawnTimer(Time.deltaTime);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        queueActive = IsSceneAllowed();
        if (queueActive && queue.Count == 0 && !HasAnyActiveNpc())
            ScheduleNextNpcSpawn();
    }

    private bool IsSceneAllowed()
    {
        if (!runOnlyInRestaurantScene)
            return true;

        Scene active = SceneManager.GetActiveScene();
        string activeName = active.name ?? string.Empty;

        if (string.Equals(activeName, restaurantSceneName, StringComparison.OrdinalIgnoreCase))
            return true;

        return activeName.IndexOf("restaurant", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void TryBindInventory()
    {
        InventoryController current = InventoryController.Instance;
        if (current == null)
            current = FindFirstObjectByType<InventoryController>();

        if (current == inventory)
            return;

        if (inventory != null)
            inventory.OnRecipeCooked -= HandleRecipeCooked;

        inventory = current;

        if (inventory != null)
        {
            inventory.OnRecipeCooked += HandleRecipeCooked;
            hasWarnedInventoryMissing = false;
        }
        else if (!hasWarnedInventoryMissing)
        {
            hasWarnedInventoryMissing = true;
            Debug.LogWarning("[RestaurantNpcQueueManager] InventoryController not found. Cannot generate recipe orders yet.");
        }
    }

    private void TickRespawnTimer(float deltaTime)
    {
        if (!waitingForNextNpcSpawn || queue.Count > 0 || HasAnyActiveNpc())
            return;

        if (npcSpawnPoint == null)
        {
            if (!hasWarnedMissingSpawnPoint)
            {
                hasWarnedMissingSpawnPoint = true;
                Debug.LogWarning("[RestaurantNpcQueueManager] NPC Spawn Point is not assigned.");
            }

            return;
        }

        hasWarnedMissingSpawnPoint = false;
        respawnTimer += deltaTime;

        if (respawnTimer < respawnDelaySeconds)
            return;

        respawnTimer = 0f;
        if (SpawnNextNpc())
            waitingForNextNpcSpawn = false;
    }

    private bool SpawnNextNpc()
    {
        if (queue.Count > 0 || HasAnyActiveNpc())
            return false;

        if (queueSpots == null || queueSpots.Length == 0)
            return false;

        NPCWalker prefab = GetNextNpcPrefab();
        if (prefab == null)
        {
            if (!hasWarnedMissingNpcPrefab)
            {
                hasWarnedMissingNpcPrefab = true;
                Debug.LogWarning("[RestaurantNpcQueueManager] No NPC prefabs are assigned.");
            }

            ScheduleNextNpcSpawn();
            return false;
        }

        hasWarnedMissingNpcPrefab = false;

        Transform slot = queueSpots[0];
        if (slot == null)
        {
            Debug.LogWarning("[RestaurantNpcQueueManager] Queue spot Q0 is null. Assign Q0..Q5 in Inspector.");
            ScheduleNextNpcSpawn();
            return false;
        }

        if (npcSpawnPoint == null)
        {
            if (!hasWarnedMissingSpawnPoint)
            {
                hasWarnedMissingSpawnPoint = true;
                Debug.LogWarning("[RestaurantNpcQueueManager] NPC Spawn Point is not assigned.");
            }

            ScheduleNextNpcSpawn();
            return false;
        }

        NPCWalker npc = Instantiate(prefab, npcSpawnPoint.position, Quaternion.identity);
        npc.ConfigureForQueue(this, slot, npcExitPoint, npcTurnPoint);

        queue.Add(npc);
        activeManagedNpcs.Add(npc);

        if (logQueueEvents)
            Debug.Log($"[RestaurantNpcQueueManager] Spawned NPC into slot Q0 using prefab {prefab.name}");

        NotifyQueueOrdersChanged();
        EnsureFrontOrder();
        return true;
    }

    private NPCWalker GetNextNpcPrefab()
    {
        bool hasNpcPrefabArray = npcPrefabs != null && npcPrefabs.Length > 0;
        if (hasNpcPrefabArray)
        {
            for (int i = 0; i < npcPrefabs.Length; i++)
            {
                int index = (nextNpcPrefabIndex + i) % npcPrefabs.Length;
                NPCWalker prefab = npcPrefabs[index];
                if (prefab == null)
                    continue;

                nextNpcPrefabIndex = (index + 1) % npcPrefabs.Length;
                return prefab;
            }

            // Array is configured, so legacy single prefab is intentionally ignored.
            if (npcPrefab != null && !hasWarnedLegacyNpcPrefabIgnored)
            {
                hasWarnedLegacyNpcPrefabIgnored = true;
                Debug.Log("[RestaurantNpcQueueManager] npcPrefabs array is configured. Legacy npcPrefab field is ignored.");
            }

            return null;
        }

        return npcPrefab;
    }

    private bool HasAnyActiveNpc()
    {
        activeManagedNpcs.RemoveWhere(npc => npc == null);
        return activeManagedNpcs.Count > 0;
    }

    private void SpawnNpcIntoQueue(NPCWalker prefab)
    {
        if (prefab == null || npcSpawnPoint == null)
            return;

        if (queue.Count > 0)
            return;

        if (queueSpots == null || queueSpots.Length == 0)
            return;

        Transform slot = queueSpots[0];
        if (slot == null)
        {
            Debug.LogWarning("[RestaurantNpcQueueManager] Queue spot Q0 is null. Assign Q0..Q5 in Inspector.");
            return;
        }

        NPCWalker npc = Instantiate(prefab, npcSpawnPoint.position, Quaternion.identity);
        npc.ConfigureForQueue(this, slot, npcExitPoint, npcTurnPoint);

        queue.Add(npc);

        if (logQueueEvents)
            Debug.Log($"[RestaurantNpcQueueManager] Spawned NPC into slot Q0 using prefab {prefab.name}");

        NotifyQueueOrdersChanged();
        EnsureFrontOrder();
    }

    private void ScheduleNextNpcSpawn()
    {
        waitingForNextNpcSpawn = true;
        respawnTimer = 0f;
    }

    private void EnsureFrontOrder()
    {
        if (queue.Count == 0)
        {
            frontNpcWithActiveOrder = null;
            pendingCookedRecipe = null;
            return;
        }

        NPCWalker front = queue[0];
        if (front == null)
            return;

        if (!front.IsWaitingInQueue)
            return;

        if (!npcOrders.TryGetValue(front, out RecipeDefinition recipe) || recipe == null)
        {
            recipe = PickRandomRecipe();
            if (recipe == null)
                return;

            AssignOrder(front, recipe);

            if (logQueueEvents)
                Debug.Log($"[RestaurantNpcQueueManager] Front NPC order: {recipe.recipeName}");
        }

        frontNpcWithActiveOrder = front;

        NotifyQueueOrdersChanged();
    }

    private void EnsureOrdersForWaitingNpcs()
    {
        if (queue.Count == 0)
            return;

        bool changed = false;

        for (int i = 0; i < queue.Count; i++)
        {
            if (!assignOrdersToAllWaitingCustomers && i > 0)
                break;

            NPCWalker npc = queue[i];
            if (npc == null || !npc.IsWaitingInQueue)
                continue;

            if (npcOrders.TryGetValue(npc, out RecipeDefinition existingRecipe) && existingRecipe != null)
                continue;

            RecipeDefinition recipe = PickRandomRecipe();
            if (recipe == null)
                continue;

            AssignOrder(npc, recipe);
            changed = true;

            if (logQueueEvents)
                Debug.Log($"[RestaurantNpcQueueManager] Assigned order to Q{i}: {recipe.recipeName}");
        }

        if (changed)
            NotifyQueueOrdersChanged();
    }

    private void TickOrderTimers(float deltaTime)
    {
        if (npcRemainingTimes.Count == 0)
        {
            StopTimerLoopSound();
            return;
        }

        if (npcRemainingTimes.Count > 0)
            StartTimerLoopSound();

        bool changed = false;

        for (int i = queue.Count - 1; i >= 0; i--)
        {
            NPCWalker npc = queue[i];
            if (npc == null)
                continue;

            if (!npcOrders.TryGetValue(npc, out RecipeDefinition recipe) || recipe == null)
                continue;

            if (!npcRemainingTimes.TryGetValue(npc, out float remaining))
            {
                npcRemainingTimes[npc] = GetInitialOrderTime(recipe);
                changed = true;
                continue;
            }

            remaining = Mathf.Max(0f, remaining - deltaTime);
            npcRemainingTimes[npc] = remaining;

            if (remaining > 0f)
            {
                changed = true;
                continue;
            }

            HandleOrderTimedOut(npc, recipe, i);
            changed = true;
        }

        if (changed)
            NotifyQueueOrdersChanged();
    }

    private void HandleOrderTimedOut(NPCWalker npc, RecipeDefinition recipe, int queueIndex)
    {
        StopTimerLoopSound();
        PlayFailureSound();

        int penalty = CalculateTimeoutPenalty(recipe);
        bool deducted = false;

        if (penalty > 0 && MoneyManager.Instance != null)
            deducted = MoneyManager.Instance.SpendMoney(penalty);

        if (queueIndex >= 0 && queueIndex < queue.Count)
            queue.RemoveAt(queueIndex);
        else
            queue.Remove(npc);

        npcOrders.Remove(npc);
        npcRemainingTimes.Remove(npc);

        if (frontNpcWithActiveOrder == npc)
            frontNpcWithActiveOrder = null;

        if (npc != null)
            npc.BeginLeavingQueue(queueIndex == 0);

        ReassignQueueSpots();

        if (logQueueEvents)
            Debug.Log($"[RestaurantNpcQueueManager] Q{queueIndex} timed out ({recipe.recipeName}). Penalty: {penalty}");
    }

    private void AssignOrder(NPCWalker npc, RecipeDefinition recipe)
    {
        if (npc == null || recipe == null)
            return;

        npcOrders[npc] = recipe;
        npcRemainingTimes[npc] = GetInitialOrderTime(recipe);
        PlayOrderSound();
    }

    private float GetInitialOrderTime(RecipeDefinition recipe)
    {
        if (recipe == null)
            return fallbackOrderTimeSeconds;

        if (recipe.orderPreparationTime > 0f)
            return recipe.orderPreparationTime;

        return fallbackOrderTimeSeconds;
    }

    private float GetRemainingTime(NPCWalker npc, RecipeDefinition recipe)
    {
        if (npc != null && npcRemainingTimes.TryGetValue(npc, out float remaining))
            return remaining;

        return GetInitialOrderTime(recipe);
    }

    private int CalculateTimeoutPenalty(RecipeDefinition recipe)
    {
        int baseReward = recipe != null ? Mathf.Max(0, recipe.rewardMoney) : 0;
        int percentPenalty = Mathf.RoundToInt(baseReward * Mathf.Max(0f, timeoutPenaltyPercent));
        return Mathf.Max(minimumTimeoutPenalty, percentPenalty);
    }

    private RecipeDefinition PickRandomRecipe()
    {
        if (inventory == null)
            return null;

        RecipeDefinition[] recipes = inventory.GetMenuRecipes();
        if (recipes == null || recipes.Length == 0)
            return null;

        List<RecipeDefinition> valid = new List<RecipeDefinition>(recipes.Length);

        for (int i = 0; i < recipes.Length; i++)
        {
            RecipeDefinition recipe = recipes[i];
            if (recipe == null)
                continue;

            if (!recipe.IsValidForOrder())
                continue;

            valid.Add(recipe);
        }

        if (valid.Count == 0)
            return null;

        return valid[UnityEngine.Random.Range(0, valid.Count)];
    }

    private void HandleRecipeCooked(RecipeDefinition cookedRecipe)
    {
        if (cookedRecipe == null)
            return;

        pendingCookedRecipe = cookedRecipe;
    }

    public bool TryServeFrontCustomerFromInventory(out string message)
    {
        message = string.Empty;

        if (queue.Count == 0)
        {
            message = "No customers in queue.";
            return false;
        }

        NPCWalker front = queue[0];
        if (front == null)
        {
            message = "Front customer missing.";
            return false;
        }

        if (!npcOrders.TryGetValue(front, out RecipeDefinition requestedRecipe) || requestedRecipe == null)
        {
            message = "Front customer has no active order.";
            return false;
        }

        if (inventory == null)
            TryBindInventory();

        if (inventory == null)
        {
            message = "Inventory not available.";
            return false;
        }

        if (requestedRecipe.result == null)
        {
            message = "Ordered recipe has no result item configured.";
            return false;
        }

        // Serve one dish per customer.
        if (!inventory.TryRemoveItem(requestedRecipe.result, 1))
        {
            message = $"You need 1 {requestedRecipe.result.displayName} to serve.";
            return false;
        }

        pendingCookedRecipe = null;
        ServeFrontNpc();
        message = $"Served {requestedRecipe.recipeName}.";
        return true;
    }

    public void TryServeFrontCustomer()
    {
        TryServeFrontCustomerFromInventory(out _);
    }

    private void ServeFrontNpc()
    {
        if (queue.Count == 0)
            return;

        NPCWalker front = queue[0];
        queue.RemoveAt(0);

        RecipeDefinition servedRecipe = null;

        if (front != null)
        {
            npcOrders.TryGetValue(front, out servedRecipe);
            npcOrders.Remove(front);
            npcRemainingTimes.Remove(front);
            // Front customer leaves via the turn-point lane for cleaner pathing.
            front.BeginLeavingQueue(true);
        }

        if (pendingCookedRecipe == servedRecipe)
            pendingCookedRecipe = null;

        if (servedRecipe != null && servedRecipe.rewardMoney > 0 && MoneyManager.Instance != null)
            MoneyManager.Instance.AddMoney(servedRecipe.rewardMoney);

        ReassignQueueSpots();
        NotifyQueueOrdersChanged();

        if (logQueueEvents)
            Debug.Log("[RestaurantNpcQueueManager] Front NPC served, queue advanced.");

        EnsureFrontOrder();
    }

    private void EnsureAudioSource()
    {
        if (_audioSource == null)
            _audioSource = GetComponent<AudioSource>();

        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        if (_sfxAudioSource == null)
        {
            AudioSource[] sources = GetComponents<AudioSource>();
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] != _audioSource)
                {
                    _sfxAudioSource = sources[i];
                    break;
                }
            }

            if (_sfxAudioSource == null)
                _sfxAudioSource = gameObject.AddComponent<AudioSource>();
        }

        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        _audioSource.spatialBlend = 0f;

        _sfxAudioSource.playOnAwake = false;
        _sfxAudioSource.loop = false;
        _sfxAudioSource.spatialBlend = 0f;
    }

    private void PlayOrderSound()
    {
        AudioSource source = _sfxAudioSource != null ? _sfxAudioSource : _audioSource;
        if (source == null || orderSound == null)
            return;

        source.PlayOneShot(orderSound);
    }

    private void PlayFailureSound()
    {
        AudioSource source = _sfxAudioSource != null ? _sfxAudioSource : _audioSource;
        if (source == null || failureSound == null)
            return;

        source.PlayOneShot(failureSound);
    }

    private void StartTimerLoopSound()
    {
        if (_isTimerLoopPlaying || _audioSource == null || timerLoopSound == null)
            return;

        _audioSource.clip = timerLoopSound;
        _audioSource.loop = true;
        _audioSource.Play();
        _isTimerLoopPlaying = true;
    }

    private void StopTimerLoopSound()
    {
        if (!_isTimerLoopPlaying || _audioSource == null)
            return;

        if (_audioSource.clip == timerLoopSound)
            _audioSource.Stop();

        _audioSource.loop = false;

        if (_audioSource.clip == timerLoopSound)
            _audioSource.clip = null;

        _isTimerLoopPlaying = false;
    }

    private void ReassignQueueSpots()
    {
        for (int i = 0; i < queue.Count; i++)
        {
            NPCWalker npc = queue[i];
            if (npc == null)
                continue;

            if (i >= queueSpots.Length)
                continue;

            Transform newSpot = queueSpots[i];
            if (newSpot == null)
                continue;

            npc.SetQueueSpot(newSpot);
        }
    }

    private void CleanupQueueReferences()
    {
        activeManagedNpcs.RemoveWhere(npc => npc == null);

        for (int i = queue.Count - 1; i >= 0; i--)
        {
            if (queue[i] != null)
                continue;

            queue.RemoveAt(i);
        }

        RemoveStaleOrderEntries();

        if (queue.Count == 0)
        {
            frontNpcWithActiveOrder = null;
            return;
        }

        if (frontNpcWithActiveOrder != null && !queue.Contains(frontNpcWithActiveOrder))
            frontNpcWithActiveOrder = null;

        NotifyQueueOrdersChanged();
    }

    private void RemoveStaleOrderEntries()
    {
        List<NPCWalker> staleKeys = new List<NPCWalker>();

        foreach (KeyValuePair<NPCWalker, RecipeDefinition> pair in npcOrders)
        {
            if (pair.Key == null || !queue.Contains(pair.Key))
                staleKeys.Add(pair.Key);
        }

        for (int i = 0; i < staleKeys.Count; i++)
            npcOrders.Remove(staleKeys[i]);

        staleKeys.Clear();

        foreach (KeyValuePair<NPCWalker, float> pair in npcRemainingTimes)
        {
            if (pair.Key == null || !queue.Contains(pair.Key))
                staleKeys.Add(pair.Key);
        }

        for (int i = 0; i < staleKeys.Count; i++)
            npcRemainingTimes.Remove(staleKeys[i]);
    }

    public void NotifyNpcExited(NPCWalker npc)
    {
        if (npc == null)
            return;

        activeManagedNpcs.Remove(npc);
        queue.Remove(npc);
        npcOrders.Remove(npc);
        npcRemainingTimes.Remove(npc);

        if (frontNpcWithActiveOrder == npc)
            frontNpcWithActiveOrder = null;

        ReassignQueueSpots();
        EnsureFrontOrder();
        NotifyQueueOrdersChanged();

        if (queue.Count == 0)
            ScheduleNextNpcSpawn();
    }

    public bool TryGetFrontOrder(out RecipeDefinition recipe, out float remainingTime)
    {
        recipe = null;
        remainingTime = 0f;

        if (queue.Count == 0)
            return false;

        NPCWalker front = queue[0];
        if (front == null)
            return false;

        if (!npcOrders.TryGetValue(front, out recipe) || recipe == null)
            return false;

        remainingTime = GetRemainingTime(front, recipe);
        return true;
    }

    public bool TryGetOrderAtQueueIndex(int queueIndex, out RecipeDefinition recipe, out float remainingTime)
    {
        recipe = null;
        remainingTime = 0f;

        if (queueIndex < 0 || queueIndex >= queue.Count)
            return false;

        NPCWalker npc = queue[queueIndex];
        if (npc == null)
            return false;

        if (!npcOrders.TryGetValue(npc, out recipe) || recipe == null)
            return false;

        remainingTime = GetRemainingTime(npc, recipe);
        return true;
    }

    public void ForceSpawnNow()
    {
        waitingForNextNpcSpawn = true;
        respawnTimer = respawnDelaySeconds;
    }

    public IReadOnlyList<QueueOrderView> GetQueueOrders()
    {
        List<QueueOrderView> entries = new List<QueueOrderView>(queue.Count);

        for (int i = 0; i < queue.Count; i++)
        {
            NPCWalker npc = queue[i];
            if (npc == null)
                continue;

            if (!npcOrders.TryGetValue(npc, out RecipeDefinition recipe) || recipe == null)
                continue;

            entries.Add(new QueueOrderView
            {
                queueIndex = i,
                recipeName = recipe.recipeName,
                rewardMoney = recipe.rewardMoney,
                remainingTime = GetRemainingTime(npc, recipe)
            });
        }

        return entries;
    }

    private void NotifyQueueOrdersChanged()
    {
        OnQueueOrdersChanged?.Invoke(GetQueueOrders());
    }
}
