using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Place this on a trigger object in MarketScene to return the player to FarmScene.
/// Works with either a trigger collider or distance-based detection.
/// </summary>
public class MarketExitTrigger : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("Used when no trigger collider is present")]
    [SerializeField] private float radius = 1.25f;
    [SerializeField] private string playerTag = "Player";

    [Header("Transition")]
    [SerializeField] private float countdownSeconds = 2f;
    [SerializeField] private string targetSceneName = "FarmScene";

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private bool _timerRunning;
    private Coroutine _runningCoroutine;
    private Collider2D _localCollider;
    private Transform _playerTransform;

    private void Awake()
    {
        _localCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        // If trigger collider exists, rely on trigger callbacks.
        if (_localCollider != null && _localCollider.isTrigger)
            return;

        if (_playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag(playerTag);
            if (player != null)
                _playerTransform = player.transform;
            else
                return;
        }

        float d = Vector2.Distance(_playerTransform.position, transform.position);
        if (d <= radius)
        {
            if (!_timerRunning)
                StartTimer();
        }
        else if (_timerRunning)
        {
            StopTimer();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_localCollider == null || !_localCollider.isTrigger)
            return;

        if (!other.CompareTag(playerTag))
            return;

        if (!_timerRunning)
            StartTimer();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (_localCollider == null || !_localCollider.isTrigger)
            return;

        if (!other.CompareTag(playerTag))
            return;

        if (_timerRunning)
            StopTimer();
    }

    private void StartTimer()
    {
        _timerRunning = true;
        _runningCoroutine = StartCoroutine(CountdownAndLoad());

        if (debugLogs)
            Debug.Log("[MarketExitTrigger] Exit countdown started.");
    }

    private void StopTimer()
    {
        _timerRunning = false;
        if (_runningCoroutine != null)
        {
            StopCoroutine(_runningCoroutine);
            _runningCoroutine = null;
        }

        if (debugLogs)
            Debug.Log("[MarketExitTrigger] Exit countdown cancelled.");
    }

    private IEnumerator CountdownAndLoad()
    {
        float t = 0f;
        while (t < countdownSeconds)
        {
            if (_localCollider == null || !_localCollider.isTrigger)
            {
                GameObject p = GameObject.FindWithTag(playerTag);
                if (p == null)
                {
                    _timerRunning = false;
                    yield break;
                }

                if (Vector2.Distance(p.transform.position, transform.position) > radius)
                {
                    _timerRunning = false;
                    yield break;
                }
            }

            t += Time.deltaTime;
            yield return null;
        }

        if (debugLogs)
            Debug.Log($"[MarketExitTrigger] Loading scene: {targetSceneName}");

        MarketReturnContext.PendingReturnToFarm = true;
        SceneManager.LoadScene(targetSceneName);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
