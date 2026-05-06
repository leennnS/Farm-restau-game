using UnityEngine;
using UnityEngine.SceneManagement;
using System;

/// <summary>
/// Central game state manager that coordinates all persistent systems.
/// Handles save/load across MoneyManager, InventoryController, and DayNightCycle.
/// </summary>
public class GameManager : MonoBehaviour
{
    private const string GameStateKey = "GameState_HasSave";
    private const string LastSceneKey = "GameState_LastScene";

    private static GameManager _instance;

    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<GameManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    _instance = go.AddComponent<GameManager>();
                }
            }
            return _instance;
        }
    }

    public static bool HasInstance => _instance != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        _ = Instance;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Auto-save periodically during gameplay
        InvokeRepeating(nameof(SaveGameState), 5f, 10f);
    }

    private void OnApplicationQuit()
    {
        SaveGameState();
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveGameState();
        }
    }

    /// <summary>
    /// Check if a save file exists (used by Continue button)
    /// </summary>
    public bool HasExistingSave()
    {
        return PlayerPrefs.HasKey(GameStateKey) && PlayerPrefs.GetInt(GameStateKey, 0) == 1;
    }

    /// <summary>
    /// Start a new game - clear all player data and reset game state
    /// </summary>
    public void NewGame()
    {
        Debug.Log("[GameManager] Starting new game - clearing player data");

        MainMenuRuntimeCleanup.PrepareForMainMenu(destroyGameManager: false);

        // Clear all persistent player data
        ClearAllGameData();

        // Reset managers to defaults
        if (MoneyManager.HasInstance)
        {
            MoneyManager.Instance.ResetToDefault();
        }

        if (InventoryController.HasInstance)
        {
            InventoryController.Instance.ClearAllItems();
        }

        if (DayNightCycleNice2D.Instance != null)
        {
            DayNightCycleNice2D.Instance.ResetToDefault();
        }

        // Mark that we have a save (for the new game we just started)
        PlayerPrefs.SetInt(GameStateKey, 1);
        PlayerPrefs.Save();

        // Load the intro scene (which transitions to FarmScene when complete)
        SceneManager.LoadScene("Intro");
    }

    /// <summary>
    /// Continue a previously saved game
    /// </summary>
    public void ContinueGame()
    {
        if (!HasExistingSave())
        {
            Debug.LogWarning("[GameManager] No save file exists. Starting new game instead.");
            NewGame();
            return;
        }

        Debug.Log("[GameManager] Loading saved game");

        // Load all persisted game data (via their respective managers)
        if (MoneyManager.HasInstance)
        {
            MoneyManager.Instance.LoadMoney();
        }

        if (InventoryController.HasInstance)
        {
            InventoryController.Instance.LoadPlayerInventory();
        }

        if (DayNightCycleNice2D.Instance != null)
        {
            DayNightCycleNice2D.Instance.LoadTimeState();
        }

        // Always resume from the farm scene
        SceneManager.LoadScene("FarmScene");
    }

    /// <summary>
    /// Save current game state - called when transitioning scenes or on quit
    /// </summary>
    public void SaveGameState()
    {
        Debug.Log("[GameManager] Saving game state");

        // All managers save their own data
        if (MoneyManager.HasInstance)
        {
            MoneyManager.Instance.SaveMoney();
        }

        if (InventoryController.HasInstance)
        {
            InventoryController.Instance.SaveInventoryData();
        }

        if (DayNightCycleNice2D.Instance != null)
        {
            DayNightCycleNice2D.Instance.SaveTimeState();
        }

        // Mark that we have a valid save
        PlayerPrefs.SetInt(GameStateKey, 1);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Completely clear all game data (used for New Game)
    /// </summary>
    public void ClearAllGameData()
    {
        PlayerPrefs.DeleteKey(GameStateKey);
        PlayerPrefs.DeleteKey("GlobalMoney");
        PlayerPrefs.DeleteKey("GlobalDebt");
        PlayerPrefs.DeleteKey("GlobalInventory");
        PlayerPrefs.DeleteKey("DayNight_TimeNormalized");
        PlayerPrefs.DeleteKey("DayNight_DayIndex");
        PlayerPrefs.DeleteKey("JournalText");

        // Clear tutorial state for a fresh new game
        PlayerPrefs.DeleteKey("FarmTutorialStarted");
        PlayerPrefs.DeleteKey("FarmTutorialCompleted");
        PlayerPrefs.DeleteKey("PendingFarmTutorial");

        PlayerPrefs.Save();
    }

    /// <summary>
    /// Delete a save and prepare for new game
    /// </summary>
    public void DeleteSave()
    {
        ClearAllGameData();
        Debug.Log("[GameManager] Save data deleted");
    }
}
