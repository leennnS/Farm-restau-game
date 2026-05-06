using UnityEngine;
using UnityEngine.UIElements;

public class CowMilkingMinigameUI : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Audio")]
    public AudioClip milkingLoopClip;
    [SerializeField] private AudioSource audioSource;

    [Header("Portraits")]
    [SerializeField] private Sprite[] playerAnimationFrames;
    [SerializeField] private float playerAnimationSpeed = 0.12f;

    [Header("Mini-game Tuning")]
    [SerializeField] private float markerSpeed = 0.8f;
    [SerializeField] private float startingZoneWidth = 0.28f;
    [SerializeField] private float minimumZoneWidth = 0.12f;
    [SerializeField] private float perfectMultiplier = 0.35f;
    [SerializeField] private float goodMultiplier = 0.5f;
    [SerializeField] private float goodReward = 0.12f;
    [SerializeField] private float perfectReward = 0.18f;
    [SerializeField] private float missPenalty = 0.08f;
    [SerializeField] private float speedIncreasePerHit = 0.05f;

    [Header("Optional Lock While Open")]
    [SerializeField] private MonoBehaviour[] scriptsToDisableWhileOpen;

    private VisualElement root;
    private VisualElement uiRoot;
    private VisualElement panel;
    private VisualElement cowImage;
    private VisualElement playerImage;
    private VisualElement totalFill;
    private VisualElement timingTrack;
    private VisualElement successZone;
    private VisualElement marker;
    private Label titleLabel;
    private Label instructionLabel;
    private Label nextKeyLabel;
    private Label statusLabel;
    private Button cancelButton;

    private CowInteraction currentCow;
    private bool isOpen;

    private float progress;
    private float currentZoneWidth;
    private float zoneCenter;
    private float markerPosition;
    private int direction = 1;
    private float currentMarkerSpeed;
    private KeyCode expectedKey = KeyCode.A;

    private float animationTimer;
    private int currentAnimationFrame;
    private bool isPlayingHitAnimation;

    private void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.loop = true;
        audioSource.playOnAwake = false;
    }

    private void OnEnable()
    {
        SetupUI();
        Hide();
    }

    private void SetupUI()
    {
        if (uiDocument == null)
        {

            return;
        }

        root = uiDocument.rootVisualElement;
        uiRoot = root.Q<VisualElement>("Root") ?? root;

        panel = root.Q<VisualElement>("MilkingPanel");
        cowImage = root.Q<VisualElement>("CowImage");
        playerImage = root.Q<VisualElement>("PlayerImage");
        totalFill = root.Q<VisualElement>("TotalFill");
        timingTrack = root.Q<VisualElement>("TimingTrack");
        successZone = root.Q<VisualElement>("SuccessZone");
        marker = root.Q<VisualElement>("Marker");
        titleLabel = root.Q<Label>("TitleLabel");
        instructionLabel = root.Q<Label>("InstructionLabel");
        nextKeyLabel = root.Q<Label>("NextKeyLabel");
        statusLabel = root.Q<Label>("StatusLabel");
        cancelButton = root.Q<Button>("CancelButton");

        if (cancelButton != null)
        {
            cancelButton.clicked -= CancelMilking;
            cancelButton.clicked += CancelMilking;
            cancelButton.UnregisterCallback<ClickEvent>(HandleCancelClick);
            cancelButton.RegisterCallback<ClickEvent>(HandleCancelClick);
            cancelButton.UnregisterCallback<PointerUpEvent>(HandleCancelPointerUp);
            cancelButton.RegisterCallback<PointerUpEvent>(HandleCancelPointerUp);
            cancelButton.SetEnabled(true);
            cancelButton.pickingMode = PickingMode.Position;
        }
    }

    private void Update()
    {
        if (!isOpen)
            return;

        UpdatePlayerAnimation();
        UpdateMarker();

        bool pressedExpectedKey = Input.GetKeyDown(expectedKey);
        bool pressedWrongKey = Input.GetKeyDown(expectedKey == KeyCode.A ? KeyCode.D : KeyCode.A);

        if (pressedExpectedKey)
            HandleHit(true);
        else if (pressedWrongKey)
            HandleHit(false);

        if (Input.GetMouseButtonUp(0) && IsPointerOverCancelButton())
            CancelMilking();

        if (Input.GetKeyDown(KeyCode.Escape))
            CancelMilking();
    }

    private void OnGUI()
    {
        if (!isOpen)
            return;

        Rect cancelRect = GetCancelButtonScreenRect();
        if (cancelRect.width <= 0f || cancelRect.height <= 0f)
            cancelRect = new Rect(Screen.width * 0.5f - 180f, Screen.height - 120f, 360f, 70f);

        GUI.depth = -10000;

        if (GUI.Button(cancelRect, GUIContent.none, GUIStyle.none))
            CancelMilking();
    }

    public void OpenMilkingGame(CowInteraction cow)
    {
        if (cow == null || isOpen)
            return;

        currentCow = cow;
        currentCow.BeginMilking();

        progress = 0f;
        currentZoneWidth = startingZoneWidth;
        zoneCenter = Random.Range(0.3f, 0.7f);
        markerPosition = 0f;
        direction = 1;
        currentMarkerSpeed = markerSpeed;
        expectedKey = KeyCode.A;

        animationTimer = 0f;
        currentAnimationFrame = 0;
        isPlayingHitAnimation = false;

        if (cowImage != null && cow.CowPortraitSprite != null)
            cowImage.style.backgroundImage = new StyleBackground(cow.CowPortraitSprite);

        SetPlayerFrame(0);

        if (titleLabel != null)
            titleLabel.text = "Milking Time";

        if (instructionLabel != null)
            instructionLabel.text = "Hit the moving marker while it is inside the green zone.";

        if (statusLabel != null)
            statusLabel.text = "Start gently...";

        UpdateNextKeyLabel();
        RefreshBars();

        if (uiDocument != null)
            uiDocument.sortingOrder = 6000;

        if (uiRoot != null)
            uiRoot.style.display = DisplayStyle.Flex;

        StartMilkingAudio();
        TogglePlayerScripts(false);
        isOpen = true;
    }

    private void UpdatePlayerAnimation()
    {
        if (!isPlayingHitAnimation)
            return;

        if (playerImage == null || playerAnimationFrames == null || playerAnimationFrames.Length == 0)
            return;

        animationTimer += Time.unscaledDeltaTime;

        if (animationTimer >= playerAnimationSpeed)
        {
            animationTimer = 0f;
            currentAnimationFrame++;

            if (currentAnimationFrame >= playerAnimationFrames.Length)
            {
                isPlayingHitAnimation = false;
                currentAnimationFrame = 0;
                SetPlayerFrame(0);
                return;
            }

            SetPlayerFrame(currentAnimationFrame);
        }
    }

    private void PlayHitAnimation()
    {
        if (playerAnimationFrames == null || playerAnimationFrames.Length == 0)
            return;

        animationTimer = 0f;
        currentAnimationFrame = 0;
        isPlayingHitAnimation = true;
        SetPlayerFrame(0);
    }

    private void SetPlayerFrame(int frameIndex)
    {
        if (playerImage == null || playerAnimationFrames == null || playerAnimationFrames.Length == 0)
            return;

        if (frameIndex < 0 || frameIndex >= playerAnimationFrames.Length)
            return;

        if (playerAnimationFrames[frameIndex] != null)
            playerImage.style.backgroundImage = new StyleBackground(playerAnimationFrames[frameIndex]);
    }

    private void UpdateMarker()
    {
        markerPosition += direction * currentMarkerSpeed * Time.unscaledDeltaTime;

        if (markerPosition >= 1f)
        {
            markerPosition = 1f;
            direction = -1;
        }
        else if (markerPosition <= 0f)
        {
            markerPosition = 0f;
            direction = 1;
        }

        RefreshBars();
    }

    private void HandleHit(bool correctKey)
    {
        float distance = Mathf.Abs(markerPosition - zoneCenter);
        bool inZone = distance <= currentZoneWidth * 0.5f;

        if (correctKey && inZone)
        {
            bool perfect = distance <= currentZoneWidth * perfectMultiplier;
            float reward = perfect ? perfectReward : goodReward;

            progress += reward;
            progress = Mathf.Clamp01(progress);

            PlayHitAnimation();

            if (statusLabel != null)
                statusLabel.text = perfect ? "Perfect squeeze!" : "Good squeeze!";

            expectedKey = expectedKey == KeyCode.A ? KeyCode.D : KeyCode.A;

            currentMarkerSpeed += speedIncreasePerHit;
            currentZoneWidth = Mathf.Max(minimumZoneWidth, currentZoneWidth - 0.01f);
            zoneCenter = Random.Range(currentZoneWidth * 0.5f, 1f - currentZoneWidth * 0.5f);

            UpdateNextKeyLabel();
            RefreshBars();

            if (progress >= 1f)
                FinishMilking(true);
        }
        else
        {
            progress -= missPenalty;
            progress = Mathf.Clamp01(progress);

            if (statusLabel != null)
                statusLabel.text = correctKey ? "Too early or too late!" : "Wrong hand rhythm!";

            RefreshBars();
        }
    }

    private void UpdateNextKeyLabel()
    {
        if (nextKeyLabel != null)
            nextKeyLabel.text = $"Next key: {expectedKey}";
    }

    private void RefreshBars()
    {
        if (totalFill != null)
            totalFill.style.width = Length.Percent(progress * 100f);

        if (successZone != null)
        {
            float left = (zoneCenter - currentZoneWidth * 0.5f) * 100f;
            float width = currentZoneWidth * 100f;

            successZone.style.left = Length.Percent(left);
            successZone.style.width = Length.Percent(width);
        }

        if (marker != null)
            marker.style.left = Length.Percent(markerPosition * 100f);
    }

    private void CancelMilking()
    {
        FinishMilking(false);
    }

    private void HandleCancelClick(ClickEvent evt)
    {
        evt.StopImmediatePropagation();
        CancelMilking();
    }

    private void HandleCancelPointerUp(PointerUpEvent evt)
    {
        if (evt.button != 0)
            return;

        evt.StopImmediatePropagation();
        CancelMilking();
    }

    private bool IsPointerOverCancelButton()
    {
        Vector2 panelPosition = Input.mousePosition;
        panelPosition.y = Screen.height - panelPosition.y;

        Rect cancelRect = GetCancelButtonScreenRect();
        if (cancelRect.width > 0f && cancelRect.height > 0f && cancelRect.Contains(panelPosition))
            return true;

        Rect fallbackRect = new Rect(Screen.width * 0.5f - 180f, Screen.height - 120f, 360f, 70f);
        return fallbackRect.Contains(panelPosition);
    }

    private Rect GetCancelButtonScreenRect()
    {
        if (cancelButton == null)
            return Rect.zero;

        Rect world = cancelButton.worldBound;
        if (world.width <= 0f || world.height <= 0f)
            return Rect.zero;

        return new Rect(world.x, world.y, world.width, world.height);
    }

    private void FinishMilking(bool success)
    {
        if (currentCow != null)
        {
            if (success)
                currentCow.CompleteMilking(true);
            else
                currentCow.CancelMilking();
        }

        currentCow = null;
        Hide();
    }

    private void Hide()
    {
        isOpen = false;
        isPlayingHitAnimation = false;
        currentAnimationFrame = 0;

        StopMilkingAudio();

        if (uiRoot != null)
            uiRoot.style.display = DisplayStyle.None;

        if (uiDocument != null)
            uiDocument.sortingOrder = 0;

        TogglePlayerScripts(true);
    }

    private void TogglePlayerScripts(bool enabledValue)
    {
        if (scriptsToDisableWhileOpen == null)
            return;

        foreach (MonoBehaviour script in scriptsToDisableWhileOpen)
        {
            if (script != null)
                script.enabled = enabledValue;
        }
    }

    private void StartMilkingAudio()
    {
        if (audioSource == null || milkingLoopClip == null)
            return;

        if (audioSource.clip != milkingLoopClip)
            audioSource.clip = milkingLoopClip;

        if (!audioSource.isPlaying)
            audioSource.Play();
    }

    private void StopMilkingAudio()
    {
        if (audioSource == null)
            return;

        if (audioSource.isPlaying)
            audioSource.Stop();
    }
}
