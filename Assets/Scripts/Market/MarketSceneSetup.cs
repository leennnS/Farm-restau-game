using UnityEngine;
using Unity.Cinemachine;

public class MarketSceneSetup : MonoBehaviour
{
    [SerializeField] private Transform marketSpawnPoint;
    [SerializeField] private CinemachineCamera cinemachineCamera;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            if (marketSpawnPoint != null)
                player.transform.position = marketSpawnPoint.position;

            if (cinemachineCamera != null)
                cinemachineCamera.Target.TrackingTarget = player.transform;

            CameraFollowFix.RebindAllCamerasTo(player.transform);
        }
        else
        {
            Debug.LogWarning("Player not found in Market scene.");
        }
    }
}
