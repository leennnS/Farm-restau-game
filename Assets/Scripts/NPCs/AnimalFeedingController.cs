using UnityEngine;
using UnityEngine.SceneManagement;

public class AnimalFeedingController : MonoBehaviour
{
    private static AnimalFeedingController _instance;

    [SerializeField] private KeyCode dropFeedKey = KeyCode.G;
    [SerializeField] private int feedPileServings = 4;
    [SerializeField] private float dropDistanceInFrontOfPlayer = 0.65f;
    [SerializeField] private string[] foodKeywords =
    {
        "seed",
        "wheat",
        "corn",
        "carrot",
        "lettuce",
        "apple",
        "berry"
    };

    private InventoryController _inventory;
    private Transform _player;
    private PickupToastUIToolkit _toast;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null)
            return;

        GameObject go = new GameObject("AnimalFeedingController");
        _instance = go.AddComponent<AnimalFeedingController>();
        Object.DontDestroyOnLoad(go);
    }

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

    private void Update()
    {
        if (!IsFarmScene())
            return;

        if (!Input.GetKeyDown(dropFeedKey))
            return;

        ResolveReferences();
        TryDropFeedPile();
    }

    private void TryDropFeedPile()
    {
        if (_inventory == null || _player == null)
            return;

        ItemDefinition foodItem = FindFoodInHotbar();
        if (foodItem == null)
        {
            if (_toast != null)
                _toast.Show("Put seeds or food in the hotbar to feed animals");
            return;
        }

        if (!_inventory.TryRemoveItem(foodItem, 1))
            return;

        Vector3 dropPosition = _player.position + Vector3.down * dropDistanceInFrontOfPlayer;
        dropPosition.z = 0f;

        AnimalFeedingSpot.Create(dropPosition, feedPileServings);

        if (_toast != null)
            _toast.Show($"Placed feed: {foodItem.displayName}");
    }

    private ItemDefinition FindFoodInHotbar()
    {
        if (_inventory == null)
            return null;

        for (int i = 0; i < InventoryController.HotbarSize; i++)
        {
            ItemDefinition item = _inventory.GetHotbarItem(i);
            if (IsFoodItem(item))
                return item;
        }

        return null;
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

    private void ResolveReferences()
    {
        if (_inventory == null)
            _inventory = InventoryController.Instance != null ? InventoryController.Instance : FindFirstObjectByType<InventoryController>();

        if (_player == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                _player = player.transform;
        }

        if (_toast == null)
            _toast = FindFirstObjectByType<PickupToastUIToolkit>();
    }

    private static bool IsFarmScene()
    {
        string sceneName = SceneManager.GetActiveScene().name ?? string.Empty;
        return sceneName.IndexOf("farm", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
