using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Event-based cooking prompt notification system.
/// - Shows "Press C to start cooking" when an order is placed
/// - Stays visible until the player presses C or all orders are completed
/// - Uses the same queue state source as the order list HUD
/// - Does NOT change core cooking logic
/// </summary>
public class CookingPromptNotification : MonoBehaviour
{
    private static CookingPromptNotification _instance;
    private const string RestaurantSceneName = "RestaurantScene";

    [Header("Input")]
    [SerializeField] private KeyCode cookingKey = KeyCode.C;

    [Header("UI")]
    private PickupToastUIToolkit pickupToast;
    private RestaurantNpcQueueManager restaurantQueueManager;
    private bool isPromptVisible = false;
    private const string CookingPromptMessage = "Press C to start cooking";

    [Header("Debug")]
    [SerializeField] private bool logCookingPromptEvents = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded_Bootstrap;
        SceneManager.sceneLoaded += OnSceneLoaded_Bootstrap;
    }

    private static void OnSceneLoaded_Bootstrap(Scene scene, LoadSceneMode mode)
    {
        if (!IsRestaurantScene(scene.name))
            return;

        if (FindFirstObjectByType<CookingPromptNotification>() != null)
            return;

        GameObject go = new GameObject("CookingPromptNotification");
        go.AddComponent<CookingPromptNotification>();
    }

    private void Awake()
    {
        if (logCookingPromptEvents)
            Debug.Log("[CookingPromptNotification] Awake called");

        if (_instance != null && _instance != this)
        {
            if (logCookingPromptEvents)
                Debug.Log("[CookingPromptNotification] Destroying duplicate instance");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        ResolveRestaurantQueueManager();
        SyncPromptWithCurrentState();

        if (restaurantQueueManager != null)
        {
            if (logCookingPromptEvents)
                Debug.Log("[CookingPromptNotification] Subscribing to RestaurantNpcQueueManager events");
            restaurantQueueManager.OnQueueOrdersChanged += HandleQueueOrdersChanged;
        }
    }

    private void OnDestroy()
    {
        if (restaurantQueueManager != null)
        {
            if (logCookingPromptEvents)
                Debug.Log("[CookingPromptNotification] Unsubscribing from RestaurantNpcQueueManager events");
            restaurantQueueManager.OnQueueOrdersChanged -= HandleQueueOrdersChanged;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (logCookingPromptEvents)
            Debug.Log($"[CookingPromptNotification] Scene loaded: {scene.name}");

        if (!IsRestaurantScene(scene.name))
        {
            HidePrompt();
            pickupToast = null;

            if (restaurantQueueManager != null)
                restaurantQueueManager.OnQueueOrdersChanged -= HandleQueueOrdersChanged;

            restaurantQueueManager = null;
            return;
        }

        ResolveRestaurantQueueManager();
        SyncPromptWithCurrentState();

        if (restaurantQueueManager != null)
            restaurantQueueManager.OnQueueOrdersChanged -= HandleQueueOrdersChanged;

        if (restaurantQueueManager != null)
            restaurantQueueManager.OnQueueOrdersChanged += HandleQueueOrdersChanged;
    }

    private void Update()
    {
        if (!IsRestaurantScene(SceneManager.GetActiveScene().name))
            return;

        // Only handle input here - hide on C key press
        if (isPromptVisible && Input.GetKeyDown(cookingKey))
        {
            if (logCookingPromptEvents)
                Debug.Log("[CookingPromptNotification] Player pressed C, hiding prompt");
            HidePrompt();
        }
    }

    // ============ EVENT HANDLER ============
    private void HandleQueueOrdersChanged(System.Collections.Generic.IReadOnlyList<RestaurantNpcQueueManager.QueueOrderView> orders)
    {
        if (logCookingPromptEvents)
            Debug.Log($"[CookingPromptNotification] HandleOrdersChanged called - {orders.Count} orders active");

        ResolvePickupToast();

        // If orders exist and prompt not visible, show it
        if (orders.Count > 0 && !isPromptVisible)
        {
            if (logCookingPromptEvents)
                Debug.Log("[CookingPromptNotification] Order detected, showing prompt");
            ShowPrompt();
        }
        // If no orders and prompt is visible, hide it
        else if (orders.Count == 0 && isPromptVisible)
        {
            if (logCookingPromptEvents)
                Debug.Log("[CookingPromptNotification] No orders, hiding prompt");
            HidePrompt();
        }
    }

    // ============ PRIVATE HELPERS ============
    private void ResolveRestaurantQueueManager()
    {
        if (restaurantQueueManager != null)
            return;

        restaurantQueueManager = FindFirstObjectByType<RestaurantNpcQueueManager>();
        if (restaurantQueueManager != null && logCookingPromptEvents)
            Debug.Log("[CookingPromptNotification] Found RestaurantNpcQueueManager");

        if (restaurantQueueManager == null && logCookingPromptEvents)
            Debug.LogWarning("[CookingPromptNotification] RestaurantNpcQueueManager NOT FOUND!");
    }

    private void SyncPromptWithCurrentState()
    {
        ResolvePickupToast();

        if (restaurantQueueManager == null)
        {
            HidePrompt();
            return;
        }

        HandleQueueOrdersChanged(restaurantQueueManager.GetQueueOrders());
    }

    private void ResolvePickupToast()
    {
        if (pickupToast != null)
            return;

        pickupToast = FindFirstObjectByType<PickupToastUIToolkit>();
        if (pickupToast != null && logCookingPromptEvents)
            Debug.Log("[CookingPromptNotification] Found PickupToastUIToolkit");

        if (pickupToast == null && logCookingPromptEvents)
            Debug.LogWarning("[CookingPromptNotification] PickupToastUIToolkit NOT FOUND!");
    }

    private void ShowPrompt()
    {
        if (!IsRestaurantScene(SceneManager.GetActiveScene().name))
            return;

        if (pickupToast == null)
        {
            if (logCookingPromptEvents)
                Debug.LogWarning("[CookingPromptNotification] ShowPrompt: pickupToast is NULL");
            return;
        }

        if (logCookingPromptEvents)
            Debug.Log("[CookingPromptNotification] ShowPrompt: Displaying notification");

        isPromptVisible = true;
        pickupToast.Show(CookingPromptMessage, 3.0f, 28);
    }

    private void HidePrompt()
    {
        if (logCookingPromptEvents)
            Debug.Log("[CookingPromptNotification] HidePrompt: Hiding notification");
        isPromptVisible = false;
    }

    private static bool IsRestaurantScene(string sceneName)
    {
        return string.Equals(sceneName, RestaurantSceneName, System.StringComparison.Ordinal);
    }
}
