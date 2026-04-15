using UnityEngine;

/// <summary>
/// Manages player spawning with special handling for first-time entry from the Intro scene.
/// When the player comes from Intro, they spawn in front of the Shed Door instead of their default position.
/// </summary>
public class SpawnManager : MonoBehaviour
{
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
        Transform player = FindFirstObjectByType<CharacterController2D>()?.transform;

        if (player == null)
        {
            Debug.LogError("[SpawnManager] Player (CharacterController2D) not found in scene!");
            return;
        }

        bool isFromIntro = PlayerPrefs.GetInt(FromIntroKey, 0) == 1 || PlayerPrefs.GetInt(ForceShedDoorSpawnOnceKey, 0) == 1;

        Transform spawnPoint = isFromIntro ? GetShedDoorSpawnPoint() : GetDefaultSpawnPoint();

        if (spawnPoint != null)
        {
            player.position = spawnPoint.position;
            Debug.Log($"[SpawnManager] Player spawned at {(isFromIntro ? "Shed Door" : "Default Position")}: {spawnPoint.position}");

            if (isFromIntro)
            {
                // If another spawn manager runs later in the frame, re-assert shed spawn once.
                StartCoroutine(EnforcePositionAtEndOfFrame(player, spawnPoint.position));
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
            Debug.Log($"[SpawnManager] Re-asserted shed door spawn at end of frame: {targetPosition}");
        }
    }
}
