using System;
using UnityEngine;

/// <summary>
/// Global money system shared across all scenes.
/// Uses PlayerPrefs for persistence and survives scene loads.
/// </summary>
public class MoneyManager : MonoBehaviour
{
    private const string MoneyKey = "GlobalMoney";

    private static MoneyManager _instance;

    [Header("Defaults")]
    [SerializeField] private int defaultStartingMoney = 250;

    [Header("UI")]
    [SerializeField] private bool autoCreateGlobalMoneyHud = true;

    public static MoneyManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<MoneyManager>();

                if (_instance == null)
                {
                    GameObject go = new GameObject("MoneyManager");
                    _instance = go.AddComponent<MoneyManager>();
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

    public int CurrentMoney { get; private set; }
    public event Action<int> OnMoneyChanged;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        LoadMoney();

        if (autoCreateGlobalMoneyHud)
            EnsureGlobalHudExists();
    }

    private void EnsureGlobalHudExists()
    {
        GlobalMoneyHUD existingHud = FindFirstObjectByType<GlobalMoneyHUD>();
        if (existingHud != null)
            return;

        GameObject hudGo = new GameObject("GlobalMoneyHUD");
        hudGo.AddComponent<GlobalMoneyHUD>();
    }

    public bool CanAfford(int amount)
    {
        if (amount < 0)
            return false;

        return CurrentMoney >= amount;
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0)
            return;

        CurrentMoney += amount;
        SaveMoney();
        OnMoneyChanged?.Invoke(CurrentMoney);
    }

    public bool SpendMoney(int amount)
    {
        if (amount < 0)
            return false;

        if (CurrentMoney < amount)
            return false;

        CurrentMoney -= amount;
        SaveMoney();
        OnMoneyChanged?.Invoke(CurrentMoney);
        return true;
    }

    public void SetMoney(int amount)
    {
        CurrentMoney = Mathf.Max(0, amount);
        SaveMoney();
        OnMoneyChanged?.Invoke(CurrentMoney);
    }

    public void ResetToDefault()
    {
        SetMoney(Mathf.Max(0, defaultStartingMoney));
    }

    public void SaveMoney()
    {
        PlayerPrefs.SetInt(MoneyKey, CurrentMoney);
        PlayerPrefs.Save();
    }

    public void LoadMoney()
    {
        if (PlayerPrefs.HasKey(MoneyKey))
        {
            CurrentMoney = PlayerPrefs.GetInt(MoneyKey, defaultStartingMoney);
        }
        else
        {
            CurrentMoney = Mathf.Max(0, defaultStartingMoney);
            SaveMoney();
        }

        OnMoneyChanged?.Invoke(CurrentMoney);
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SaveMoney();
    }

    private void OnApplicationQuit()
    {
        SaveMoney();
    }
}