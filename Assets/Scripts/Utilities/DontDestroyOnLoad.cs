using UnityEngine;
using UnityEngine.SceneManagement;

public class DontDestroyOnLoad : MonoBehaviour
{
    private static DontDestroyOnLoad instance;

    private void Awake()
    {
        // Ensure only one instance of this persistent object exists
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Only move/scale if this object is the player (tagged "Player")
        if (!gameObject.CompareTag("Player")) return;

        // Find a spawn point in the newly loaded scene
        SpawnPoint sp = FindObjectOfType<SpawnPoint>();
        if (sp != null)
        {
            Transform t = transform;
            t.position = sp.transform.position;
            float s = Mathf.Max(0.0001f, sp.playerScale);
            t.localScale = new Vector3(s, s, s);
        }
    }
}
