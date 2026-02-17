using UnityEngine;
using UnityEngine.SceneManagement;

public static class RestaurantSpawnManager
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "RestaurantScene")
            return;

        // If a player already exists (tagged Player), don't instantiate another
        var existing = GameObject.FindWithTag("Player");
        if (existing != null)
        {
            // Ensure scale and movement constraint are set
            SetupExistingPlayer(existing);
            Debug.Log("RestaurantSpawnManager: Player already exists, configured existing instance.");
            return;
        }

        // Load the player prefab from Resources. Place the prefab named exactly 'Main Character' under a Resources folder.
        GameObject prefab = Resources.Load<GameObject>("Main Character");
        if (prefab == null)
        {
            Debug.LogError("RestaurantSpawnManager: Could not find prefab 'Main Character' in Resources. Please place the player prefab in a Resources folder and name it 'Main Character'.");
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
            Debug.LogWarning("RestaurantSpawnManager: 'RestaurantSpawnPoint' not found; created one. Move it where you want the player to spawn.");
        }

        // Determine desired scale: if spawn has a non-default localScale, use it; otherwise default to (0.5,0.5,1)
        Vector3 desiredScale = spawn.transform.localScale != Vector3.one ? spawn.transform.localScale : new Vector3(0.5f, 0.5f, 1f);

        // Instantiate player prefab
        GameObject player = GameObject.Instantiate(prefab, spawn.transform.position, Quaternion.identity);
        player.name = prefab.name; // keep prefab name
        player.tag = "Player";

        // Apply scale
        player.transform.localScale = desiredScale;

        // Keep player across scenes if you want persistence. Comment out if not desired.
        Object.DontDestroyOnLoad(player);

        // Ensure player has Rigidbody2D and Collider2D (warn but do not add)
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb == null)
            Debug.LogWarning("RestaurantSpawnManager: Instantiated player has no Rigidbody2D. Add one to enable proper physics collisions.");

        var col = player.GetComponent<Collider2D>();
        if (col == null)
            Debug.LogWarning("RestaurantSpawnManager: Instantiated player has no Collider2D. Add one to enable proper collisions with floor and walls.");

        // Attach movement constraint so player can't leave the Grid/floor
        var constraint = player.GetComponent<PlayerMovementConstraint>();
        if (constraint == null)
            player.AddComponent<PlayerMovementConstraint>();

        Debug.Log($"RestaurantSpawnManager: Instantiated player at {spawn.transform.position} with scale {desiredScale}.");
    }

    static void SetupExistingPlayer(GameObject player)
    {
        // Set scale to spawn scale if point exists
        GameObject spawn = GameObject.Find("RestaurantSpawnPoint");
        if (spawn != null)
        {
            if (spawn.transform.localScale != Vector3.one)
                player.transform.localScale = spawn.transform.localScale;
            // Optionally move player to spawn if desired: we leave position unchanged per requirement
        }

        if (player.GetComponent<PlayerMovementConstraint>() == null)
            player.AddComponent<PlayerMovementConstraint>();
    }
}
