using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class CollisionFixerOnLoad : MonoBehaviour
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

        // Create a temporary runner to execute the setup coroutine
        GameObject runner = new GameObject("_CollisionFixerRunner");
        CollisionFixerRunner fixerRunner = runner.AddComponent<CollisionFixerRunner>();
        fixerRunner.StartFix();
    }

    private class CollisionFixerRunner : MonoBehaviour
    {
        internal void StartFix()
        {
            StartCoroutine(RunFix());
        }

        private IEnumerator RunFix()
        {
            // Wait for player setup pipeline to complete
            yield return StartCoroutine(PlayerSetupPipeline.WaitForPlayerSetup(5f));

            GameObject player = PlayerSetupPipeline.GetPlayer();
            if (player == null)
            {
                Debug.LogWarning("[CollisionFixerOnLoad] Player not found after pipeline setup");
                Destroy(gameObject);
                yield break;
            }

            // Ensure PlayerMovementConstraint is present (especially for indoor scenes)
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

            Debug.Log("[CollisionFixerOnLoad] Fixed player collision and rigidbody settings");

            Destroy(gameObject);
        }
    }
}
