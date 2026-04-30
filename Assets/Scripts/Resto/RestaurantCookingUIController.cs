using UnityEngine;
using UnityEngine.SceneManagement;

public class RestaurantCookingUIController : MonoBehaviour
{
    private static RestaurantCookingUIController _instance;

    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.C;

    [Header("Scene Filter")]
    [SerializeField] private bool runOnlyInRestaurantScene = true;
    [SerializeField] private string restaurantSceneName = "RestaurantScene";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<RestaurantCookingUIController>() != null)
            return;

        GameObject go = new GameObject("RestaurantCookingUI");
        go.AddComponent<RestaurantCookingUIController>();
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
        if (!Input.GetKeyDown(toggleKey))
            return;

        if (!IsSceneAllowed())
            return;

        InventoryController inventory = InventoryController.Instance;
        if (inventory == null)
            inventory = FindFirstObjectByType<InventoryController>();

        if (inventory == null)
        {

            return;
        }

        if (inventory.IsCookingOnlyModeOpen)
            inventory.CloseCookingOnlyMode(true);
        else
            inventory.OpenCookingOnlyMode(true);
    }

    private bool IsSceneAllowed()
    {
        if (!runOnlyInRestaurantScene)
            return true;

        Scene active = SceneManager.GetActiveScene();
        string activeName = active.name ?? string.Empty;
        string expectedName = restaurantSceneName ?? string.Empty;

        if (string.Equals(activeName, expectedName, System.StringComparison.OrdinalIgnoreCase))
            return true;

        return activeName.IndexOf("restaurant", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
