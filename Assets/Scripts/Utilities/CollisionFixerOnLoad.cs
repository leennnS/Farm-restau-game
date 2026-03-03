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
            Debug.LogWarning($"CollisionFixerOnLoad: no Player (tag 'Player') found in scene {scene.name}.");
            return;
        }

        // Ensure PlayerMovementConstraint is present
        var constraint = player.GetComponent<PlayerMovementConstraint>();
        if (constraint == null)
        {
            player.AddComponent<PlayerMovementConstraint>();
            Debug.Log("CollisionFixerOnLoad: Added PlayerMovementConstraint to player.");
        }

        // Ensure player Rigidbody2D uses continuous collision and interpolation for reliable physics
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            Debug.Log("CollisionFixerOnLoad: Configured Rigidbody2D collisionDetectionMode=Continuous and interpolation=Interpolate.");
        }
        else
        {
            Debug.LogWarning("CollisionFixerOnLoad: Player has no Rigidbody2D; collisions may not behave as expected.");
        }

        // Ensure all Collider2D under Grid are non-trigger at runtime (so they block movement)
        GameObject grid = GameObject.Find("Grid");
        if (grid != null)
        {
            var cols = grid.GetComponentsInChildren<Collider2D>(true);
            int changed = 0;
            foreach (var c in cols)
            {
                if (c.isTrigger)
                {
                    c.isTrigger = false; // runtime change only
                    changed++;
                }
            }
            if (changed > 0)
                Debug.Log($"CollisionFixerOnLoad: set isTrigger=false on {changed} Collider2D(s) under Grid.");
        }
    }
}
