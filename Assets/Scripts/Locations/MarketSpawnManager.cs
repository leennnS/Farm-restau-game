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

        // Find existing player
        var player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[MarketSpawnManager] Player not found!");
            return;
        }

        // Find spawn point
        GameObject spawn = GameObject.Find("MarketSpawnPoint");
        if (spawn == null)
        {
            Debug.LogError("[MarketSpawnManager] MarketSpawnPoint not found in scene!");
            return;
        }

        // Move player to spawn point
        player.transform.position = spawn.transform.position;
        CameraFollowFix.RebindAllCamerasTo(player.transform);

        Debug.Log($"[MarketSpawnManager] Player moved to {spawn.transform.position}");
    }
}
