using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class HousePlayerSetupManager : MonoBehaviour
{
    private static float? cachedPlayerSpeed;
    private static Coroutine setupCoroutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Create a temporary runner to execute the setup coroutine
        GameObject runner = new GameObject("_HousePlayerSetupRunner");
        HouseSetupRunner setupRunner = runner.AddComponent<HouseSetupRunner>();
        setupRunner.StartSetup(scene.name);
    }

    private class HouseSetupRunner : MonoBehaviour
    {
        internal void StartSetup(string sceneName)
        {
            StartCoroutine(RunSetup(sceneName));
        }

        private IEnumerator RunSetup(string sceneName)
        {
            // Give one frame for scene to fully load
            yield return null;

            yield return PlayerSetupPipeline.WaitForPlayerSetup(5f);

            GameObject player = PlayerSetupPipeline.GetPlayer();
            if (player == null)
                player = PlayerSetupPipeline.FindPlayerInLoadedScenes();

            if (player == null)
            {
                if (sceneName == "HouseInteriorLITEDEMO")
                    Debug.LogWarning("[HousePlayerSetupManager] Player not found after scene load");

                Destroy(gameObject);
                yield break;
            }

            CharacterController2D controller = player.GetComponent<CharacterController2D>();
            if (controller == null)
            {
                Destroy(gameObject);
                yield break;
            }

            if (sceneName == "HouseInteriorLITEDEMO")
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

            if (sceneName != "HouseInteriorLITEDEMO")
            {
                Destroy(gameObject);
                yield break;
            }

            // Attach movement constraint component if not present
            var limiter = player.GetComponent<PlayerMovementConstraint>();
            if (limiter == null)
            {
                limiter = player.AddComponent<PlayerMovementConstraint>();
                // Set defaults appropriate for house scene
                limiter.widthMultiplier = 0.45f;
                limiter.heightMultiplier = 0.275f;
            }

            CameraFollowFix.RebindAllCamerasTo(player.transform);

            Debug.Log($"[HousePlayerSetupManager] Configured player '{player.name}' for scene {sceneName}");

            Destroy(gameObject);
        }
    }
}
