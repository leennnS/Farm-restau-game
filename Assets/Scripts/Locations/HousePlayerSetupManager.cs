using UnityEngine;
using UnityEngine.SceneManagement;

public class HousePlayerSetupManager
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
            Debug.LogWarning("HousePlayerSetupManager: no GameObject with tag 'Player' found.");
            return;
        }

        // Set player scale (keep z as 1)
        player.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

        // Attach movement constraint component if not present
        var limiter = player.GetComponent<PlayerMovementConstraint>();
        if (limiter == null)
        {
            limiter = player.AddComponent<PlayerMovementConstraint>();
            // Set defaults appropriate for house scene
            limiter.widthMultiplier = 0.45f;
            limiter.heightMultiplier = 0.275f;
        }

        Debug.Log($"HousePlayerSetupManager: configured player '{player.name}' for scene {scene.name}.");
    }
}
