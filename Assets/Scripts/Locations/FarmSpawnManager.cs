using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

/// <summary>
/// Ensures player is correctly restored after leaving MarketScene.
/// This runs only when MarketReturnContext.PendingReturnToFarm is set by MarketExitTrigger.
/// </summary>
public static class FarmSpawnManager
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool fromMarket = MarketReturnContext.PendingReturnToFarm;
        bool fromRestaurant = RestaurantReturnContext.PendingReturnToFarm;
        bool fromHouse = HouseExitTrigger.PendingReturnToFarm;
        if (!fromMarket && !fromRestaurant && !fromHouse)
            return;

        MarketReturnContext.PendingReturnToFarm = false;
        RestaurantReturnContext.PendingReturnToFarm = false;
        HouseExitTrigger.PendingReturnToFarm = false;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning($"[FarmSpawnManager] Player not found in scene '{scene.name}'.");
            return;
        }

        GameObject spawn = null;
        if (fromRestaurant)
        {
            spawn = GameObject.Find("SpawnPointResto");
        }
        else if (fromHouse)
        {
            spawn = GameObject.Find("HouseReturnSpawnPoint");
            if (spawn == null)
            {
                SpawnPoint sp = Object.FindFirstObjectByType<SpawnPoint>();
                if (sp != null) spawn = sp.gameObject;
            }
        }
        else
        {
            // Prefer explicit return spawn markers, then fallback to generic SpawnPoint behavior.
            spawn = GameObject.Find("FarmReturnSpawnPoint");
            if (spawn == null) spawn = GameObject.Find("FarmSpawnPoint");
            if (spawn == null)
            {
                SpawnPoint sp = Object.FindFirstObjectByType<SpawnPoint>();
                if (sp != null) spawn = sp.gameObject;
            }
        }

        if (spawn != null)
        {
            player.transform.position = spawn.transform.position;
        }

        // Interior-only limiter can trap player in open farm maps if carried over.
        PlayerMovementConstraint limiter = player.GetComponent<PlayerMovementConstraint>();
        if (limiter != null)
            Object.Destroy(limiter);

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            // Keep only rotation lock, clear other frozen axes that can cause "can't move".
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.simulated = true;
        }

        // Ensure renderers are enabled if some scene scripts disabled them.
        SpriteRenderer[] renderers = player.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].enabled = true;

        // Ensure movement controller is enabled.
        CharacterController2D controller = player.GetComponent<CharacterController2D>();
        if (controller != null)
            controller.enabled = true;

        RebindCameraToPlayer(player.transform);

        Debug.Log($"[FarmSpawnManager] Market return restored in scene '{scene.name}' at {player.transform.position} scale={player.transform.localScale}");
    }

    private static void RebindCameraToPlayer(Transform playerTransform)
    {
        bool rebound = false;

        CinemachineCamera[] cineCams = Object.FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        for (int i = 0; i < cineCams.Length; i++)
        {
            if (cineCams[i] == null)
                continue;

            cineCams[i].Target.TrackingTarget = playerTransform;
            rebound = true;
        }

        // Fallback if no Cinemachine camera exists/is active.
        Camera mainCam = Camera.main;
        if (!rebound && mainCam != null)
        {
            Vector3 p = playerTransform.position;
            Vector3 c = mainCam.transform.position;
            mainCam.transform.position = new Vector3(p.x, p.y, c.z);
        }
    }
}
