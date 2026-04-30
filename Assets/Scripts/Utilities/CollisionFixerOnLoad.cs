using UnityEngine;
using UnityEngine.SceneManagement;

public static class CollisionFixerOnLoad
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Run for both RestaurantScene and HouseInteriorLITEDEMO to ensure player constraints and Rigidbody settings
        if (scene.name != "RestaurantScene" && scene.name != "HouseInteriorLITEDEMO")
            return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            return;
        }

        // Ensure PlayerMovementConstraint is present
        var constraint = player.GetComponent<PlayerMovementConstraint>();
        if (constraint == null)
        {
            player.AddComponent<PlayerMovementConstraint>();
        }

        // Ensure player Rigidbody2D uses continuous collision and interpolation for reliable physics
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }


    }
}
