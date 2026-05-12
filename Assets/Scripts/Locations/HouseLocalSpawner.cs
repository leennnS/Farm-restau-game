using UnityEngine;
using System.Collections;

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
        Debug.Log("[HouseLocalSpawner] Awake called in House scene");
        // Spawn immediately - player should already exist
        StartCoroutine(DoSpawn());
    }

    private IEnumerator DoSpawn()
    {
        // Give one frame for scene to fully load
        yield return null;

        // Try to find player via tag first, then component
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            CharacterController2D playerCtrl = FindFirstObjectByType<CharacterController2D>();
            if (playerCtrl != null)
                player = playerCtrl.gameObject;
        }

        if (player == null)
        {
            Debug.LogWarning("[HouseLocalSpawner] Player not found");
            yield break;
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
            Debug.LogWarning("[HouseLocalSpawner] No spawn point assigned and could not find 'HouseSpawnPoint' by name");
            yield break;
        }

        // Move player to spawn point
        player.transform.position = spawnPoint.position;
        CameraFollowFix.RebindAllCamerasTo(player.transform);
        Debug.Log($"[HouseLocalSpawner] Moved player to {spawnPoint.position}");
    }
}
