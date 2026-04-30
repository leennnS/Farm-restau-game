using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to the HouseSpawnPoint GameObject.
/// Uses distance-based detection to determine when the Player is in range.
/// When the Player (tag "Player") stays in range for <see cref="countdownSeconds"/>, the scene "FarmScene" is loaded.
/// </summary>
public class HouseExitTrigger : MonoBehaviour
{
    public static bool PendingReturnToFarm;

    [Tooltip("Radius for distance-based detection (world units)")]
    public float radius = 1.25f;

    [Tooltip("Seconds to wait while player remains in range before loading the target scene")]
    public float countdownSeconds = 6f;

    [Tooltip("Name of the scene to load after countdown")]
    public string targetSceneName = "FarmScene";

    bool timerRunning = false;
    Coroutine runningCoroutine;

    Transform playerTransform;

    void Update()
    {
        // Distance-based detection
        if (playerTransform == null)
        {
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null) playerTransform = playerGO.transform;
            else return;
        }

        float d = Vector2.Distance(playerTransform.position, transform.position);
        if (d <= radius)
        {
            if (!timerRunning)
                StartTimer();
        }
        else
        {
            if (timerRunning)
                StopTimer();
        }
    }

    void StartTimer()
    {
        timerRunning = true;
        runningCoroutine = StartCoroutine(CountdownAndLoad());
    }

    void StopTimer()
    {
        timerRunning = false;
        if (runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
            runningCoroutine = null;
        }
    }

    IEnumerator CountdownAndLoad()
    {
        float t = 0f;
        while (t < countdownSeconds)
        {
            // Check if player still exists
            var p = GameObject.FindWithTag("Player");
            if (p == null) yield break;

            float d = Vector2.Distance(p.transform.position, transform.position);
            if (d > radius)
            {
                timerRunning = false;
                yield break;
            }

            t += Time.deltaTime;
            yield return null;
        }

        // Final safety check: ensure player still exists
        var player = GameObject.FindWithTag("Player");
        if (player == null)
            yield break;

        PlayerPrefs.DeleteKey("FromIntroScene");
        PlayerPrefs.DeleteKey("ForceShedDoorSpawnOnce");
        PlayerPrefs.SetInt("ReturnToFarmFrom", 3); // 3 = House
        PlayerPrefs.Save();

        PendingReturnToFarm = true;
        // Load target scene
        SceneManager.LoadScene(targetSceneName);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
