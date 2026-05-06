using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Guided first-time farm tutorial shown only after intro->farm transition.
/// </summary>
public class FarmTutorialManager : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInstanceBeforeSceneLoad()
    {
        if (_instance == null)
        {
            var go = new GameObject("FarmTutorialManager_Global");
            go.AddComponent<FarmTutorialManager>();
            DontDestroyOnLoad(go);
        }
    }

    private const string PendingFarmTutorialKey = "PendingFarmTutorial";
    private const string FarmTutorialStartedKey = "FarmTutorialStarted";
    private const string FarmTutorialCompletedKey = "FarmTutorialCompleted";
    private static Sprite runtimeArrowSprite;

    private enum TutorialStep
    {
        None,
        OpenChest,
        OpenInventory,
        VisitHouse,
        VisitMarket,
        VisitRestaurant,
        RestaurantIntro,
        Complete
    }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private StarterToolsChest starterChest;
    [SerializeField] private FarmTutorialUIController tutorialUI;
    [SerializeField] private Transform waypointMarker;

    [Header("Targets")]
    [SerializeField] private Transform chestTarget;
    [SerializeField] private Transform houseTarget;
    [SerializeField] private Transform marketTarget;
    [SerializeField] private Transform restaurantTarget;

    [Header("Behavior")]
    [SerializeField] private float arriveDistance = 2.5f;
    [SerializeField] private float restaurantIntroDuration = 2.8f;
    [SerializeField] private KeyCode inventoryToggleKey = KeyCode.I;
    [SerializeField] private KeyCode skipKey = KeyCode.K;
    [SerializeField] private bool debugLogs = true;

    [Header("Waypoint Guide")]
    [SerializeField] private bool useDirectionalWaypoint = true;
    [SerializeField] private float waypointDirectionDistance = 3.2f;
    [SerializeField] private float waypointVerticalOffset = 1.35f;
    [SerializeField] private float waypointBobAmplitude = 0.16f;
    [SerializeField] private float waypointBobSpeed = 3f;
    [SerializeField] private Color runtimeWaypointColor = new Color(1f, 0.9f, 0.25f, 0.95f);
    [SerializeField] private int runtimeWaypointSortingOrder = 5000;
    [SerializeField] private float runtimeWaypointScale = 1.15f;
    [SerializeField] private bool createSecondaryRuntimeArrow = true;
    [SerializeField] private Vector3 secondaryArrowLocalOffset = new Vector3(0f, -0.45f, 0f);
    [SerializeField] private float secondaryArrowScaleMultiplier = 0.9f;
    [SerializeField] private float secondaryArrowAlpha = 0.78f;

    private TutorialStep currentStep = TutorialStep.None;
    private bool isRunning;
    private static FarmTutorialManager _instance;
    private bool forceDisabled = false;
    private bool houseVisited;
    private bool marketVisited;
    private bool restaurantVisited;
    private float nextWaitLogTime;
    private Transform activeWaypointTarget;
    private Vector3 waypointInitialScale;

    private void Start()
    {
        if (debugLogs)
        {
            int pending = PlayerPrefs.GetInt(PendingFarmTutorialKey, 0);
            int started = PlayerPrefs.GetInt(FarmTutorialStartedKey, 0);
            int completed = PlayerPrefs.GetInt(FarmTutorialCompletedKey, 0);
            Debug.Log($"[FarmTutorial] Flags on Start -> pending:{pending}, started:{started}, completed:{completed}");
        }

        // Only resolve refs on New Game to avoid creating arrows on Continue Game
        if (ShouldStartTutorial() || PlayerPrefs.GetInt(FarmTutorialStartedKey, 0) == 1)
        {
            ResolveReferences();
        }
        // Ensure this manager persists across scenes and only one instance exists
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        if (waypointMarker != null)
            waypointInitialScale = waypointMarker.localScale;

        if (!ShouldStartTutorial())
        {
            HideWaypoint();
            return;
        }

        BeginTutorial();
    }

    private void Awake()
    {
        // Establish singleton and ensure waypoint marker exists early (before Start)
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Only resolve refs if tutorial should run (to avoid creating arrows on Continue Game)
        if (ShouldStartTutorial())
        {
            ResolveReferences();
        }
    }

    private void Update()
    {
        // Global 'K' key should disable tutorial immediately from any scene
        if (Input.GetKeyDown(skipKey))
        {
            // If running, complete (skip); if not running, disable future starts
            if (isRunning)
                CompleteTutorial(true);
            else
                DisableTutorialImmediate();

            return;
        }

        if (!isRunning)
            return;

        UpdateWaypointGuide();

        if (Input.GetKeyDown(skipKey))
        {
            CompleteTutorial(true);
            return;
        }

        if (currentStep == TutorialStep.RestaurantIntro)
            return;

        if (currentStep == TutorialStep.OpenInventory)
        {
            if (Input.GetKeyDown(inventoryToggleKey))
            {
                MoveToStep(TutorialStep.VisitHouse);
            }
            else if (debugLogs && Time.time >= nextWaitLogTime)
            {
                nextWaitLogTime = Time.time + 3f;
                Debug.Log($"[FarmTutorial] Waiting for inventory open key '{inventoryToggleKey}' to continue.");
            }

            return;
        }

        if (currentStep == TutorialStep.OpenChest)
        {
            if (starterChest == null)
                starterChest = FindFirstObjectByType<StarterToolsChest>();

            if (chestTarget == null && starterChest != null)
                chestTarget = starterChest.transform;

            if (starterChest != null && (starterChest.IsLooted || starterChest.IsOpen))
            {
                MoveToStep(TutorialStep.VisitHouse);
            }
            else if (debugLogs && Time.time >= nextWaitLogTime)
            {
                nextWaitLogTime = Time.time + 3f;
                Debug.Log("[FarmTutorial] Waiting for chest interaction (open or looted) to continue.");
            }
            return;
        }

        if (player == null)
            return;

        Transform target = GetTargetForStep(currentStep);
        if (target == null)
        {
            if (debugLogs && Time.time >= nextWaitLogTime)
            {
                nextWaitLogTime = Time.time + 3f;
                Debug.Log($"[FarmTutorial] Waiting for target assignment on step '{currentStep}'. Assign target transforms or use zone triggers.");
            }
            return;
        }

        float sqrDist = (player.position - target.position).sqrMagnitude;
        if (sqrDist <= arriveDistance * arriveDistance)
        {
            switch (currentStep)
            {
                case TutorialStep.VisitHouse:
                    MarkZoneVisited(FarmTutorialZoneType.House);
                    break;
                case TutorialStep.VisitMarket:
                    MarkZoneVisited(FarmTutorialZoneType.Market);
                    break;
                case TutorialStep.VisitRestaurant:
                    MarkZoneVisited(FarmTutorialZoneType.Restaurant);
                    break;
            }
        }
    }

    private void ResolveReferences()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (starterChest == null)
            starterChest = FindFirstObjectByType<StarterToolsChest>();

        if (chestTarget == null && starterChest != null)
            chestTarget = starterChest.transform;

        if (chestTarget == null)
            chestTarget = FindTransformByNames("StarterToolsChest", "ToolChest", "Chest");

        if (houseTarget == null)
            houseTarget = FindTransformByNames("HouseReturnSpawnPoint", "HouseSpawnPoint", "House", "FarmHouse");

        if (marketTarget == null)
            marketTarget = FindTransformByNames("MarketSpawnPoint", "MarketEntrance", "Market", "MarketScenePortal");

        if (restaurantTarget == null)
            restaurantTarget = FindTransformByNames("SpawnPointResto", "RestaurantEntrance", "Restaurant", "RestoEntrance", "Resto");

        if (tutorialUI == null)
            tutorialUI = FindFirstObjectByType<FarmTutorialUIController>();

        if (tutorialUI == null)
        {
            GameObject tutorialUiRoot = new GameObject("FarmTutorialUIRoot");
            tutorialUI = tutorialUiRoot.AddComponent<FarmTutorialUIController>();
        }

        EnsureWaypointMarker();
    }

    private void EnsureWaypointMarker()
    {
        // Safety check: only create arrows if tutorial is meant to run (guards against accidental calls)
        if (!ShouldStartTutorial() && PlayerPrefs.GetInt(FarmTutorialCompletedKey, 0) == 0)
        {
            if (debugLogs)
                Debug.Log("[FarmTutorial] Skipping waypoint marker creation: tutorial should not run in this session.");
            return;
        }

        // Do not create or enable the waypoint marker in the Intro scene.
        string activeSceneName = SceneManager.GetActiveScene().name;
        if (activeSceneName == "Intro")
        {
            GameObject maybeExisting = GameObject.Find("FarmTutorialWaypoint");
            if (maybeExisting != null)
                maybeExisting.SetActive(false);
            if (debugLogs)
                Debug.Log("[FarmTutorial] Skipping waypoint creation in Intro scene.");
            return;
        }

        // If a marker already exists in the scene (possible from previous runs), reuse it.
        GameObject existing = GameObject.Find("FarmTutorialWaypoint");
        if (existing != null)
        {
            waypointMarker = existing.transform;
            // Ensure it's parented to this manager and preserved across scenes
            waypointMarker.SetParent(transform, false);
            DontDestroyOnLoad(waypointMarker.gameObject);
            waypointMarker.localScale = Vector3.one * runtimeWaypointScale;
            // Make sure sprite is set (in case it was created without one)
            SpriteRenderer sr = waypointMarker.GetComponent<SpriteRenderer>();
            if (sr == null)
                sr = waypointMarker.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrCreateRuntimeArrowSprite();
            sr.color = runtimeWaypointColor;
            sr.sortingOrder = runtimeWaypointSortingOrder;
            return;
        }

        GameObject marker = new GameObject("FarmTutorialWaypoint");
        marker.transform.SetParent(transform, false);
        SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
        renderer.sprite = GetOrCreateRuntimeArrowSprite();
        renderer.color = runtimeWaypointColor;
        renderer.sortingOrder = runtimeWaypointSortingOrder;

        if (createSecondaryRuntimeArrow)
        {
            GameObject secondArrow = new GameObject("FarmTutorialWaypoint_Secondary");
            secondArrow.transform.SetParent(marker.transform, false);
            secondArrow.transform.localPosition = secondaryArrowLocalOffset;
            secondArrow.transform.localScale = Vector3.one * Mathf.Max(0.1f, secondaryArrowScaleMultiplier);

            SpriteRenderer secondRenderer = secondArrow.AddComponent<SpriteRenderer>();
            secondRenderer.sprite = renderer.sprite;
            secondRenderer.sortingOrder = runtimeWaypointSortingOrder - 1;

            Color secondaryColor = runtimeWaypointColor;
            secondaryColor.a *= Mathf.Clamp01(secondaryArrowAlpha);
            secondRenderer.color = secondaryColor;
        }

        waypointMarker = marker.transform;
        waypointMarker.localScale = Vector3.one * runtimeWaypointScale;
        DontDestroyOnLoad(marker);

        if (debugLogs)
            Debug.Log("[FarmTutorial] No waypoint marker assigned. Created runtime arrow marker.");
    }

    private static Sprite GetOrCreateRuntimeArrowSprite()
    {
        if (runtimeArrowSprite != null)
            return runtimeArrowSprite;

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color clear = new Color(0f, 0f, 0f, 0f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
                texture.SetPixel(x, y, clear);
        }

        // Arrow head triangle + rectangular tail.
        for (int y = 0; y < size; y++)
        {
            int centerX = size / 2;

            if (y >= 20)
            {
                int halfWidth = Mathf.RoundToInt((y - 20) * 0.55f);
                int min = Mathf.Clamp(centerX - halfWidth, 0, size - 1);
                int max = Mathf.Clamp(centerX + halfWidth, 0, size - 1);
                for (int x = min; x <= max; x++)
                    texture.SetPixel(x, y, Color.white);
            }
            else
            {
                for (int x = centerX - 7; x <= centerX + 7; x++)
                {
                    if (x >= 0 && x < size)
                        texture.SetPixel(x, y, Color.white);
                }
            }
        }

        texture.Apply();
        runtimeArrowSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.15f), 64f);
        runtimeArrowSprite.name = "RuntimeFarmTutorialArrow";
        return runtimeArrowSprite;
    }

    private bool ShouldStartTutorial()
    {
        if (forceDisabled)
        {
            if (debugLogs)
                Debug.Log("[FarmTutorial] Not starting: tutorial was force-disabled by player.");
            return false;
        }
        if (SceneManager.GetActiveScene().name != "FarmScene")
        {
            if (debugLogs)
                Debug.Log("[FarmTutorial] Not starting: current scene is not FarmScene.");
            return false;
        }

        if (PlayerPrefs.GetInt(FarmTutorialCompletedKey, 0) == 1)
        {
            if (debugLogs)
                Debug.Log("[FarmTutorial] Not starting: FarmTutorialCompleted is already set.");
            return false;
        }

        int pending = PlayerPrefs.GetInt(PendingFarmTutorialKey, 0);

        // Start once only when Intro explicitly requests it.
        bool shouldStart = pending == 1;

        if (!shouldStart && debugLogs)
            Debug.Log("[FarmTutorial] Not starting: intro handoff flag is not pending. Tutorial runs only once after intro.");

        if (shouldStart && debugLogs)
            Debug.Log($"[FarmTutorial] Start gate passed (pending:{pending}).");

        return shouldStart;
    }

    private void BeginTutorial()
    {
        isRunning = true;
        PlayerPrefs.SetInt(FarmTutorialStartedKey, 1);
        PlayerPrefs.DeleteKey(PendingFarmTutorialKey);
        PlayerPrefs.Save();

        if (starterChest != null)
            starterChest.LootCollected += OnChestLootCollected;

        MoveToStep(TutorialStep.OpenChest);

        if (debugLogs)
            Debug.Log("[FarmTutorial] Started.");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Re-resolve scene-local references after scene load, but only if tutorial should run or is running
        if (ShouldStartTutorial() || isRunning)
        {
            ResolveReferences();
        }

        // If we just arrived to FarmScene and the intro handoff is pending, start the tutorial.
        if (scene.name == "FarmScene" && ShouldStartTutorial())
        {
            BeginTutorial();
            return;
        }

        // If tutorial is already running, re-apply the current step so UI and waypoint restore correctly.
        if (isRunning)
            MoveToStep(currentStep);
    }

    private void MoveToStep(TutorialStep step)
    {
        currentStep = step;
        ResolveReferences();

        switch (step)
        {
            case TutorialStep.OpenChest:
                ShowMessage("This is your farm now. Walk to the chest and press E to open it.\nPress K to skip the tutorial.");
                SetWaypoint(chestTarget);
                break;
            case TutorialStep.OpenInventory:
                ShowMessage($"Great. Press {inventoryToggleKey} to open your inventory and check your tools.");
                HideWaypoint();
                break;
            case TutorialStep.VisitHouse:
                ShowMessage("Now follow the guide arrow to your house. This is your shelter and home base.");
                SetWaypoint(houseTarget);
                break;
            case TutorialStep.VisitMarket:
                ShowMessage("Good. Follow the guide arrow to the market. You can buy seeds and useful supplies there.");
                SetWaypoint(marketTarget);
                break;
            case TutorialStep.VisitRestaurant:
                ShowMessage("Now follow the guide arrow to the restaurant.");
                SetWaypoint(restaurantTarget);
                break;
            case TutorialStep.RestaurantIntro:
                ShowMessage("This is the restaurant this is where u will cook and serve customers");
                HideWaypoint();
                CancelInvoke(nameof(AdvanceFromRestaurantIntro));
                Invoke(nameof(AdvanceFromRestaurantIntro), restaurantIntroDuration);
                break;
            case TutorialStep.Complete:
                CompleteTutorial(false);
                break;
        }
    }

    private void AdvanceFromRestaurantIntro()
    {
        if (!isRunning || currentStep != TutorialStep.RestaurantIntro)
            return;

        MoveToStep(TutorialStep.Complete);
    }

    private void OnDestroy()
    {
        // Ensure we never leave the static sceneLoaded handler referencing a destroyed instance.
        try
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        catch { }

        if (_instance == this)
            _instance = null;
    }

    private void OnChestLootCollected()
    {
        if (!isRunning || currentStep != TutorialStep.OpenChest)
            return;

        MoveToStep(TutorialStep.OpenInventory);
    }

    public void MarkZoneVisited(FarmTutorialZoneType zoneType)
    {
        if (!isRunning)
            return;

        if (zoneType == FarmTutorialZoneType.House)
            houseVisited = true;

        if (zoneType == FarmTutorialZoneType.Market)
            marketVisited = true;

        if (zoneType == FarmTutorialZoneType.Restaurant)
            restaurantVisited = true;

        if (currentStep == TutorialStep.VisitHouse && houseVisited)
        {
            MoveToStep(TutorialStep.VisitMarket);
            return;
        }

        if (currentStep == TutorialStep.VisitMarket && marketVisited)
        {
            MoveToStep(TutorialStep.VisitRestaurant);
            return;
        }

        if (currentStep == TutorialStep.VisitRestaurant && restaurantVisited)
        {
            MoveToStep(TutorialStep.RestaurantIntro);
        }
    }

    private Transform GetTargetForStep(TutorialStep step)
    {
        switch (step)
        {
            case TutorialStep.OpenChest:
                return chestTarget;
            case TutorialStep.OpenInventory:
                return null;
            case TutorialStep.VisitHouse:
                return houseTarget;
            case TutorialStep.VisitMarket:
                return marketTarget;
            case TutorialStep.VisitRestaurant:
                return restaurantTarget;
            case TutorialStep.RestaurantIntro:
                return null;
            default:
                return null;
        }
    }

    private void ShowMessage(string message)
    {
        if (tutorialUI != null)
            tutorialUI.ShowMessage(message);
    }

    private void CompleteTutorial(bool skipped)
    {
        isRunning = false;
        currentStep = TutorialStep.Complete;
        CancelInvoke(nameof(AdvanceFromRestaurantIntro));

        if (starterChest != null)
            starterChest.LootCollected -= OnChestLootCollected;

        if (tutorialUI != null)
        {
            if (skipped)
                tutorialUI.ShowMessage("You can explore at your own pace now.");

            Invoke(nameof(HideTutorialText), skipped ? 1.8f : 1.2f);
        }

        HideWaypoint();

        PlayerPrefs.SetInt(FarmTutorialCompletedKey, 1);
        PlayerPrefs.DeleteKey(FarmTutorialStartedKey);
        PlayerPrefs.DeleteKey(PendingFarmTutorialKey);
        PlayerPrefs.Save();

        // Unsubscribe from sceneLoaded to avoid dangling handlers
        if (_instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;

        if (debugLogs)
            Debug.Log(skipped ? "[FarmTutorial] Skipped and completed." : "[FarmTutorial] Completed.");
    }

    private void DisableTutorialImmediate()
    {
        // Fully disable and mark as completed so it won't start again
        isRunning = false;
        forceDisabled = true;

        if (starterChest != null)
            starterChest.LootCollected -= OnChestLootCollected;

        HideWaypoint();
        if (tutorialUI != null)
            tutorialUI.HideMessage();

        PlayerPrefs.SetInt(FarmTutorialCompletedKey, 1);
        PlayerPrefs.DeleteKey(FarmTutorialStartedKey);
        PlayerPrefs.DeleteKey(PendingFarmTutorialKey);
        PlayerPrefs.Save();

        if (debugLogs)
            Debug.Log("[FarmTutorial] Disabled immediately by player (K).");
    }

    private void HideTutorialText()
    {
        if (tutorialUI != null)
            tutorialUI.HideMessage();
    }

    private void SetWaypoint(Transform target)
    {
        activeWaypointTarget = target;

        if (waypointMarker == null)
            return;

        if (target == null)
        {
            waypointMarker.gameObject.SetActive(false);
            return;
        }

        waypointMarker.gameObject.SetActive(true);
        waypointMarker.position = target.position + new Vector3(0f, 1.35f, 0f);
    }

    private void HideWaypoint()
    {
        activeWaypointTarget = null;

        if (waypointMarker != null)
            waypointMarker.gameObject.SetActive(false);
    }

    private void UpdateWaypointGuide()
    {
        if (waypointMarker == null || activeWaypointTarget == null)
            return;

        float bobOffset = Mathf.Sin(Time.time * waypointBobSpeed) * waypointBobAmplitude;

        if (!useDirectionalWaypoint || player == null)
        {
            waypointMarker.position = activeWaypointTarget.position + new Vector3(0f, waypointVerticalOffset + bobOffset, 0f);
            return;
        }

        Vector3 direction = activeWaypointTarget.position - player.position;
        direction.z = 0f;

        if (direction.sqrMagnitude < 0.01f)
            direction = Vector3.up;

        direction.Normalize();

        Vector3 markerPosition = player.position + direction * waypointDirectionDistance;
        markerPosition.z = activeWaypointTarget.position.z;
        markerPosition.y += waypointVerticalOffset + bobOffset;
        waypointMarker.position = markerPosition;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        // Flip orientation so arrow visually points FROM player TOWARD the target.
        waypointMarker.rotation = Quaternion.Euler(0f, 0f, angle + 90f);
        waypointMarker.localScale = waypointInitialScale == Vector3.zero ? Vector3.one : waypointInitialScale;
    }

    private Transform FindTransformByNames(params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            GameObject obj = GameObject.Find(names[i]);
            if (obj != null)
                return obj.transform;
        }

        return null;
    }

    [ContextMenu("Tutorial Debug/Force Start Next Farm Load")]
    private void DebugForceStartNextLoad()
    {
        PlayerPrefs.SetInt(PendingFarmTutorialKey, 1);
        PlayerPrefs.DeleteKey(FarmTutorialCompletedKey);
        PlayerPrefs.Save();
        Debug.Log("[FarmTutorial] Debug: pending set to 1, completed cleared. Reload FarmScene to test tutorial start.");
    }

    [ContextMenu("Tutorial Debug/Reset Tutorial Progress")]
    private void DebugResetTutorialProgress()
    {
        PlayerPrefs.DeleteKey(PendingFarmTutorialKey);
        PlayerPrefs.DeleteKey(FarmTutorialStartedKey);
        PlayerPrefs.DeleteKey(FarmTutorialCompletedKey);
        PlayerPrefs.Save();
        Debug.Log("[FarmTutorial] Debug: all tutorial progress flags cleared.");
    }
}
