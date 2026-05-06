using UnityEngine;
using UnityEngine.SceneManagement;

public class HousePlayerSetupManager
{
    private static float? cachedPlayerSpeed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            if (scene.name == "HouseInteriorLITEDEMO")
                Debug.LogWarning("HousePlayerSetupManager: no GameObject with tag 'Player' found.");

            return;
        }

        CharacterController2D controller = player.GetComponent<CharacterController2D>();
        if (controller == null)
            return;

        if (scene.name == "HouseInteriorLITEDEMO")
        {
            if (!cachedPlayerSpeed.HasValue)
                cachedPlayerSpeed = controller.speed;

            controller.speed = 9f;
        }
        else if (cachedPlayerSpeed.HasValue)
        {
            controller.speed = cachedPlayerSpeed.Value;
            cachedPlayerSpeed = null;
        }

        if (scene.name != "HouseInteriorLITEDEMO")
            return;

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
