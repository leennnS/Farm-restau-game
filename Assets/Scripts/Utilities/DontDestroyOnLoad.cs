using UnityEngine;
using UnityEngine.SceneManagement;

public class DontDestroyOnLoad : MonoBehaviour
{
    private static DontDestroyOnLoad instance;
    [SerializeField] private string marketSceneName = "MarketScene";
    [SerializeField] private Vector3 marketSceneScale = new Vector3(1f, 0.75f, 1f);
    [SerializeField] private string houseSceneName = "HouseInteriorLITEDEMO";

    private Vector3 originalScale; // Store original player scale

    private void Awake()
    {
        // Singleton pattern — only one player exists
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log($"[DontDestroyOnLoad] Awake - Attached to '{gameObject.name}' in scene '{gameObject.scene.name}'");

        // Store the player's original scale
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MAIN MENU" || scene.name == "MainMenu")
        {
            Destroy(gameObject);
            return;
        }

        if (scene.name == marketSceneName)
        {
            transform.localScale = marketSceneScale;
            return;
        }

        if (scene.name == houseSceneName)
        {
            transform.localScale = new Vector3(0.6f, 0.4f, 1f);
            return;
        }

        RestoreOriginalScale();
    }

    public void RestoreOriginalScale()
    {
        transform.localScale = originalScale;
    }
}
