using UnityEngine;
using UnityEngine.SceneManagement;

public static class EnforcePlayerFreezeRotation
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning($"EnforcePlayerFreezeRotation: No GameObject with tag 'Player' found when scene '{scene.name}' loaded.");
            return;
        }

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogWarning($"EnforcePlayerFreezeRotation: Player '{player.name}' has no Rigidbody2D.");
            return;
        }

        // Preserve existing constraints and add FreezeRotation
        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;

        Debug.Log($"EnforcePlayerFreezeRotation: Applied FreezeRotation to Rigidbody2D on '{player.name}' in scene '{scene.name}'.");
    }
}
