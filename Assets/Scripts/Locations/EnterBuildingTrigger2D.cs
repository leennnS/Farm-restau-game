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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (debugLogs)
        {
            Debug.Log($"[EnterBuildingTrigger2D] Player entered trigger zone for {sceneName}. Starting entry timer.");
        }

        if (!IsPlayer(other))
        {
            return;
        }

        // Start the timer to enter the building
        entryCoroutine = StartCoroutine(EnterBuildingAfterDelay());
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (debugLogs)
        {
            Debug.Log("[EnterBuildingTrigger2D] Player exited trigger zone. Cancelling entry.");
        }

        if (!IsPlayer(other))
        {
            return;
        }

        // If the player leaves, cancel the timer
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

        // If we successfully waited, load the scene
        if (debugLogs)
        {
            Debug.Log($"[EnterBuildingTrigger2D] Delay complete. Loading scene: {sceneName}");
        }
        SceneManager.LoadScene(sceneName);
    }

    private bool IsPlayer(Collider2D other)
    {
        bool isPlayer = false;
        if (!string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag))
        {
            isPlayer = true;
        }

        return isPlayer;
    }
}
