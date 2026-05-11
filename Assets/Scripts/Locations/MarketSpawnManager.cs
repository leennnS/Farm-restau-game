using UnityEngine;
using UnityEngine.SceneManagement;

public static class MarketSpawnManager
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "MarketScene")
            return;

        Debug.Log($"[MarketSpawnManager] OnSceneLoaded called at time {Time.time:F2}s");

        // Find existing player
        var player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[MarketSpawnManager] Player not found!");
            return;
        }

        Debug.Log($"[MarketSpawnManager] Found player: {player.name} at position {player.transform.position}, active={player.activeInHierarchy}");

        // Find spawn point
        GameObject spawn = GameObject.Find("MarketSpawnPoint");
        if (spawn == null)
        {
            Debug.LogError("[MarketSpawnManager] MarketSpawnPoint not found in scene!");
            return;
        }

        // Move player to spawn point
        player.transform.position = spawn.transform.position;
        Debug.Log($"[MarketSpawnManager] Moved player to spawn point: {spawn.transform.position}");

        Debug.Log("[MarketSpawnManager] Calling CameraFollowFix.RebindAllCamerasTo");
        CameraFollowFix.RebindAllCamerasTo(player.transform);

        Debug.Log($"[MarketSpawnManager] Player repositioned and camera rebound");
    }
}
