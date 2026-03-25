using UnityEngine;
using UnityEngine.SceneManagement;

public class DontDestroyOnLoad : MonoBehaviour
{
    private static DontDestroyOnLoad instance;
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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
    }

    public void RestoreOriginalScale()
    {
        transform.localScale = originalScale;
    }
}