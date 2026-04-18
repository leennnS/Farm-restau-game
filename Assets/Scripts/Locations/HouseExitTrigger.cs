using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to the HouseSpawnPoint GameObject.
/// If a trigger Collider2D (isTrigger=true) exists on the same GameObject, it will use OnTriggerEnter2D/Exit2D.
/// Otherwise it falls back to distance-based detection using <see cref="radius"/>.
/// When the Player (tag "Player") stays in range for <see cref="countdownSeconds"/>, the scene "FarmScene" is loaded.
/// </summary>
public class HouseExitTrigger : MonoBehaviour
{
    public static bool PendingReturnToFarm;

    [Tooltip("Radius used when no trigger collider is present (world units)")]
    public float radius = 1.25f;

    [Tooltip("Seconds to wait while player remains in range before loading the target scene")]
    public float countdownSeconds = 6f;

    [Tooltip("Name of the scene to load after countdown")]
    public string targetSceneName = "FarmScene";

    bool timerRunning = false;
    Coroutine runningCoroutine;

    Transform playerTransform;

    Collider2D localCollider;

    void Awake()
    {
        localCollider = GetComponent<Collider2D>();
        if (localCollider != null && localCollider.isTrigger)
        {
            // rely on OnTriggerEnter2D / OnTriggerExit2D
        }
        else
        {
            // fallback uses distance checks
        }
    }

    void Update()
    {
        // If there's a trigger collider we don't need distance checks
        if (localCollider != null && localCollider.isTrigger)
            return;

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

    void OnTriggerEnter2D(Collider2D other)
    {
        if (localCollider == null || !localCollider.isTrigger) return;
        if (!other.CompareTag("Player")) return;

        if (!timerRunning)
            StartTimer();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (localCollider == null || !localCollider.isTrigger) return;
        if (!other.CompareTag("Player")) return;

        if (timerRunning)
            StopTimer();
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
            // If player no longer exists or left range (distance mode), stop
            if (localCollider == null || !localCollider.isTrigger)
            {
                var p = GameObject.FindWithTag("Player");
                if (p == null) yield break;
                float d = Vector2.Distance(p.transform.position, transform.position);
                if (d > radius)
                {
                    timerRunning = false;
                    yield break;
                }
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
