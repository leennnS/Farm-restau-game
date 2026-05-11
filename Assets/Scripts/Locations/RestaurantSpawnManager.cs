using UnityEngine;
using UnityEngine.SceneManagement;

public static class RestaurantSpawnManager
{
    private static GameObject persistentPlayer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "RestaurantScene")
            return;

        // If persistent player exists, just place at spawn
        if (persistentPlayer != null)
        {
            return;
        }

        // Check scene for existing player (in case we just entered this scene)
        GameObject existing = GameObject.FindWithTag("Player");
        if (existing != null)
        {
            persistentPlayer = existing;
            return;
        }

        // Load prefab
        GameObject prefab = Resources.Load<GameObject>("Main Character");
        if (prefab == null)
        {
            Debug.LogError("RestaurantSpawnManager: Could not find prefab 'Main Character' in Resources.");
            return;
        }

        // Find or create spawn point
        GameObject spawn = GameObject.Find("RestaurantSpawnPoint");
        GameObject grid = GameObject.Find("Grid");
        if (spawn == null)
        {
            spawn = new GameObject("RestaurantSpawnPoint");
            if (grid != null)
                spawn.transform.SetParent(grid.transform, true);
            Debug.LogWarning("RestaurantSpawnManager: 'RestaurantSpawnPoint' not found; created one.");
        }

        // Instantiate player at spawn
        GameObject player = Object.Instantiate(prefab, spawn.transform.position, Quaternion.identity);
        player.name = prefab.name;
        player.tag = "Player";

        // Detach from any parent to prevent inherited scale
        player.transform.SetParent(null);

        // Apply prefab’s original scale
        player.transform.localScale = prefab.transform.localScale;

        // Optional: parent to grid while keeping world scale
        if (grid != null)
            player.transform.SetParent(grid.transform, worldPositionStays: true);

        // Make persistent across scenes
        Object.DontDestroyOnLoad(player);
        persistentPlayer = player;

        // Ensure Rigidbody2D and Collider2D exist
        if (player.GetComponent<Rigidbody2D>() == null)
            Debug.LogWarning("Player has no Rigidbody2D. Add one for physics.");
        if (player.GetComponent<Collider2D>() == null)
            Debug.LogWarning("Player has no Collider2D. Add one for collisions.");

        // Ensure movement constraint
        if (player.GetComponent<PlayerMovementConstraint>() == null)
            player.AddComponent<PlayerMovementConstraint>();

        CameraFollowFix.RebindAllCamerasTo(player.transform);

        Debug.Log($"RestaurantSpawnManager: Instantiated player at {spawn.transform.position} with scale {player.transform.localScale}.");
    }

}
