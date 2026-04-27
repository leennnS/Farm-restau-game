using System.Collections;
using UnityEngine;

/// <summary>
/// Orchestrates fishing mini-game flow:
/// - input handling
/// - gameplay logic updates
/// - UI rendering
/// - result resolution and rewards
/// </summary>
[RequireComponent(typeof(FishingMiniGameView))]
[RequireComponent(typeof(FishingBarMiniGameLogic))]
public class FishingUIController_Hold : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FishingMiniGameController miniGameController;
    [SerializeField] private FishingMiniGameView fishingView;
    [SerializeField] private FishingBarMiniGameLogic miniGameLogic;

    [Header("Session")]
    [SerializeField] private float resultDisplaySeconds = 1.8f;
    [SerializeField] private KeyCode holdInputKey = KeyCode.Space;
    [SerializeField] private bool mouseHoldAlsoReels = true;

    private bool _uiActive;
    private bool _resultResolved;
    private CatchableDefinition _currentCatchable;

    private void Awake()
    {
        if (fishingView == null)
            fishingView = GetComponent<FishingMiniGameView>();

        if (miniGameLogic == null)
            miniGameLogic = GetComponent<FishingBarMiniGameLogic>();
    }

    private void OnEnable()
    {
        if (miniGameLogic != null)
        {
            miniGameLogic.OnStateChanged += HandleStateChanged;
            miniGameLogic.OnMiniGameFinished += HandleMiniGameFinished;
        }

        if (miniGameController != null)
            miniGameController.OnCatchComplete += HandleExternalCatchComplete;
    }

    private void OnDisable()
    {
        if (miniGameLogic != null)
        {
            miniGameLogic.OnStateChanged -= HandleStateChanged;
            miniGameLogic.OnMiniGameFinished -= HandleMiniGameFinished;
        }

        if (miniGameController != null)
            miniGameController.OnCatchComplete -= HandleExternalCatchComplete;
    }

    public void OpenFishingUI(LakeZoneDefinition zone = null)
    {
        if (_uiActive)
            return;

        if (miniGameController == null || miniGameLogic == null || fishingView == null || !fishingView.IsReady)
        {
            Debug.LogError("[FishingUIController_Hold] Missing required references for fishing mini-game.");
            return;
        }

        _uiActive = true;
        _resultResolved = false;

        miniGameController.StartFishing(zone);
        _currentCatchable = miniGameController.GetCurrentCatchable();

        fishingView.Show();
        fishingView.SetFishName(_currentCatchable != null ? _currentCatchable.catchableName : "Unknown Fish");
        fishingView.SetRarityText(BuildRarityText(_currentCatchable));
        fishingView.SetInputHint("Hold SPACE to move hook right. Release to drift left.");
        fishingView.SetStatus("Keep the fish inside the hook zone.", new Color(0.95f, 0.91f, 0.82f));

        float difficulty = _currentCatchable != null ? _currentCatchable.difficultyScore : 0.5f;
        miniGameLogic.Begin(difficulty);
    }

    public void CloseFishingUI()
    {
        if (!_uiActive)
            return;

        _uiActive = false;
        _resultResolved = false;
        _currentCatchable = null;

        if (miniGameLogic != null)
            miniGameLogic.Stop();

        if (fishingView != null)
            fishingView.Hide();
    }

    private void Update()
    {
        if (!_uiActive || _resultResolved || miniGameLogic == null)
            return;

        bool holding = Input.GetKey(holdInputKey);
        if (mouseHoldAlsoReels)
            holding = holding || Input.GetMouseButton(0);

        miniGameLogic.SetHolding(holding);
        miniGameLogic.Tick(Time.deltaTime);
    }

    private void HandleStateChanged(FishingBarSnapshot snapshot)
    {
        if (!_uiActive || fishingView == null)
            return;

        fishingView.RenderSnapshot(snapshot);

        if (snapshot.warning)
        {
            fishingView.SetStatus("Line tension is critical!", new Color(1f, 0.62f, 0.45f));
        }
        else if (snapshot.fishInsideZone)
        {
            fishingView.SetStatus("Nice control. Keep it steady.", new Color(0.72f, 0.96f, 0.72f));
        }
        else
        {
            fishingView.SetStatus("Track the fish with your hook zone.", new Color(0.93f, 0.91f, 0.8f));
        }
    }

    private void HandleMiniGameFinished(bool success, bool perfect)
    {
        if (_resultResolved)
            return;

        _resultResolved = true;

        if (success)
            GrantCatchToInventory();

        FishingResultType resultType = success ? FishingResultType.Success : FishingResultType.Escaped;
        miniGameController.CompleteCatch(resultType);

        if (fishingView != null)
        {
            string message;
            if (success && perfect)
                message = "Perfect Catch!";
            else if (success)
                message = "Caught!";
            else
                message = "The fish escaped";

            fishingView.ShowResult(message, success, perfect);
            fishingView.SetStatus(message, success ? new Color(0.72f, 0.98f, 0.72f) : new Color(1f, 0.65f, 0.52f));
        }

        StartCoroutine(CloseAfterDelay(resultDisplaySeconds));
    }

    private void HandleExternalCatchComplete(FishingResultType result)
    {
        if (!_uiActive)
            return;

        // If another system completes catch, close gracefully.
        if (!_resultResolved)
            StartCoroutine(CloseAfterDelay(resultDisplaySeconds));
    }

    private void GrantCatchToInventory()
    {
        if (_currentCatchable == null || _currentCatchable.inventoryItem == null)
            return;

        if (!InventoryController.HasInstance)
            return;

        bool added = InventoryController.Instance.TryAdd(_currentCatchable.inventoryItem, 1);
        if (!added)
            Debug.LogWarning($"[FishingUIController_Hold] Inventory full. Could not add {_currentCatchable.catchableName}.");
    }

    private static string BuildRarityText(CatchableDefinition catchable)
    {
        if (catchable == null)
            return "Rarity: Unknown";

        return $"Rarity: {catchable.rarity}";
    }

    private IEnumerator CloseAfterDelay(float delay)
    {
        float clamped = Mathf.Max(0.1f, delay);
        yield return new WaitForSeconds(clamped);
        CloseFishingUI();
    }
}
