using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Manages player spawning with special handling for first-time entry from the Intro scene.
/// When the player comes from Intro, they spawn in front of the Shed Door instead of their default position.
/// </summary>
public class SpawnManager : MonoBehaviour
{
    private const string ReturnToFarmFromKey = "ReturnToFarmFrom";
    private const string SkipSpawnManagerOnceKey = "SkipSpawnManagerOnce";
    private const string PreferredNonIntroSpawnPointName = "FarmReturnSpawnPoint";

    [SerializeField]
    private Transform shedDoorSpawnPoint;

    [SerializeField]
    private Transform defaultSpawnPoint;

    [SerializeField]
    private string shedDoorSpawnPointName = "ShedDoorSpawnPoint";

    [SerializeField]
    private string defaultSpawnPointName = "DefaultSpawnPoint";

    private const string FromIntroKey = "FromIntroScene";
    private const string ForceShedDoorSpawnOnceKey = "ForceShedDoorSpawnOnce";

    private static SpawnManager _instance;

    public static SpawnManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SpawnManager>();
            }

            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    private void Start()
    {
        SpawnPlayer();
    }

    /// <summary>
    /// Position the player at the appropriate spawn point based on where they came from.
    /// </summary>
    private void SpawnPlayer()
    {
        Transform player = ResolvePlayerTransform();

        if (player == null)
        {
            Debug.LogError("[SpawnManager] Player (CharacterController2D) not found in scene!");
            return;
        }

        if (PlayerPrefs.GetInt(SkipSpawnManagerOnceKey, 0) == 1)
        {
            PlayerPrefs.DeleteKey(SkipSpawnManagerOnceKey);
            PlayerPrefs.Save();
            Debug.Log("[SpawnManager] Skipped one-shot because FarmSpawnManager already set return spawn.");
            return;
        }

        bool hasPersistentReturnMarker = PlayerPrefs.GetInt(ReturnToFarmFromKey, 0) != 0;
        bool isInteriorReturnPending = hasPersistentReturnMarker || MarketReturnContext.PendingReturnToFarm || RestaurantReturnContext.PendingReturnToFarm || HouseExitTrigger.PendingReturnToFarm;
        bool forceShedDoorSpawnOnce = PlayerPrefs.GetInt(ForceShedDoorSpawnOnceKey, 0) == 1;
        bool legacyFromIntro = PlayerPrefs.GetInt(FromIntroKey, 0) == 1;

        // Use the shed-door spawn only for the explicit one-shot intro completion key.
        bool isFromIntro = forceShedDoorSpawnOnce;

        // Interior return flow is handled by FarmSpawnManager. Do not move the player here.
        if (isInteriorReturnPending)
        {
            if (isFromIntro || legacyFromIntro)
            {
                PlayerPrefs.DeleteKey(FromIntroKey);
                PlayerPrefs.DeleteKey(ForceShedDoorSpawnOnceKey);
                PlayerPrefs.Save();
                Debug.Log("[SpawnManager] Cleared stale intro spawn flags because an interior return is pending.");
            }

            Debug.Log("[SpawnManager] Skipped spawn because interior return is pending (handled by FarmSpawnManager).");
            return;
        }

        // Clear legacy intro marker so it cannot force shed spawn in future sessions.
        if (legacyFromIntro && !forceShedDoorSpawnOnce)
        {
            PlayerPrefs.DeleteKey(FromIntroKey);
            PlayerPrefs.Save();
            Debug.Log("[SpawnManager] Cleared legacy FromIntroScene marker (no one-shot shed spawn requested).");
        }

        Transform spawnPoint = isFromIntro ? GetShedDoorSpawnPoint() : GetPreferredNonIntroSpawnPoint();

        if (spawnPoint != null)
        {
            player.position = spawnPoint.position;
            CameraFollowFix.RebindAllCamerasTo(player);
            Debug.Log($"[SpawnManager] Player spawned at {(isFromIntro ? "Shed Door" : "Default Position")}: {spawnPoint.position}");

            if (isFromIntro)
            {
                // If another spawn manager runs later in the frame, re-assert shed spawn once.
                StartCoroutine(EnforcePositionAtEndOfFrame(player, spawnPoint.position));
                StartCoroutine(ForceIntroVisualSync(player, spawnPoint.position, 12));
            }

            // Clear the intro flag so subsequent loads use the default spawn
            PlayerPrefs.DeleteKey(FromIntroKey);
            PlayerPrefs.DeleteKey(ForceShedDoorSpawnOnceKey);
            PlayerPrefs.Save();
        }
        else
        {
            Debug.LogWarning($"[SpawnManager] No spawn point found! Using current position.");
        }
    }

    /// <summary>
    /// Get the Shed Door spawn point. Tries to find by reference, then by name.
    /// </summary>
    private Transform GetShedDoorSpawnPoint()
    {
        if (shedDoorSpawnPoint != null)
            return shedDoorSpawnPoint;

        GameObject shedDoor = GameObject.Find(shedDoorSpawnPointName);
        if (shedDoor != null)
            return shedDoor.transform;

        Debug.LogWarning($"[SpawnManager] Shed Door spawn point '{shedDoorSpawnPointName}' not found!");
        return null;
    }

    /// <summary>
    /// Get the default spawn point. Tries to find by reference, then by name.
    /// </summary>
    private Transform GetDefaultSpawnPoint()
    {
        if (defaultSpawnPoint != null)
            return defaultSpawnPoint;

        GameObject defaultSpawn = GameObject.Find(defaultSpawnPointName);
        if (defaultSpawn != null)
            return defaultSpawn.transform;

        Debug.LogWarning($"[SpawnManager] Default spawn point '{defaultSpawnPointName}' not found!");
        return null;
    }

    /// <summary>
    /// Get preferred spawn point for non-intro entries. Uses FarmReturnSpawnPoint first,
    /// then falls back to configured DefaultSpawnPoint.
    /// </summary>
    private Transform GetPreferredNonIntroSpawnPoint()
    {
        GameObject preferred = GameObject.Find(PreferredNonIntroSpawnPointName);
        if (preferred != null)
            return preferred.transform;

        return GetDefaultSpawnPoint();
    }

    /// <summary>
    /// Public method to manually set spawn points (useful if finding by name fails)
    /// </summary>
    public void SetSpawnPoints(Transform shedDoor, Transform defaultSpawn)
    {
        shedDoorSpawnPoint = shedDoor;
        defaultSpawnPoint = defaultSpawn;
    }

    private System.Collections.IEnumerator EnforcePositionAtEndOfFrame(Transform player, Vector3 targetPosition)
    {
        yield return new WaitForEndOfFrame();
        if (player != null)
        {
            player.position = targetPosition;
            CameraFollowFix.RebindAllCamerasTo(player);
            Debug.Log($"[SpawnManager] Re-asserted shed door spawn at end of frame: {targetPosition}");
        }
    }

    private Transform ResolvePlayerTransform()
    {
        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
            return taggedPlayer.transform;

        return FindFirstObjectByType<CharacterController2D>()?.transform;
    }

    private System.Collections.IEnumerator ForceIntroVisualSync(Transform player, Vector3 targetPosition, int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            if (player == null)
                yield break;

            player.position = targetPosition;
            CameraFollowFix.RebindAllCamerasTo(player);

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Vector3 c = mainCam.transform.position;
                mainCam.transform.position = new Vector3(targetPosition.x, targetPosition.y, c.z);
            }

            yield return null;
        }
    }
}
