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
    [SerializeField] private Transform npcSpawnPoint;
    [SerializeField] private Transform npcTurnPoint;
    [SerializeField] private Transform npcExitPoint;

    [Header("Spawning")]
    [SerializeField] private float spawnIntervalSeconds = 120f;
    [SerializeField] private int maxActiveNpcs = 1;
    [SerializeField] private bool spawnFirstNpcImmediately = true;

    [Header("Scene Trigger")]
    [SerializeField] private bool runOnlyInRestaurantScene = true;
    [SerializeField] private string restaurantSceneName = "RestaurantScene";

    [Header("UI")]
    [SerializeField] private RestaurantOrderPopupUI orderPopupUI;

    [Header("Order Timing")]
    [SerializeField] private bool assignOrdersToAllWaitingCustomers = true;
    [SerializeField] private float fallbackOrderTimeSeconds = 45f;
    [SerializeField] private float timeoutPenaltyPercent = 0.5f;
    [SerializeField] private int minimumTimeoutPenalty = 10;

    [Header("Debug")]
    [SerializeField] private bool logQueueEvents = true;

    private readonly List<NPCWalker> queue = new List<NPCWalker>();
    private readonly Dictionary<NPCWalker, RecipeDefinition> npcOrders = new Dictionary<NPCWalker, RecipeDefinition>();
    private readonly Dictionary<NPCWalker, float> npcRemainingTimes = new Dictionary<NPCWalker, float>();

    private InventoryController inventory;
    private float spawnTimer;
    private bool queueActive;
    private bool hasWarnedPopupMissing;
    private bool hasWarnedInventoryMissing;
    private bool hasWarnedMissingNpcPrefab;
    private bool hasWarnedMissingSpawnPoint;
    private bool hasWarnedInvalidMaxActive;
    private RecipeDefinition pendingCookedRecipe;

    private NPCWalker frontNpcWithActiveOrder;

    public event System.Action<IReadOnlyList<QueueOrderView>> OnQueueOrdersChanged;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (orderPopupUI != null)
            orderPopupUI.OnServePressed += TryServeFrontCustomer;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (orderPopupUI != null)
            orderPopupUI.OnServePressed -= TryServeFrontCustomer;

        if (inventory != null)
            inventory.OnRecipeCooked -= HandleRecipeCooked;

        inventory = null;
    }

    private void Start()
    {
        queueActive = IsSceneAllowed();
        spawnTimer = 0f;
        TryBindInventory();

        if (orderPopupUI == null)
            Debug.LogWarning("[RestaurantNpcQueueManager] Order popup UI is not assigned. Orders can be generated but popup will not display.");

        if (queueSpots == null || queueSpots.Length == 0)
            Debug.LogWarning("[RestaurantNpcQueueManager] Queue spots are not assigned. Assign Q0..Q5 in Inspector.");
        else if (queueSpots[0] == null)
            Debug.LogWarning("[RestaurantNpcQueueManager] Q0 is null. Front-of-queue ordering cannot work.");

        if (npcTurnPoint == null)
            Debug.LogWarning("[RestaurantNpcQueueManager] NPC Turn Point is not assigned. NPCs will walk straight to the queue slot.");

        // Spawn first customer immediately if requested.
        if (queueActive && spawnFirstNpcImmediately)
            SpawnNpcIntoQueue();

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
        TickSpawner(Time.deltaTime);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        queueActive = IsSceneAllowed();
        if (queueActive)
        {
            TryBindInventory();
            EnsureFrontOrder();
        }
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

    private void TickSpawner(float deltaTime)
    {
        if (npcPrefab == null || npcSpawnPoint == null)
        {
            if (npcPrefab == null && !hasWarnedMissingNpcPrefab)
            {
                hasWarnedMissingNpcPrefab = true;
                Debug.LogWarning("[RestaurantNpcQueueManager] NPC Prefab is not assigned.");
            }

            if (npcSpawnPoint == null && !hasWarnedMissingSpawnPoint)
            {
                hasWarnedMissingSpawnPoint = true;
                Debug.LogWarning("[RestaurantNpcQueueManager] NPC Spawn Point is not assigned.");
            }

            return;
        }

        hasWarnedMissingNpcPrefab = false;
        hasWarnedMissingSpawnPoint = false;

        if (queueSpots == null || queueSpots.Length == 0)
            return;

        if (maxActiveNpcs <= 0)
        {
            if (!hasWarnedInvalidMaxActive)
            {
                hasWarnedInvalidMaxActive = true;
                Debug.LogWarning("[RestaurantNpcQueueManager] Max Active NPCs is 0 or less. Increase it to spawn customers.");
            }

            return;
        }

        hasWarnedInvalidMaxActive = false;

        if (queue.Count >= Mathf.Min(maxActiveNpcs, queueSpots.Length))
            return;

        spawnTimer += deltaTime;

        if (spawnTimer < spawnIntervalSeconds)
            return;

        spawnTimer = 0f;
        SpawnNpcIntoQueue();
    }

    private void SpawnNpcIntoQueue()
    {
        int targetSlot = queue.Count;
        if (targetSlot >= queueSpots.Length)
            return;

        Transform slot = queueSpots[targetSlot];
        if (slot == null)
        {
            Debug.LogWarning("[RestaurantNpcQueueManager] Queue spot is null. Assign Q0..Q5 in Inspector.");
            return;
        }

        NPCWalker npc = Instantiate(npcPrefab, npcSpawnPoint.position, Quaternion.identity);
        npc.ConfigureForQueue(this, slot, npcExitPoint, npcTurnPoint);

        queue.Add(npc);

        if (logQueueEvents)
            Debug.Log($"[RestaurantNpcQueueManager] Spawned NPC into slot Q{targetSlot}");

        NotifyQueueOrdersChanged();
        EnsureFrontOrder();
    }

    private void EnsureFrontOrder()
    {
        if (queue.Count == 0)
        {
            frontNpcWithActiveOrder = null;
            pendingCookedRecipe = null;
            orderPopupUI?.Hide();
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

        if (orderPopupUI == null)
        {
            if (!hasWarnedPopupMissing)
            {
                hasWarnedPopupMissing = true;
                Debug.LogWarning("[RestaurantNpcQueueManager] ShowOrder skipped because orderPopupUI is null.");
            }
        }
        else
        {
            hasWarnedPopupMissing = false;
        }

        float remainingTime = GetRemainingTime(front, recipe);
        bool canServeNow = pendingCookedRecipe != null && pendingCookedRecipe == recipe;
        orderPopupUI?.ShowOrder(recipe, remainingTime, canServeNow);

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
            return;

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

        if (pendingCookedRecipe == recipe)
            pendingCookedRecipe = null;

        if (frontNpcWithActiveOrder == npc)
            frontNpcWithActiveOrder = null;

        if (npc != null)
            npc.BeginLeavingQueue(queueIndex == 0);

        ReassignQueueSpots();

        string status = deducted
            ? $"Order timed out. -{penalty} money"
            : "Order timed out";
        orderPopupUI?.ShowServedMessage(status);

        if (logQueueEvents)
            Debug.Log($"[RestaurantNpcQueueManager] Q{queueIndex} timed out ({recipe.recipeName}). Penalty: {penalty}");
    }

    private void AssignOrder(NPCWalker npc, RecipeDefinition recipe)
    {
        if (npc == null || recipe == null)
            return;

        npcOrders[npc] = recipe;
        npcRemainingTimes[npc] = GetInitialOrderTime(recipe);
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

        if (queue.Count == 0)
        {
            orderPopupUI?.ShowServedMessage($"{cookedRecipe.recipeName} is ready");
            return;
        }

        NPCWalker front = queue[0];
        if (front == null)
            return;

        if (!npcOrders.TryGetValue(front, out RecipeDefinition requestedRecipe) || requestedRecipe == null)
            return;

        if (requestedRecipe == cookedRecipe)
        {
            float remainingTime = GetRemainingTime(front, requestedRecipe);
            orderPopupUI?.ShowOrder(requestedRecipe, remainingTime, true);

            if (logQueueEvents)
                Debug.Log($"[RestaurantNpcQueueManager] Cooked dish matches front order: {cookedRecipe.recipeName}. Waiting for manual serve.");

            return;
        }

        orderPopupUI?.ShowServedMessage($"{cookedRecipe.recipeName} cooked. Wrong dish for front customer.");

        if (logQueueEvents)
            Debug.Log($"[RestaurantNpcQueueManager] Cooked dish {cookedRecipe.recipeName} does not match front order {requestedRecipe.recipeName}.");
    }

    public void TryServeFrontCustomer()
    {
        if (queue.Count == 0)
            return;

        NPCWalker front = queue[0];
        if (front == null)
            return;

        if (!npcOrders.TryGetValue(front, out RecipeDefinition requestedRecipe) || requestedRecipe == null)
            return;

        if (pendingCookedRecipe == null)
        {
            orderPopupUI?.ShowServedMessage("No cooked dish ready to serve");
            return;
        }

        if (pendingCookedRecipe != requestedRecipe)
        {
            orderPopupUI?.ShowServedMessage("Wrong dish for front customer");
            return;
        }

        pendingCookedRecipe = null;
        ServeFrontNpc();
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
        if (servedRecipe != null)
            orderPopupUI?.ShowServedMessage($"Served {servedRecipe.recipeName} (+{servedRecipe.rewardMoney})");
        else
            orderPopupUI?.ShowServedMessage("Order served");

        NotifyQueueOrdersChanged();

        if (logQueueEvents)
            Debug.Log("[RestaurantNpcQueueManager] Front NPC served, queue advanced.");

        EnsureFrontOrder();
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

        queue.Remove(npc);
        npcOrders.Remove(npc);
        npcRemainingTimes.Remove(npc);

        if (frontNpcWithActiveOrder == npc)
            frontNpcWithActiveOrder = null;

        ReassignQueueSpots();
        EnsureFrontOrder();
        NotifyQueueOrdersChanged();
    }

    public void ForceSpawnNow()
    {
        spawnTimer = spawnIntervalSeconds;
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
