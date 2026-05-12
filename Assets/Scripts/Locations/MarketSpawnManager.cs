using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

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

        GameObject runner = new GameObject("_MarketSpawnRunner");
        MarketSpawnRunner spawnRunner = runner.AddComponent<MarketSpawnRunner>();
        spawnRunner.StartSpawn();
    }

    private class MarketSpawnRunner : MonoBehaviour
    {
        internal void StartSpawn()
        {
            StartCoroutine(RunSpawn());
        }

        private IEnumerator RunSpawn()
        {
            yield return null;
            yield return PlayerSetupPipeline.WaitForPlayerSetup(5f);

            GameObject player = PlayerSetupPipeline.GetPlayer();
            if (player == null)
                player = PlayerSetupPipeline.FindPlayerInLoadedScenes();

            if (player == null)
            {
                Destroy(gameObject);
                yield break;
            }

            GameObject spawn = GameObject.Find("MarketSpawnPoint");
            if (spawn == null)
            {
                Destroy(gameObject);
                yield break;
            }

            player.transform.position = spawn.transform.position;
            CameraFollowFix.RebindAllCamerasTo(player.transform);

            Destroy(gameObject);
        }
    }
}
