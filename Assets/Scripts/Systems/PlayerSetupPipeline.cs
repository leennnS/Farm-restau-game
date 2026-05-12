using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Authoritative player setup pipeline that runs FIRST during scene load.
/// Ensures player is properly initialized before any scene-specific setup scripts run.
/// 
/// All other scene managers should wait for this pipeline to complete before attempting
/// to find or configure the player.
/// 
/// Setup sequence:
/// 1. Validate player exists in scene (tag or component lookup)
/// 2. Ensure player is tagged with "Player"
/// 3. Enable player GameObject
/// 4. Enable CharacterController2D component
/// 5. Enable Rigidbody2D component
/// 6. Signal completion to waiting systems
/// </summary>
public class PlayerSetupPipeline : MonoBehaviour
{
    private static GameObject persistentPlayer;
    private static bool isSetupComplete = false;
    private static bool isSetupInProgress = false;
    private static event Action OnSetupCompleted;
    private const float DefaultSetupTimeoutSeconds = 5f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Skip setup on menu/intro scenes where player doesn't exist
        if (scene.name == "MAIN MENU" || scene.name == "MainMenu" || scene.name == "IntroScene")
        {
            isSetupComplete = false;
            persistentPlayer = null;
            return;
        }

        isSetupComplete = false;
        isSetupInProgress = true;

