using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Global money system shared across all scenes.
/// Uses PlayerPrefs for persistence and survives scene loads.
/// </summary>
public class MoneyManager : MonoBehaviour
{
    private const string MoneyKey = "GlobalMoney";
    private const string DebtKey = "GlobalDebt";

    private static MoneyManager _instance;

    [Header("Defaults")]
    [SerializeField] private int defaultStartingMoney = 250;
    [SerializeField] private int defaultStartingDebt = 0;
    [SerializeField] private int maxLoanAmount = 5000;

    [Header("UI")]
    [SerializeField] private bool autoCreateGlobalMoneyHud = true;

    [Header("Debug Loan Shortcuts")]
    [SerializeField] private bool enableLoanDebugShortcuts = true;
    [SerializeField] private KeyCode takeLoanKey = KeyCode.L;
    [SerializeField] private int debugLoanAmount = 500;
    [SerializeField] private KeyCode repayLoanKey = KeyCode.R;
    [SerializeField] private int debugRepayAmount = 500;

    [Header("Loan Hint")]
    [SerializeField] private bool showZeroMoneyLoanHint = true;
    [SerializeField] private string zeroMoneyLoanHintMessage = "No money? Press L to take a loan.";
    [SerializeField] private float zeroMoneyLoanHintDuration = 5f;

    private bool _hasShownZeroMoneyHint;

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
    public int CurrentDebt { get; private set; }
    public event Action<int> OnMoneyChanged;
    public event Action<int> OnDebtChanged;

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
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Create HUD when entering game scenes (not menu)
        if (autoCreateGlobalMoneyHud && !IsMenuScene(scene.name))
        {
            EnsureGlobalHudExists();
        }
    }

    private bool IsMenuScene(string sceneName)
    {
        return sceneName.Equals("MAIN MENU", System.StringComparison.Ordinal);
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
        HandleZeroMoneyLoanHint();
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
        HandleZeroMoneyLoanHint();
        return true;
    }

    public void SetMoney(int amount)
    {
        CurrentMoney = Mathf.Max(0, amount);
        SaveMoney();
        OnMoneyChanged?.Invoke(CurrentMoney);
        HandleZeroMoneyLoanHint();
    }

    public bool CanTakeLoan(int amount)
    {
        if (amount <= 0)
            return false;

        return CurrentDebt + amount <= Mathf.Max(0, maxLoanAmount);
    }

    public bool TakeLoan(int amount)
    {
        if (!CanTakeLoan(amount))
            return false;

        CurrentDebt += amount;
        CurrentMoney += amount;

        SaveMoney();
        OnDebtChanged?.Invoke(CurrentDebt);
        OnMoneyChanged?.Invoke(CurrentMoney);
        HandleZeroMoneyLoanHint();
        return true;
    }

    public int RepayDebt(int amount)
    {
        if (amount <= 0 || CurrentDebt <= 0 || CurrentMoney <= 0)
            return 0;

        int paid = Mathf.Min(amount, CurrentDebt, CurrentMoney);
        CurrentDebt -= paid;
        CurrentMoney -= paid;

        SaveMoney();
        OnDebtChanged?.Invoke(CurrentDebt);
        OnMoneyChanged?.Invoke(CurrentMoney);
        HandleZeroMoneyLoanHint();
        return paid;
    }

    public void ResetToDefault()
    {
        SetMoney(Mathf.Max(0, defaultStartingMoney));
        CurrentDebt = defaultStartingDebt;
        SaveMoney();
        OnDebtChanged?.Invoke(CurrentDebt);
    }

    public void SaveMoney()
    {
        PlayerPrefs.SetInt(MoneyKey, CurrentMoney);
        PlayerPrefs.SetInt(DebtKey, CurrentDebt);
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
        }

        CurrentDebt = Mathf.Max(0, PlayerPrefs.GetInt(DebtKey, defaultStartingDebt));

        // Keep keys in sync with defaults when first booting a profile.
        SaveMoney();

        OnMoneyChanged?.Invoke(CurrentMoney);
        OnDebtChanged?.Invoke(CurrentDebt);
        HandleZeroMoneyLoanHint();
    }

    private void HandleZeroMoneyLoanHint()
    {
        if (!showZeroMoneyLoanHint)
            return;

        if (CurrentMoney > 0)
        {
            _hasShownZeroMoneyHint = false;
            return;
        }

        if (_hasShownZeroMoneyHint)
            return;

        PickupToastUIToolkit toast = FindFirstObjectByType<PickupToastUIToolkit>();
        if (toast != null)
            toast.Show(zeroMoneyLoanHintMessage, zeroMoneyLoanHintDuration);

        _hasShownZeroMoneyHint = true;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SaveMoney();
    }

    private void Update()
    {
        if (!enableLoanDebugShortcuts)
            return;

        if (Input.GetKeyDown(takeLoanKey))
        {
            bool success = TakeLoan(debugLoanAmount);
            if (!success)
                Debug.Log($"[MoneyManager] Loan denied. Requested={debugLoanAmount}, CurrentDebt={CurrentDebt}, MaxLoan={maxLoanAmount}");
        }

        if (Input.GetKeyDown(repayLoanKey))
        {
            int paid = RepayDebt(debugRepayAmount);
            if (paid <= 0)
                Debug.Log($"[MoneyManager] Repay failed. Requested={debugRepayAmount}, Money={CurrentMoney}, Debt={CurrentDebt}");
        }
    }

    private void OnApplicationQuit()
    {
        SaveMoney();
    }
}