using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OrderManager : MonoBehaviour
{
    private static OrderManager _instance;
    public static OrderManager Instance => _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<OrderManager>() == null)
        {
            GameObject managerGo = new GameObject("OrderManager");
            managerGo.AddComponent<OrderManager>();
        }

        if (FindFirstObjectByType<OrderListHUD>() == null)
        {
            GameObject hudGo = new GameObject("OrderListHUD");
            hudGo.AddComponent<OrderListHUD>();
        }
    }

    [Header("Order Settings")]
    [SerializeField] private int maxActiveOrders = 3;

    [Header("Scene Filter")]
    [SerializeField] private bool runOnlyInRestaurantScene = false;
    [SerializeField] private string restaurantSceneName = "Restaurant";

    [Header("Debug")]
    [SerializeField] private bool logOrderEvents = true;

    private readonly List<Order> _activeOrders = new List<Order>();
    private InventoryController _inventory;

    public IReadOnlyList<Order> ActiveOrders => _activeOrders;

    public event Action<IReadOnlyList<Order>> OnOrdersChanged;
    public event Action<Order> OnOrderCompleted;
    public event Action<Order> OnOrderFailed;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        EnsureInventoryHooked();
    }

    private void Update()
    {
        if (!ShouldRunOrderLogic())
            return;

        EnsureInventoryHooked();
        UpdateOrderTimers(Time.deltaTime);
    }

    private void OnDisable()
    {
        UnhookInventory();
    }

    private void EnsureInventoryHooked()
    {
        InventoryController current = InventoryController.Instance;
        if (current == null)
            current = FindFirstObjectByType<InventoryController>();

        if (current == _inventory)
            return;

        UnhookInventory();
        _inventory = current;

        if (_inventory != null)
            _inventory.OnRecipeCooked += HandleRecipeCooked;
    }

    private void UnhookInventory()
    {
        if (_inventory != null)
            _inventory.OnRecipeCooked -= HandleRecipeCooked;

        _inventory = null;
    }

    public void FillOrdersToMax()
    {
        int target = Mathf.Max(1, maxActiveOrders);
        while (_activeOrders.Count < target)
        {
            if (!CreateRandomOrderFromMenu())
                break;
        }

        NotifyOrdersChanged();
    }

    public bool CreateOrder(RecipeDefinition recipe)
    {
        if (recipe == null)
            return false;

        int target = Mathf.Max(1, maxActiveOrders);
        if (_activeOrders.Count >= target)
            return false;

        if (!recipe.IsValidForOrder())
        {

            return false;
        }

        for (int i = 0; i < _activeOrders.Count; i++)
        {
            if (_activeOrders[i].recipe == recipe)
                return false;
        }

        Order order = new Order(recipe);
        _activeOrders.Add(order);

        if (logOrderEvents)


            NotifyOrdersChanged();
        return true;
    }

    public bool CreateRandomOrderFromMenu()
    {
        if (_inventory == null)
            return false;

        int target = Mathf.Max(1, maxActiveOrders);
        if (_activeOrders.Count >= target)
            return false;

        RecipeDefinition[] menu = _inventory.GetMenuRecipes();
        if (menu == null || menu.Length == 0)
            return false;

        List<RecipeDefinition> candidates = new List<RecipeDefinition>(menu.Length);
        for (int i = 0; i < menu.Length; i++)
        {
            RecipeDefinition recipe = menu[i];
            if (recipe == null)
                continue;

            if (!recipe.IsValidForOrder())
            {

                continue;
            }

            bool alreadyActive = false;
            for (int j = 0; j < _activeOrders.Count; j++)
            {
                if (_activeOrders[j].recipe == recipe)
                {
                    alreadyActive = true;
                    break;
                }
            }

            if (!alreadyActive)
                candidates.Add(recipe);
        }

        if (candidates.Count == 0)
            return false;

        RecipeDefinition selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        return CreateOrder(selected);
    }

    private void UpdateOrderTimers(float deltaTime)
    {
        if (_activeOrders.Count == 0)
            return;

        bool changed = false;

        for (int i = _activeOrders.Count - 1; i >= 0; i--)
        {
            Order order = _activeOrders[i];
            float before = order.remainingTime;
            order.Tick(deltaTime);

            if (!Mathf.Approximately(before, order.remainingTime))
                changed = true;

            if (!order.IsExpired)
                continue;

            _activeOrders.RemoveAt(i);

            if (order.penaltyMoney > 0)
                MoneyManager.Instance.SpendMoney(order.penaltyMoney);

            if (logOrderEvents)


                OnOrderFailed?.Invoke(order);
            changed = true;
        }

        if (changed)
            NotifyOrdersChanged();
    }

    private void HandleRecipeCooked(RecipeDefinition cookedRecipe)
    {
        if (!ShouldRunOrderLogic())
            return;

        if (cookedRecipe == null || _activeOrders.Count == 0)
            return;

        for (int i = 0; i < _activeOrders.Count; i++)
        {
            Order order = _activeOrders[i];
            if (order.recipe != cookedRecipe)
                continue;

            _activeOrders.RemoveAt(i);
            if (order.rewardMoney > 0)
                MoneyManager.Instance.AddMoney(order.rewardMoney);

            if (logOrderEvents)


                OnOrderCompleted?.Invoke(order);
            NotifyOrdersChanged();
            return;
        }
    }

    private bool ShouldRunOrderLogic()
    {
        if (!runOnlyInRestaurantScene)
            return true;

        Scene activeScene = SceneManager.GetActiveScene();
        return string.Equals(activeScene.name, restaurantSceneName, StringComparison.Ordinal);
    }

    private void NotifyOrdersChanged()
    {
        OnOrdersChanged?.Invoke(_activeOrders);
    }
}
