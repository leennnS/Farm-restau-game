using UnityEngine;
using UnityEngine.SceneManagement;

public static class EnforcePlayerScaleOnLoad
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Intentionally left blank to avoid modifying player scale across scenes.
    }
}
