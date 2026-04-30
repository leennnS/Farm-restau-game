using UnityEngine;

/// <summary>
/// Local spawner placed in the House scene (attached to an empty GameObject).
/// This is a fallback to ensure player spawns at the HouseSpawnPoint if static HouseSpawnManager fails.
/// Attach this to any GameObject in the HouseInteriorLITEDEMO scene.
/// </summary>
public class HouseLocalSpawner : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;

    private void Awake()
    {
        Debug.Log("HouseLocalSpawner: Awake called in House scene");

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("HouseLocalSpawner: Could not find player with 'Player' tag.");
            return;
        }

        if (spawnPoint == null)
        {
            // Try to find it by name
            GameObject spawnObj = GameObject.Find("HouseSpawnPoint");
            if (spawnObj != null)
                spawnPoint = spawnObj.transform;
        }

        if (spawnPoint == null)
        {
            Debug.LogWarning("HouseLocalSpawner: No spawn point assigned and could not find 'HouseSpawnPoint' by name.");
            return;
        }

        // Move player to spawn point
        player.transform.position = spawnPoint.position;
        Debug.Log($"HouseLocalSpawner: Moved player to {spawnPoint.position}");
    }
}
