using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CameraFollowFix : MonoBehaviour
{
    private static CameraFollowFix instance;
    private CinemachineCamera cam;
    private Transform currentTarget;

    [Header("Scene Lens")]
    [SerializeField] private string marketSceneName = "MarketScene";
    [SerializeField] private float marketOrthographicSize = 10f;
    [SerializeField] private string houseSceneName = "HouseInteriorLITEDEMO";

    [Header("Farm Map Zoom")]
    [SerializeField] private string farmSceneName = "FarmScene";
    [SerializeField] private KeyCode mapZoomKey = KeyCode.M;
    [SerializeField] private float fallbackFarmMapOrthographicSize = 35f;
    [SerializeField] private float farmMapViewPadding = 2f;
    [SerializeField] private float mapZoomSpeed = 8f;
    [SerializeField] private bool holdKeyForMapView;
    [SerializeField] private Vector3 playerMarkerOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] private float playerMarkerScale = 1.1f;
    [SerializeField] private float playerMarkerScreenSize = 34f;
    [SerializeField] private bool showMapKeyHint = true;
    [SerializeField] private Vector3 mapTilemapCullingBounds = new Vector3(8f, 8f, 8f);
    [SerializeField, Min(1)] private int transitionSnapFrames = 12;

    private float originalOrthographicSize;
    private bool originalOrthographicSizeCaptured;
    private float sceneOrthographicSize;
    private float mapOrthographicSize;
    private bool mapViewActive;
    private Transform mapViewTarget;
    private GameObject playerMarker;
    private Sprite playerMarkerSprite;
    private Texture2D playerMarkerTexture;
    private CharacterController2D playerController;
    private Coroutine assignPlayerRoutine;
    private Coroutine transitionSnapRoutine;
    private Coroutine targetValidationRoutine;
    private readonly List<TilemapCullingState> tilemapCullingStates = new List<TilemapCullingState>();

    // Target validation fields
    private int currentTargetInstanceId = -1;
    private string lastValidTargetName = "";
    private float lastTargetValidationTime = 0f;
    [SerializeField] private float targetValidationInterval = 1f;  // Validate target every 1 second

    public static CameraFollowFix Instance => instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            GameObject duplicateRoot = transform.root.gameObject;
            ClearEditorSelectionIfNeeded(duplicateRoot);
            Destroy(duplicateRoot);
            return;
        }

        instance = this;
        cam = GetComponent<CinemachineCamera>();

        if (cam != null)
        {
            var lens = cam.Lens;
            originalOrthographicSize = lens.OrthographicSize;
            sceneOrthographicSize = originalOrthographicSize;
            originalOrthographicSizeCaptured = true;
        }

        DontDestroyOnLoad(transform.root.gameObject); // persist whole camera rig
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        RestoreTilemapCullingBounds();
        SetHotbarSuppressed(false);
        SetPlayerMovementLocked(false);
    }

    void Update()
    {
        if (cam == null)
            return;

        // CRITICAL: If currentTarget is null, attempt recovery immediately
        if (currentTarget == null)
        {
            Debug.LogError($"[CameraFollowFix.Update] CRITICAL: currentTarget is null! Camera may be broken. Attempting immediate recovery...");

            // Try to find player immediately
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Debug.Log($"[CameraFollowFix.Update] Found player during recovery: {player.name}");
                AssignTargetNow(player.transform, true);
            }
            else
            {
                Debug.LogError($"[CameraFollowFix.Update] Player not found during recovery attempt. Triggering AssignPlayer coroutine...");

                if (assignPlayerRoutine != null)
                    StopCoroutine(assignPlayerRoutine);

                assignPlayerRoutine = StartCoroutine(AssignPlayer());
            }
            return;  // Skip rest of update until target is recovered
        }

        // Periodically validate that the current target is still valid
        ValidateCurrentTarget();

        bool inFarmScene = SceneManager.GetActiveScene().name == farmSceneName;
        if (!inFarmScene)
        {
            SetMapViewActive(false);
            return;
        }

        if (holdKeyForMapView)
            SetMapViewActive(Input.GetKey(mapZoomKey));
        else if (Input.GetKeyDown(mapZoomKey))
            SetMapViewActive(!mapViewActive);

        UpdateLensZoom();
        UpdatePlayerMarker();
    }

    void OnGUI()
    {
        DrawMapKeyHint();

        if (!mapViewActive || currentTarget == null)
            return;

        UnityEngine.Camera mainCamera = UnityEngine.Camera.main;
        if (mainCamera == null)
            return;

        Vector3 screenPosition = mainCamera.WorldToScreenPoint(currentTarget.position);
        if (screenPosition.z < 0f)
            return;

        float size = playerMarkerScreenSize;
        Rect markerRect = new Rect(
            screenPosition.x - size * 0.5f,
            Screen.height - screenPosition.y - size,
            size,
            size);

        GUI.DrawTexture(markerRect, GetPlayerMarkerTexture(), ScaleMode.ScaleToFit, true);
    }

    private void DrawMapKeyHint()
    {
        if (!showMapKeyHint || SceneManager.GetActiveScene().name != farmSceneName)
            return;

        const float width = 92f;
        const float height = 34f;
        Rect hintRect = new Rect(Screen.width - width - 18f, Screen.height - height - 18f, width, height);

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.fontSize = 15;
        boxStyle.fontStyle = FontStyle.Bold;
        boxStyle.alignment = TextAnchor.MiddleCenter;
        boxStyle.normal.textColor = Color.white;

        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.Box(hintRect, GUIContent.none);

        GUI.color = Color.white;
        GUI.Label(hintRect, mapViewActive ? "M  Close" : "M  Map", boxStyle);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[CameraFollowFix] OnSceneLoaded called for scene: {scene.name} at time {Time.time:F2}s");

        SetMapViewActive(false);
        ApplySceneLensOverride(scene.name);

        if (assignPlayerRoutine != null)
        {
            Debug.Log("[CameraFollowFix] Stopping existing AssignPlayer routine");
            StopCoroutine(assignPlayerRoutine);
        }

        Debug.Log("[CameraFollowFix] Starting new AssignPlayer routine");
        assignPlayerRoutine = StartCoroutine(AssignPlayer());
    }

    private void ApplySceneLensOverride(string sceneName)
    {
        float targetOrthographicSize = originalOrthographicSize;
        if (sceneName == marketSceneName)
            targetOrthographicSize = marketOrthographicSize;
        else if (sceneName == houseSceneName)
            targetOrthographicSize = 5f;

        sceneOrthographicSize = targetOrthographicSize;

        if (cam != null)
        {
            var lens = cam.Lens;

            if (!originalOrthographicSizeCaptured)
            {
                originalOrthographicSize = lens.OrthographicSize;
                originalOrthographicSizeCaptured = true;
            }

            lens.OrthographicSize = targetOrthographicSize;
            cam.Lens = lens;
        }
    }

    IEnumerator AssignPlayer()
    {
        GameObject player = null;
        int searchAttempts = 0;
        float startTime = Time.time;

        // Keep trying until the persistent player has survived or been spawned into the new scene.
        while (player == null)
        {
            searchAttempts++;
            // Only log warnings if the search is taking unusually long (after 30+ frames / ~0.5s)
            if (searchAttempts > 30 && searchAttempts % 30 == 0)
                Debug.LogWarning($"[CameraFollowFix.AssignPlayer] Still searching for player... (attempt {searchAttempts}, elapsed {Time.time - startTime:F2}s)");

            player = GameObject.FindGameObjectWithTag("Player");

            // Safety check: if we've been searching too long, log error
            if (searchAttempts > 300)  // 5+ seconds at 60fps
            {
                Debug.LogError($"[CameraFollowFix.AssignPlayer] Failed to find player after {searchAttempts} attempts ({Time.time - startTime:F2}s elapsed). This indicates a serious timing issue.");
                assignPlayerRoutine = null;
                yield break;
            }

            yield return null; // wait next frame
        }

        // Only log success if it took more than one frame (normal is 1-3 frames)
        if (searchAttempts > 1)
            Debug.Log($"[CameraFollowFix.AssignPlayer] Player found after {searchAttempts} attempts ({Time.time - startTime:F2}s). Player: {player.name} at position {player.transform.position}");

        // Validate player is active and has required components
        if (!player.activeInHierarchy)
            Debug.LogWarning($"[CameraFollowFix.AssignPlayer] WARNING: Found player is NOT active in hierarchy!");

        if (player.GetComponent<CharacterController2D>() == null)
            Debug.LogWarning($"[CameraFollowFix.AssignPlayer] WARNING: Player has no CharacterController2D component!");

        AssignTargetNow(player.transform, true);
        assignPlayerRoutine = null;
    }

    public void AssignTargetNow(Transform target)
    {
        AssignTargetNow(target, false);
    }

    public void AssignTargetNow(Transform target, bool snapCamera)
    {
        if (cam == null)
        {
            Debug.LogError($"[CameraFollowFix.AssignTargetNow] CinemachineCamera is null! Cannot assign target.");
            return;
        }

        if (target == null)
        {
            Debug.LogError($"[CameraFollowFix.AssignTargetNow] Target is null! Cannot assign to camera.");
            return;
        }

        Debug.Log($"[CameraFollowFix.AssignTargetNow] Assigning target: {target.name} at position {target.position}, snapCamera={snapCamera}");

        currentTarget = target;
        currentTargetInstanceId = target.gameObject.GetInstanceID();
        lastValidTargetName = target.name;
        lastTargetValidationTime = Time.time;
        playerController = target.GetComponent<CharacterController2D>();

        // Project primarily uses TrackingTarget. Keep Follow for compatibility.
        cam.Target.TrackingTarget = target;
        cam.Follow = target;

        Debug.Log($"[CameraFollowFix.AssignTargetNow] Successfully assigned camera target. TrackingTarget: {cam.Target.TrackingTarget?.name ?? "null"}, Follow: {cam.Follow?.name ?? "null"}");

        if (snapCamera)
            SnapCameraToTarget(target);
    }

    public static void RebindAllCamerasTo(Transform target, bool snapCamera = true)
    {
        if (target == null)
        {
            Debug.LogError("[CameraFollowFix.RebindAllCamerasTo] Target is null! Cannot rebind cameras.");
            return;
        }

        Debug.Log($"[CameraFollowFix.RebindAllCamerasTo] Rebinding all cameras to target: {target.name} at position {target.position}, snapCamera={snapCamera}");

        CinemachineCamera[] cineCams = Object.FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        Debug.Log($"[CameraFollowFix.RebindAllCamerasTo] Found {cineCams.Length} CinemachineCamera(s)");

        for (int i = 0; i < cineCams.Length; i++)
        {
            if (cineCams[i] == null)
                continue;

            Debug.Log($"[CameraFollowFix.RebindAllCamerasTo] Rebinding camera {i}: {cineCams[i].name}");
            cineCams[i].Target.TrackingTarget = target;
            cineCams[i].Follow = target;
        }

        CameraFollowFix followFix = instance != null ? instance : Object.FindFirstObjectByType<CameraFollowFix>();
        if (followFix != null)
        {
            Debug.Log($"[CameraFollowFix.RebindAllCamerasTo] Found CameraFollowFix instance, calling AssignTargetNow");
            followFix.AssignTargetNow(target, snapCamera);
        }
        else
        {
            Debug.LogWarning($"[CameraFollowFix.RebindAllCamerasTo] CameraFollowFix instance not found, snapping main camera");
            if (snapCamera)
                SnapMainCameraTo(target);
        }
    }

    private void SnapCameraToTarget(Transform target)
    {
        if (target == null)
            return;

        if (cam != null)
        {
            Vector3 p = target.position;
            Vector3 c = cam.transform.position;
            cam.transform.position = new Vector3(p.x, p.y, c.z);
        }

        SnapMainCameraTo(target);

        if (transitionSnapRoutine != null)
            StopCoroutine(transitionSnapRoutine);

        transitionSnapRoutine = StartCoroutine(SnapCameraForFrames(target, transitionSnapFrames));
    }

    private void ValidateCurrentTarget()
    {
        // Periodically validate that the current target is still valid
        if (Time.time - lastTargetValidationTime < targetValidationInterval)
            return;

        lastTargetValidationTime = Time.time;

        // Check if currentTarget is still valid
        if (currentTarget == null)
        {
            Debug.LogWarning($"[CameraFollowFix.ValidateCurrentTarget] Current target became null! Triggering player search...");

            // Try to find player again
            if (assignPlayerRoutine != null)
                StopCoroutine(assignPlayerRoutine);

            assignPlayerRoutine = StartCoroutine(AssignPlayer());
            return;
        }

        // Check if the instance ID matches (ensure it's the same object)
        if (currentTarget.gameObject.GetInstanceID() != currentTargetInstanceId)
        {
            Debug.LogWarning($"[CameraFollowFix.ValidateCurrentTarget] Target instance ID mismatch! Expected {currentTargetInstanceId}, but current is {currentTarget.gameObject.GetInstanceID()}. This shouldn't happen.");
            return;
        }

        // Check if target is still active
        if (!currentTarget.gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"[CameraFollowFix.ValidateCurrentTarget] Current target '{currentTarget.name}' is NOT active in hierarchy. Camera may not follow properly.");
            return;
        }

        // Verify camera is still properly assigned
        if (cam != null)
        {
            if (cam.Target.TrackingTarget != currentTarget || cam.Follow != currentTarget)
            {
                Debug.LogWarning($"[CameraFollowFix.ValidateCurrentTarget] Camera target assignments lost! Resetting...");
                Debug.Log($"  TrackingTarget: {cam.Target.TrackingTarget?.name ?? "null"} (expected {currentTarget.name})");
                Debug.Log($"  Follow: {cam.Follow?.name ?? "null"} (expected {currentTarget.name})");

                // Re-assign to restore
                cam.Target.TrackingTarget = currentTarget;
                cam.Follow = currentTarget;

                Debug.Log($"[CameraFollowFix.ValidateCurrentTarget] Camera targets restored.");
            }
        }
    }

    private IEnumerator SnapCameraForFrames(Transform target, int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            if (target == null)
                break;

            if (cam != null)
            {
                cam.Target.TrackingTarget = target;
                cam.Follow = target;

                Vector3 p = target.position;
                Vector3 c = cam.transform.position;
                cam.transform.position = new Vector3(p.x, p.y, c.z);
            }

            SnapMainCameraTo(target);
            yield return null;
        }

        transitionSnapRoutine = null;
    }

    private static void SnapMainCameraTo(Transform target)
    {
        if (target == null)
            return;

        UnityEngine.Camera mainCamera = EnsureMainCamera(target.position);

        Vector3 p = target.position;
        Vector3 c = mainCamera.transform.position;
        mainCamera.transform.position = new Vector3(p.x, p.y, c.z);

        RuntimeFallbackCameraFollow fallbackFollow = mainCamera.GetComponent<RuntimeFallbackCameraFollow>();
        if (fallbackFollow != null)
            fallbackFollow.SetTarget(target);
    }

    private static UnityEngine.Camera EnsureMainCamera(Vector3 targetPosition)
    {
        UnityEngine.Camera mainCamera = UnityEngine.Camera.main;
        if (mainCamera != null)
            return mainCamera;

        GameObject cameraObject = new GameObject("Runtime Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(targetPosition.x, targetPosition.y, -10f);

        mainCamera = cameraObject.AddComponent<UnityEngine.Camera>();
        mainCamera.orthographic = true;
        mainCamera.orthographicSize = GetFallbackOrthographicSizeForActiveScene();
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color(0.011f, 0.011f, 0.011f, 0f);
        cameraObject.AddComponent<RuntimeFallbackCameraFollow>();

        if (Object.FindFirstObjectByType<AudioListener>() == null)
            cameraObject.AddComponent<AudioListener>();

        Object.DontDestroyOnLoad(cameraObject);
        return mainCamera;
    }

    private static float GetFallbackOrthographicSizeForActiveScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "MarketScene")
            return 10f;

        if (sceneName == "HouseInteriorLITEDEMO")
            return 5f;

        return 5f;
    }

    private void UpdateLensZoom()
    {
        float targetSize = mapViewActive ? mapOrthographicSize : sceneOrthographicSize;
        var lens = cam.Lens;
        lens.OrthographicSize = Mathf.Lerp(lens.OrthographicSize, targetSize, Time.unscaledDeltaTime * mapZoomSpeed);

        if (Mathf.Abs(lens.OrthographicSize - targetSize) < 0.01f)
            lens.OrthographicSize = targetSize;

        cam.Lens = lens;
    }

    private void SetMapViewActive(bool active)
    {
        if (mapViewActive == active)
            return;

        mapViewActive = active;
        SetPlayerMovementLocked(mapViewActive);
        SetHotbarSuppressed(mapViewActive);

        if (mapViewActive)
        {
            ExpandTilemapCullingBoundsForMap();
            FocusMapViewOnMainFarmTilemaps();
        }
        else if (currentTarget != null && cam != null)
        {
            RestoreTilemapCullingBounds();
            cam.Target.TrackingTarget = currentTarget;
            cam.Follow = currentTarget;
        }

        if (!mapViewActive && playerMarker != null)
            playerMarker.SetActive(false);
    }

    private void UpdatePlayerMarker()
    {
        if (!mapViewActive || currentTarget == null)
        {
            if (playerMarker != null)
                playerMarker.SetActive(false);

            return;
        }

        if (playerMarker == null)
            return;

        playerMarker.SetActive(false);
        playerMarker.transform.position = currentTarget.position + playerMarkerOffset;
        playerMarker.transform.localScale = Vector3.one * CalculatePlayerMarkerScale();
    }

    private void EnsurePlayerMarker()
    {
        if (playerMarker != null)
            return;

        playerMarker = new GameObject("FarmMapPlayerMarker");
        playerMarker.hideFlags = HideFlags.HideAndDontSave;
        playerMarker.transform.SetParent(transform.root, false);

        SpriteRenderer renderer = playerMarker.AddComponent<SpriteRenderer>();
        renderer.sprite = GetPlayerMarkerSprite();
        renderer.color = new Color(1f, 0.05f, 0.03f, 1f);
        renderer.sortingOrder = 10000;

        playerMarker.transform.localScale = Vector3.one * CalculatePlayerMarkerScale();
    }

    private void FocusMapViewOnMainFarmTilemaps()
    {
        EnsureMapViewTarget();

        Bounds mapBounds;
        if (TryGetMainFarmTilemapBounds(out mapBounds))
        {
            mapViewTarget.position = new Vector3(mapBounds.center.x, mapBounds.center.y, currentTarget != null ? currentTarget.position.z : 0f);
            mapOrthographicSize = CalculateOrthographicSizeForBounds(mapBounds);
        }
        else
        {
            mapViewTarget.position = currentTarget != null ? currentTarget.position : Vector3.zero;
            mapOrthographicSize = fallbackFarmMapOrthographicSize;
        }

        cam.Target.TrackingTarget = mapViewTarget;
        cam.Follow = mapViewTarget;
    }

    private void EnsureMapViewTarget()
    {
        if (mapViewTarget != null)
            return;

        GameObject targetObject = new GameObject("FarmMapViewTarget");
        targetObject.hideFlags = HideFlags.HideAndDontSave;
        targetObject.transform.SetParent(transform.root, false);
        mapViewTarget = targetObject.transform;
    }

    private bool TryGetMainFarmTilemapBounds(out Bounds bounds)
    {
        bool hasBounds = false;
        bounds = default;

        FarmingManager farmingManager = FindFirstObjectByType<FarmingManager>();
        if (farmingManager != null)
        {
            hasBounds |= EncapsulateTilemapBounds(farmingManager.GroundTilemap, ref bounds, hasBounds);
            hasBounds |= EncapsulateTilemapBounds(farmingManager.CropTilemap, ref bounds, hasBounds);
        }

        if (!hasBounds)
        {
            hasBounds |= EncapsulateTilemapBounds(FindTilemapByName("GroundTilemap"), ref bounds, hasBounds);
            hasBounds |= EncapsulateTilemapBounds(FindTilemapByName("CropTilemap"), ref bounds, hasBounds);
        }

        if (!hasBounds)
        {
            Tilemap[] visibleTilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
            foreach (Tilemap tilemap in visibleTilemaps)
            {
                string lowerName = tilemap.name.ToLowerInvariant();
                if ((lowerName.Contains("ground") || lowerName.Contains("crop") || lowerName.Contains("soil") || lowerName.Contains("path")) && IsVisibleTilemap(tilemap))
                    hasBounds |= EncapsulateTilemapBounds(tilemap, ref bounds, hasBounds);
            }
        }

        return hasBounds;
    }

    private bool IsVisibleTilemap(Tilemap tilemap)
    {
        if (tilemap == null || !tilemap.gameObject.activeInHierarchy || !tilemap.enabled)
            return false;

        TilemapRenderer renderer = tilemap.GetComponent<TilemapRenderer>();
        return renderer != null && renderer.enabled;
    }

    private Tilemap FindTilemapByName(string tilemapName)
    {
        GameObject tilemapObject = GameObject.Find(tilemapName);
        if (tilemapObject != null && tilemapObject.TryGetComponent(out Tilemap tilemap))
            return tilemap;

        return null;
    }

    private bool EncapsulateTilemapBounds(Tilemap tilemap, ref Bounds combinedBounds, bool hasBounds)
    {
        if (tilemap == null || tilemap.localBounds.size == Vector3.zero)
            return false;

        Bounds localBounds = tilemap.localBounds;
        Vector3 min = tilemap.transform.TransformPoint(localBounds.min);
        Vector3 max = tilemap.transform.TransformPoint(localBounds.max);
        Bounds worldBounds = new Bounds((min + max) * 0.5f, new Vector3(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y), Mathf.Abs(max.z - min.z)));

        if (hasBounds)
            combinedBounds.Encapsulate(worldBounds);
        else
            combinedBounds = worldBounds;

        return true;
    }

    private float CalculateOrthographicSizeForBounds(Bounds bounds)
    {
        float aspect = 16f / 9f;
        if (UnityEngine.Camera.main != null && UnityEngine.Camera.main.aspect > 0f)
            aspect = UnityEngine.Camera.main.aspect;

        float verticalSize = bounds.extents.y + farmMapViewPadding;
        float horizontalSize = bounds.extents.x / aspect + farmMapViewPadding;
        return Mathf.Max(verticalSize, horizontalSize, fallbackFarmMapOrthographicSize);
    }

    private void SetPlayerMovementLocked(bool locked)
    {
        if (playerController == null && currentTarget != null)
            playerController = currentTarget.GetComponent<CharacterController2D>();

        if (playerController != null)
            playerController.SetMovementLocked(locked);
    }

    private float CalculatePlayerMarkerScale()
    {
        return playerMarkerScale;
    }

    private void ExpandTilemapCullingBoundsForMap()
    {
        RestoreTilemapCullingBounds();

        TilemapRenderer[] renderers = FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None);
        foreach (TilemapRenderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            tilemapCullingStates.Add(new TilemapCullingState(renderer, renderer.chunkCullingBounds));

            Vector3 currentBounds = renderer.chunkCullingBounds;
            renderer.chunkCullingBounds = new Vector3(
                Mathf.Max(currentBounds.x, mapTilemapCullingBounds.x),
                Mathf.Max(currentBounds.y, mapTilemapCullingBounds.y),
                Mathf.Max(currentBounds.z, mapTilemapCullingBounds.z));
        }
    }

    private void RestoreTilemapCullingBounds()
    {
        for (int i = 0; i < tilemapCullingStates.Count; i++)
        {
            if (tilemapCullingStates[i].renderer != null)
                tilemapCullingStates[i].renderer.chunkCullingBounds = tilemapCullingStates[i].chunkCullingBounds;
        }

        tilemapCullingStates.Clear();
    }

    private void SetHotbarSuppressed(bool suppressed)
    {
        HotBarHUDController hotbarHud = FindFirstObjectByType<HotBarHUDController>();
        if (hotbarHud != null)
            hotbarHud.SetMapSuppressed(suppressed);

        HotBarController hotbarController = FindFirstObjectByType<HotBarController>();
        if (hotbarController != null)
            hotbarController.SetMapSuppressed(suppressed);
    }

    private static void ClearEditorSelectionIfNeeded(GameObject objectBeingDestroyed)
    {
#if UNITY_EDITOR
        if (objectBeingDestroyed == null)
            return;

        Object selectedObject = Selection.activeObject;
        if (selectedObject == null)
            return;

        GameObject selectedGameObject = selectedObject as GameObject;
        if (selectedGameObject == null && selectedObject is Component selectedComponent)
            selectedGameObject = selectedComponent.gameObject;

        if (selectedGameObject != null && selectedGameObject.transform.IsChildOf(objectBeingDestroyed.transform))
            Selection.activeObject = null;
#endif
    }

    private Sprite GetPlayerMarkerSprite()
    {
        if (playerMarkerSprite != null)
            return playerMarkerSprite;

        Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color fill = Color.white;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                bool inHead = (x - 16) * (x - 16) + (y - 22) * (y - 22) <= 42;
                bool inPointer = y <= 22 && y >= 3 && Mathf.Abs(x - 16) <= (23 - y) / 2;
                bool cutout = (x - 16) * (x - 16) + (y - 22) * (y - 22) <= 12;
                texture.SetPixel(x, y, (inHead || inPointer) && !cutout ? fill : clear);
            }
        }

        texture.Apply();

        playerMarkerSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.15f), 32f);
        playerMarkerSprite.name = "RuntimeFarmMapPlayerMarker";
        return playerMarkerSprite;
    }

    private Texture2D GetPlayerMarkerTexture()
    {
        if (playerMarkerTexture != null)
            return playerMarkerTexture;

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color fill = new Color(1f, 0f, 0f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - 32f;
                float dy = y - 43f;
                bool inHead = dx * dx + dy * dy <= 17f * 17f;
                bool inPointer = y <= 43 && y >= 6 && Mathf.Abs(dx) <= (43 - y) * 0.42f;
                bool cutout = dx * dx + dy * dy <= 6f * 6f;
                texture.SetPixel(x, y, (inHead || inPointer) && !cutout ? fill : clear);
            }
        }

        texture.Apply();
        playerMarkerTexture = texture;
        return playerMarkerTexture;
    }

    private struct TilemapCullingState
    {
        public readonly TilemapRenderer renderer;
        public readonly Vector3 chunkCullingBounds;

        public TilemapCullingState(TilemapRenderer renderer, Vector3 chunkCullingBounds)
        {
            this.renderer = renderer;
            this.chunkCullingBounds = chunkCullingBounds;
        }
    }
}

[DisallowMultipleComponent]
public class RuntimeFallbackCameraFollow : MonoBehaviour
{
    private Transform target;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        SnapNow();
    }

    private void LateUpdate()
    {
        SnapNow();
    }

    private void SnapNow()
    {
        if (target == null)
            return;

        Vector3 p = target.position;
        Vector3 c = transform.position;
        transform.position = new Vector3(p.x, p.y, c.z);
    }
}
