using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public static class RestaurantSpawnManager
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "RestaurantScene")
            return;

        GameObject runner = new GameObject("_RestaurantSpawnRunner");
        RestaurantSpawnRunner spawnRunner = runner.AddComponent<RestaurantSpawnRunner>();
        spawnRunner.StartSpawn();
    }

    private class RestaurantSpawnRunner : MonoBehaviour
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
                GameObject prefab = Resources.Load<GameObject>("Main Character");
                if (prefab == null)
                {
                    Destroy(gameObject);
                    yield break;
                }

                GameObject spawn = GameObject.Find("RestaurantSpawnPoint");
                GameObject grid = GameObject.Find("Grid");
                if (spawn == null)
                {
                    spawn = new GameObject("RestaurantSpawnPoint");
                    if (grid != null)
                        spawn.transform.SetParent(grid.transform, true);
                }

                player = Object.Instantiate(prefab, spawn.transform.position, Quaternion.identity);
                player.name = prefab.name;
                player.tag = "Player";
                player.transform.SetParent(null);
                player.transform.localScale = prefab.transform.localScale;
                if (grid != null)
                    player.transform.SetParent(grid.transform, worldPositionStays: true);

                PlayerSetupPipeline.PreparePlayerForSceneChange();
            }

            if (player == null)
            {
                Destroy(gameObject);
                yield break;
            }

            if (player.GetComponent<PlayerMovementConstraint>() == null)
                player.AddComponent<PlayerMovementConstraint>();

            CameraFollowFix.RebindAllCamerasTo(player.transform);

            Destroy(gameObject);
        }
    }

}
