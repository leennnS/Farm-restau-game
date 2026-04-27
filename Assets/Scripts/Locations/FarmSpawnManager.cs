using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

/// <summary>
/// Ensures player is correctly restored after leaving MarketScene.
/// This runs only when MarketReturnContext.PendingReturnToFarm is set by MarketExitTrigger.
/// </summary>
public static class FarmSpawnManager
{
    private const string ReturnToFarmFromKey = "ReturnToFarmFrom";
    private const string SkipSpawnManagerOnceKey = "SkipSpawnManagerOnce";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        int returnSource = PlayerPrefs.GetInt(ReturnToFarmFromKey, 0); // 0 None, 1 Market, 2 Restaurant, 3 House

        bool fromMarket = returnSource == 1 || MarketReturnContext.PendingReturnToFarm;
        bool fromRestaurant = returnSource == 2 || RestaurantReturnContext.PendingReturnToFarm;
        bool fromHouse = returnSource == 3 || HouseExitTrigger.PendingReturnToFarm;

        bool introSpawnRequested = PlayerPrefs.GetInt("ForceShedDoorSpawnOnce", 0) == 1;
        bool legacyFromIntro = PlayerPrefs.GetInt("FromIntroScene", 0) == 1;

        // If we're explicitly returning from an interior, that spawn should always win over intro shed spawn.
        if (fromMarket || fromRestaurant || fromHouse)
        {
            if (introSpawnRequested || legacyFromIntro)
            {
                PlayerPrefs.DeleteKey("FromIntroScene");
                PlayerPrefs.DeleteKey("ForceShedDoorSpawnOnce");
                PlayerPrefs.Save();
            }
        }
        // Intro-to-farm flow is handled by SpawnManager only when no interior return is pending.
        else if (introSpawnRequested)
        {
            MarketReturnContext.PendingReturnToFarm = false;
            RestaurantReturnContext.PendingReturnToFarm = false;
            HouseExitTrigger.PendingReturnToFarm = false;
            return;
        }
        else if (legacyFromIntro)
        {
            // Legacy marker can be left by older intro scripts; clear it to avoid stale behavior.
            PlayerPrefs.DeleteKey("FromIntroScene");
            PlayerPrefs.Save();
        }

        if (!fromMarket && !fromRestaurant && !fromHouse)
            return;

        MarketReturnContext.PendingReturnToFarm = false;
        RestaurantReturnContext.PendingReturnToFarm = false;
        HouseExitTrigger.PendingReturnToFarm = false;

        // SpawnManager runs later in scene Start and can override this return position.
        // Mark one-shot skip so the return spawn selected here remains final.
        PlayerPrefs.SetInt(SkipSpawnManagerOnceKey, 1);
        PlayerPrefs.DeleteKey(ReturnToFarmFromKey);
        PlayerPrefs.Save();

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

        string sourceName = fromRestaurant ? "Restaurant" : fromHouse ? "House" : "Market";
        Debug.Log($"[FarmSpawnManager] Restored return from {sourceName} in scene '{scene.name}' at {player.transform.position} scale={player.transform.localScale}");
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
