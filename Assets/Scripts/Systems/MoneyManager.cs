using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

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
    [SerializeField] private int loanUnitAmount = 500;
    [SerializeField] private int maxActiveDebts = 2;

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
    [SerializeField] private string zeroMoneyLoanHintMessage = "L to take a loan and R to repay";
    [SerializeField] private float zeroMoneyLoanHintDuration = 5f;
    [SerializeField] private float zeroMoneyLoanHintRepeatInterval = 120f; // seconds between repeated hints when at zero

    [Header("Audio")]
    [SerializeField] private AudioClip moneyChangeSound;

    private bool _hasShownZeroMoneyHint;
    private Coroutine _zeroMoneyHintRoutine;
    private AudioSource _audioSource;
    private bool _playedMoneyChangeSoundThisFrame;
    private bool _hasCreatedFallbackMoneyChangeSound;
    private Coroutine _ensureLoanToastAfterSceneLoadRoutine;
    private const string MoneyChangeSoundResourcePath = "SFX/money-change";

    public static MoneyManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<MoneyManager>();

                if (_instance == null)
                {
                    GameObject moneyManagerObject = new GameObject("MoneyManager");
                    _instance = moneyManagerObject.AddComponent<MoneyManager>();
                    Debug.LogWarning("[MoneyManager] No MoneyManager found in scene. Created one automatically.");
                }
            }

            return _instance;
        }
    }

    public static bool HasInstance => _instance != null;

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
        EnsureAudioSource();
        EnsureMoneyChangeSound();
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

        EnsureLoanToastExists();

        // Re-evaluate hint visibility after scene transitions so loan prompts remain available.
        HandleZeroMoneyLoanHint();

        if (_ensureLoanToastAfterSceneLoadRoutine != null)
            StopCoroutine(_ensureLoanToastAfterSceneLoadRoutine);

        _ensureLoanToastAfterSceneLoadRoutine = StartCoroutine(EnsureLoanToastWhenUiReady());
    }

    private System.Collections.IEnumerator EnsureLoanToastWhenUiReady()
    {
        const float retryDurationSeconds = 2f;
        const float retryIntervalSeconds = 0.1f;

        float elapsed = 0f;
        while (elapsed < retryDurationSeconds)
        {
            PickupToastUIToolkit toast = EnsureLoanToastExists();
            if (toast != null)
            {
                if (CurrentMoney <= 0)
                    ShowZeroMoneyLoanHintImmediate();

                _ensureLoanToastAfterSceneLoadRoutine = null;
                yield break;
            }

            yield return new WaitForSeconds(retryIntervalSeconds);
            elapsed += retryIntervalSeconds;
        }

        _ensureLoanToastAfterSceneLoadRoutine = null;
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
        PlayMoneyChangeSound();
        NotifyMoneyChanged();
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
        PlayMoneyChangeSound();
        NotifyMoneyChanged();
        HandleZeroMoneyLoanHint();
        return true;
    }

    public void SetMoney(int amount)
    {
        CurrentMoney = Mathf.Max(0, amount);
        SaveMoney();
        PlayMoneyChangeSound();
        NotifyMoneyChanged();
        HandleZeroMoneyLoanHint();
    }

    public bool CanTakeLoan(int amount)
    {
        if (amount <= 0)
            return false;

        if (CurrentMoney == 0)
            return true;

        return CurrentDebt < 1000;
    }

    public bool TakeLoan(int amount)
    {
        if (!CanTakeLoan(amount))
            return false;

        CurrentDebt += amount;
        CurrentMoney += amount;

        SaveMoney();
        PlayMoneyChangeSound();
        NotifyDebtChanged();
        NotifyMoneyChanged();
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
        PlayMoneyChangeSound();
        NotifyDebtChanged();
        NotifyMoneyChanged();
        HandleZeroMoneyLoanHint();
        return paid;
    }

    public void ResetToDefault()
    {
        SetMoney(Mathf.Max(0, defaultStartingMoney));
        CurrentDebt = defaultStartingDebt;
        SaveMoney();
        NotifyDebtChanged();
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

        // Migrate old saves to current debt rules without requiring a new game.
        int debtCap = GetEffectiveDebtCap();
        if (CurrentDebt > debtCap)
            CurrentDebt = debtCap;

        // Keep keys in sync with defaults when first booting a profile.
        SaveMoney();

        NotifyMoneyChanged();
        NotifyDebtChanged();
        HandleZeroMoneyLoanHint();
    }

    private void NotifyMoneyChanged()
    {
        OnMoneyChanged?.Invoke(CurrentMoney);
        RefreshMoneyDisplays();
    }

    private void NotifyDebtChanged()
    {
        OnDebtChanged?.Invoke(CurrentDebt);
        RefreshMoneyDisplays();
    }

    private void RefreshMoneyDisplays()
    {
        GlobalMoneyHUD[] globalHuds = FindObjectsByType<GlobalMoneyHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < globalHuds.Length; i++)
        {
            if (globalHuds[i] != null)
                globalHuds[i].Refresh();
        }

        MoneyDisplayUI[] moneyDisplays = FindObjectsByType<MoneyDisplayUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < moneyDisplays.Length; i++)
        {
            if (moneyDisplays[i] != null)
                moneyDisplays[i].Refresh();
        }
    }

    private int GetEffectiveDebtCap()
    {
        int unitAmount = Mathf.Max(1, loanUnitAmount);
        int activeDebtLimit = Mathf.Max(1, maxActiveDebts) * unitAmount;
        int configuredCap = maxLoanAmount > 0 ? Mathf.Min(activeDebtLimit, maxLoanAmount) : activeDebtLimit;
        return Mathf.Max(0, configuredCap);
    }

    private void HandleZeroMoneyLoanHint()
    {
        if (!showZeroMoneyLoanHint)
            return;

        if (CurrentMoney > 0)
        {
            _hasShownZeroMoneyHint = false;
            StopZeroMoneyHintLoop();
            return;
        }

        // Show immediately and start repeat loop so hint reappears every few minutes
        ShowZeroMoneyLoanHintImmediate();
        StartZeroMoneyHintLoop();
    }

    private void StartZeroMoneyHintLoop()
    {
        if (_zeroMoneyHintRoutine != null)
            return;

        if (zeroMoneyLoanHintRepeatInterval <= 0f)
            return;

        _zeroMoneyHintRoutine = StartCoroutine(ZeroMoneyHintLoop());
    }

    private void StopZeroMoneyHintLoop()
    {
        if (_zeroMoneyHintRoutine == null)
            return;

        StopCoroutine(_zeroMoneyHintRoutine);
        _zeroMoneyHintRoutine = null;
    }

    private System.Collections.IEnumerator ZeroMoneyHintLoop()
    {
        while (CurrentMoney <= 0)
        {
            yield return new WaitForSeconds(Mathf.Max(1f, zeroMoneyLoanHintRepeatInterval));
            if (CurrentMoney <= 0)
                ShowZeroMoneyLoanHintImmediate();
        }

        _zeroMoneyHintRoutine = null;
    }

    public void ShowZeroMoneyLoanHintImmediate()
    {
        if (!showZeroMoneyLoanHint)
            return;

        PickupToastUIToolkit toast = FindFirstObjectByType<PickupToastUIToolkit>();
        if (toast == null)
            toast = EnsureLoanToastExists();

        if (toast != null)
            toast.Show(zeroMoneyLoanHintMessage, zeroMoneyLoanHintDuration);

        _hasShownZeroMoneyHint = true;
    }

    private PickupToastUIToolkit EnsureLoanToastExists()
    {
        PickupToastUIToolkit toast = FindFirstObjectByType<PickupToastUIToolkit>();
        if (toast != null)
            return toast;

        UIDocument sourceDocument = FindSourceUiDocument();
        if (sourceDocument == null || sourceDocument.panelSettings == null)
            return null;

        GameObject toastGo = new GameObject("RuntimePickupToastUI");
        UIDocument toastDocument = toastGo.AddComponent<UIDocument>();
        toastDocument.panelSettings = sourceDocument.panelSettings;
        toastDocument.sortingOrder = sourceDocument.sortingOrder + 100;
        toastGo.AddComponent<PickupToastUIToolkit>();
        DontDestroyOnLoad(toastGo);

        return FindFirstObjectByType<PickupToastUIToolkit>();
    }

    private UIDocument FindSourceUiDocument()
    {
        UIDocument[] documents = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
        for (int i = 0; i < documents.Length; i++)
        {
            UIDocument document = documents[i];
            if (document != null && document.isActiveAndEnabled && document.panelSettings != null)
                return document;
        }

        return null;
    }

    private void EnsureAudioSource()
    {
        if (_audioSource == null)
            _audioSource = GetComponent<AudioSource>();

        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        _audioSource.spatialBlend = 0f;
    }

    private void PlayMoneyChangeSound()
    {
        if (_playedMoneyChangeSoundThisFrame || _audioSource == null)
            return;

        EnsureMoneyChangeSound();

        if (moneyChangeSound == null)
            return;

        _audioSource.PlayOneShot(moneyChangeSound);
        _playedMoneyChangeSoundThisFrame = true;
    }

    private void EnsureMoneyChangeSound()
    {
        if (moneyChangeSound != null || _hasCreatedFallbackMoneyChangeSound)
            return;

        moneyChangeSound = Resources.Load<AudioClip>(MoneyChangeSoundResourcePath);
        if (moneyChangeSound != null)
            return;

        moneyChangeSound = CreateFallbackMoneyChangeSound();
        _hasCreatedFallbackMoneyChangeSound = true;
    }

    private AudioClip CreateFallbackMoneyChangeSound()
    {
        const int sampleRate = 44100;
        const float durationSeconds = 0.18f;
        int sampleCount = Mathf.CeilToInt(sampleRate * durationSeconds);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float envelope = Mathf.Exp(-time * 18f);
            float toneA = Mathf.Sin(2f * Mathf.PI * 880f * time);
            float toneB = Mathf.Sin(2f * Mathf.PI * 1320f * time) * 0.6f;
            float toneC = Mathf.Sin(2f * Mathf.PI * 1760f * time) * 0.35f;
            samples[i] = (toneA + toneB + toneC) * 0.22f * envelope;
        }

        AudioClip clip = AudioClip.Create("FallbackMoneyChange", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
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
            ShowZeroMoneyLoanHintImmediate();
            TakeLoan(debugLoanAmount);
        }

        if (Input.GetKeyDown(repayLoanKey))
        {
            int paid = RepayDebt(debugRepayAmount);
            if (paid <= 0)
                Debug.Log($"[MoneyManager] Repay failed. Requested={debugRepayAmount}, Money={CurrentMoney}, Debt={CurrentDebt}");
        }
    }

    private void LateUpdate()
    {
        _playedMoneyChangeSoundThisFrame = false;
    }

    private void OnApplicationQuit()
    {
        SaveMoney();
    }
}
