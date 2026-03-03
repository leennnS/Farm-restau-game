using UnityEngine;
using UnityEngine.SceneManagement;

public static class EnforcePlayerScaleOnLoad
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Scenes where we want the player scaled down
        if (scene.name != "HouseInteriorLITEDEMO" && scene.name != "RestaurantScene")
            return;

        var player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning($"EnforcePlayerScaleOnLoad: No GameObject with tag 'Player' found when scene '{scene.name}' loaded.");
            return;
        }

        Vector3 desired = new Vector3(0.5f, 0.5f, 1f);
        // Add a temporary component to enforce scale for a short duration to handle race conditions
        var fixer = player.GetComponent<PlayerScaleFixer>();
        if (fixer == null) fixer = player.AddComponent<PlayerScaleFixer>();
        fixer.targetScale = desired;
        fixer.duration = 0.75f;
        Debug.Log($"EnforcePlayerScaleOnLoad: Enforcing player '{player.name}' scale to {desired} for scene '{scene.name}'.");
    }
}