        // Create a temporary GameObject to run our pipeline coroutine
        GameObject pipelineRunner = new GameObject("_PlayerSetupPipelineRunner");
        PlayerSetupPipelineRunner runner = pipelineRunner.AddComponent<PlayerSetupPipelineRunner>();
        runner.StartPipeline();
    }

    /// <summary>
    /// Internal class that actually runs the pipeline coroutine
    /// </summary>
    private class PlayerSetupPipelineRunner : MonoBehaviour
    {
        internal void StartPipeline()
        {
            StartCoroutine(RunPipeline());
        }

        private IEnumerator RunPipeline()
        {
            // Stage 1: Find player (with timeout to prevent infinite waiting)
            float timeout = DefaultSetupTimeoutSeconds;
            float elapsed = 0f;
            if (!IsValidPlayer(persistentPlayer))
                persistentPlayer = null;

            Scene currentScene = SceneManager.GetActiveScene();
            Debug.Log($"[PlayerSetupPipeline] Starting player search in scene '{currentScene.name}' (Root count: {currentScene.GetRootGameObjects().Length})");

            // Wait a frame for persistent objects from previous scene to transfer over
            yield return null;

            Debug.Log($"[PlayerSetupPipeline] After initial frame wait - scene now has {currentScene.GetRootGameObjects().Length} root objects");

            // Debug: list DontDestroyOnLoad components present in all loaded scenes
            var persistentComponents = FindObjectsByType<DontDestroyOnLoad>(FindObjectsSortMode.None);
            Debug.Log($"[PlayerSetupPipeline] Found {persistentComponents.Length} DontDestroyOnLoad components across loaded scenes");
            for (int p = 0; p < persistentComponents.Length; p++)
            {
                var go = persistentComponents[p].gameObject;
                Debug.Log($"  - Persistent[{p}]: {go.name} in scene '{go.scene.name}' active={go.activeInHierarchy}");
            }
            while (persistentPlayer == null && elapsed < timeout)
            {
                persistentPlayer = FindPlayerCandidate();

                if (persistentPlayer == null)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }

            if (persistentPlayer == null)
            {
                Debug.LogError($"[PlayerSetupPipeline] FAILED to find player after {timeout} seconds in scene '{SceneManager.GetActiveScene().name}'");

                // Final debug: Show all scenes and all CharacterController2D instances
                Debug.LogError($"[PlayerSetupPipeline] Total scenes loaded: {SceneManager.sceneCount}");
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene s = SceneManager.GetSceneAt(i);
                    Debug.LogError($"  Scene {i}: '{s.name}' (loaded: {s.isLoaded}, root count: {s.GetRootGameObjects().Length})");
                }

                CharacterController2D[] allCtrlGlobal = FindObjectsByType<CharacterController2D>(FindObjectsSortMode.None);
                Debug.LogError($"[PlayerSetupPipeline] Total CharacterController2D in all scenes: {allCtrlGlobal.Length}");

                isSetupInProgress = false;
                Destroy(gameObject);
                yield break;
            }

            Debug.Log($"[PlayerSetupPipeline] Found player: {persistentPlayer.name}");

            EnsurePlayerReady(persistentPlayer);
            CullDuplicatePlayers(persistentPlayer);

            // Wait one frame to ensure all state changes are applied
            yield return null;

            isSetupComplete = true;
            isSetupInProgress = false;

            Debug.Log($"[PlayerSetupPipeline] Setup complete for player '{persistentPlayer.name}' in scene '{SceneManager.GetActiveScene().name}'");

            // Signal all waiting systems
            OnSetupCompleted?.Invoke();

            // Clean up pipeline runner
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Check if player setup is complete. Use before attempting to find or configure player.
    /// </summary>
    public static bool IsSetupComplete => isSetupComplete;

    /// <summary>
    /// Get the persistent player GameObject (only valid after setup is complete)
    /// </summary>
    public static GameObject GetPlayer()
    {
        if (isSetupComplete && persistentPlayer != null)
            return persistentPlayer;

        return null;
    }

    /// <summary>
    /// Wait for player setup to complete. Call this from any script that needs the player.
    /// Timeout ensures we don't wait forever if player never appears.
    /// </summary>
    public static IEnumerator WaitForPlayerSetup(float timeoutSeconds = 5f)
    {
        float elapsed = 0f;

        while (!isSetupComplete && elapsed < timeoutSeconds)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!isSetupComplete)
        {
            Debug.LogWarning($"[PlayerSetupPipeline] Timeout waiting for player setup after {timeoutSeconds} seconds");
        }
    }

    public static void PreparePlayerForSceneChange()
    {
        GameObject player = FindPlayerCandidate();
        if (player == null)
            return;

        persistentPlayer = player;
        EnsurePlayerReady(player);
        CullDuplicatePlayers(player);
    }

    public static GameObject FindPlayerInLoadedScenes()
    {
        return FindPlayerCandidate();
    }

    private static GameObject FindPlayerCandidate()
    {
        if (IsValidPlayer(persistentPlayer))
            return persistentPlayer;

        GameObject tagged = GameObject.FindGameObjectWithTag("Player");
        if (IsValidPlayer(tagged))
            return tagged;

        CharacterController2D[] controllers = FindObjectsByType<CharacterController2D>(FindObjectsSortMode.None);
        GameObject selected = SelectBestPlayerFromControllers(controllers);
        if (selected != null)
            return selected;

        CharacterController2D[] allControllers = Resources.FindObjectsOfTypeAll<CharacterController2D>();
        selected = SelectBestPlayerFromControllers(allControllers);
        if (selected != null)
            return selected;

        MonoBehaviour alt = FindFirstMonoBehaviourByTypeName("Character2D");
        if (alt != null && IsValidPlayer(alt.gameObject))
            return alt.gameObject;

        return null;
    }

    private static GameObject SelectBestPlayerFromControllers(CharacterController2D[] controllers)
    {
        GameObject best = null;
        int bestScore = int.MinValue;

        for (int i = 0; i < controllers.Length; i++)
        {
            CharacterController2D controller = controllers[i];
            if (controller == null)
                continue;

            GameObject candidate = controller.gameObject;
            if (!IsValidPlayer(candidate))
                continue;

            int score = 0;
            if (candidate.scene.name == "DontDestroyOnLoad")
                score += 3;
            if (candidate.CompareTag("Player"))
                score += 2;
            if (candidate.activeInHierarchy)
                score += 1;

            if (score > bestScore)
            {
                best = candidate;
                bestScore = score;
            }
        }

        return best;
    }

    private static bool IsValidPlayer(GameObject candidate)
    {
        if (candidate == null)
            return false;

        if (!candidate.scene.IsValid() || !candidate.scene.isLoaded)
            return false;

        if (candidate.hideFlags != HideFlags.None)
            return false;

        if (candidate.CompareTag("Player"))
            return true;

        if (candidate.GetComponent<CharacterController2D>() != null)
            return true;

        if (candidate.GetComponent("Character2D") != null)
            return true;

        return false;
    }

    private static void EnsurePlayerReady(GameObject player)
    {
        if (player == null)
            return;

        if (player.transform.parent != null)
            player.transform.SetParent(null);

        if (!player.CompareTag("Player"))
            player.tag = "Player";

        if (!player.activeSelf)
            player.SetActive(true);

        UnityEngine.Object.DontDestroyOnLoad(player);

        CharacterController2D playerController = player.GetComponent<CharacterController2D>();
        if (playerController != null && !playerController.enabled)
            playerController.enabled = true;

        Behaviour altController = player.GetComponent("Character2D") as Behaviour;
        if (altController != null && !altController.enabled)
            altController.enabled = true;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null && !rb.simulated)
            rb.simulated = true;
    }

    private static void CullDuplicatePlayers(GameObject keep)
    {
        if (keep == null)
            return;

        CharacterController2D[] controllers = Resources.FindObjectsOfTypeAll<CharacterController2D>();
        for (int i = 0; i < controllers.Length; i++)
        {
            CharacterController2D controller = controllers[i];
            if (controller == null)
                continue;

            GameObject candidate = controller.gameObject;
            if (candidate == keep)
                continue;

            if (!IsValidPlayer(candidate))
                continue;

            UnityEngine.Object.Destroy(candidate);
        }

        GameObject[] taggedPlayers = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < taggedPlayers.Length; i++)
        {
            GameObject candidate = taggedPlayers[i];
            if (candidate == keep)
                continue;

            if (!IsValidPlayer(candidate))
                continue;

            UnityEngine.Object.Destroy(candidate);
        }
    }

    private static MonoBehaviour FindFirstMonoBehaviourByTypeName(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return null;

        MonoBehaviour[] behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null)
                continue;

            if (!behaviour.gameObject.scene.IsValid() || !behaviour.gameObject.scene.isLoaded)
                continue;

            if (behaviour.GetType().Name == typeName)
                return behaviour;
        }

        return null;
    }

    /// <summary>
    /// Register a callback to be invoked when player setup completes
    /// </summary>
    public static void RegisterOnSetupCompleted(Action callback)
    {
        if (isSetupComplete)
        {
            // If already complete, invoke immediately
            callback?.Invoke();
        }
        else
        {
            OnSetupCompleted += callback;
        }
    }

    /// <summary>
    /// Unregister a callback
    /// </summary>
    public static void UnregisterOnSetupCompleted(Action callback)
    {
        OnSetupCompleted -= callback;
    }
}
