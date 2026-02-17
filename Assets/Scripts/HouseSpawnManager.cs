using UnityEngine;
using UnityEngine.SceneManagement;

// This static manager subscribes to scene load events and moves the existing player
// to the HouseSpawnPoint when the "HouseInteriorLITEDEMO" scene is loaded.
public static class HouseSpawnManager
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "HouseInteriorLITEDEMO")
            return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("HouseSpawnManager: Could not find GameObject with tag 'Player'.");
            return;
        }

        // Try to find the spawn point by name. It is expected to exist inside 'Grid'.
        GameObject spawn = GameObject.Find("HouseSpawnPoint");

        if (spawn == null)
        {
            // fallback: try to find under Grid specifically
            GameObject grid = GameObject.Find("Grid");
            if (grid != null)
            {
                Transform sp = grid.transform.Find("HouseSpawnPoint");
                if (sp != null) spawn = sp.gameObject;
            }
        }

        if (spawn == null)
        {
            Debug.LogWarning("HouseSpawnManager: Could not find 'HouseSpawnPoint' in the scene.");
            return;
        }

        // Move the existing player to the spawn position. Do not instantiate or duplicate.
        player.transform.position = spawn.transform.position;
        Debug.Log($"HouseSpawnManager: moved player '{player.name}' to HouseSpawnPoint at {spawn.transform.position}.");
    }
}
