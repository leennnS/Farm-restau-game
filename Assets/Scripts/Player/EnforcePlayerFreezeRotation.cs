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
            return;
        }

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            return;
        }

        // Preserve existing constraints and add FreezeRotation
        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;
    }
}
