using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // Required for Coroutines

public class EnterBuildingTrigger2D : MonoBehaviour
{
    [Header("Scene Transition Settings")]
    public string sceneName;
    public float entryDelay = 2f; // Time to wait before entering
    public string playerTag = "Player";

    [Header("Debug")]
    public bool debugLogs = false;

    private Coroutine entryCoroutine;
    private Transform trackedPlayerRoot;
    private int playerOverlapCount;
    private bool isLoading;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        Transform playerRoot = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform.root;
        if (trackedPlayerRoot == null || trackedPlayerRoot == playerRoot)
        {
            trackedPlayerRoot = playerRoot;
            playerOverlapCount++;
        }

        if (debugLogs)
        {
            Debug.Log($"[EnterBuildingTrigger2D] Player entered trigger zone for {sceneName}. Overlaps={playerOverlapCount}");
        }

        if (entryCoroutine != null || isLoading)
            return;

        entryCoroutine = StartCoroutine(EnterBuildingAfterDelay());
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        Transform playerRoot = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform.root;
        if (trackedPlayerRoot == playerRoot)
            playerOverlapCount = Mathf.Max(0, playerOverlapCount - 1);

        if (debugLogs)
        {
            Debug.Log($"[EnterBuildingTrigger2D] Player exited trigger zone for {sceneName}. Overlaps={playerOverlapCount}");
        }

        if (playerOverlapCount > 0 || isLoading)
            return;

        trackedPlayerRoot = null;

        if (entryCoroutine != null)
        {
            StopCoroutine(entryCoroutine);
            entryCoroutine = null;
        }
    }

    private IEnumerator EnterBuildingAfterDelay()
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(entryDelay);

        if (playerOverlapCount <= 0 || trackedPlayerRoot == null)
        {
            entryCoroutine = null;
            yield break;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError($"[EnterBuildingTrigger2D] Cannot load an empty scene name on '{name}'.", this);
            entryCoroutine = null;
            yield break;
        }

        // If we successfully waited, load the scene
        if (debugLogs)
        {
            Debug.Log($"[EnterBuildingTrigger2D] Delay complete. Loading scene: {sceneName}");
        }

        isLoading = true;
        SceneManager.LoadScene(sceneName);
    }

    private bool IsPlayer(Collider2D other)
    {
        if (other == null || string.IsNullOrEmpty(playerTag))
            return false;

        if (other.CompareTag(playerTag))
            return true;

        if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(playerTag))
            return true;

        return other.transform.root != null && other.transform.root.CompareTag(playerTag);
    }
}
