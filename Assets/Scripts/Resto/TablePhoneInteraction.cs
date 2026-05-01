using UnityEngine;
using System.Collections;
using UnityEngine.UIElements;

/// <summary>
/// Interaction for a restaurant table phone. Attach to the table GameObject (BoxCollider2D set to isTrigger=true).
/// - Shows a toast when player enters trigger: "Press P to call a customer"
/// - Opens a small panel with phone image and "Call Customer" button on P
/// - Calls RestaurantNpcQueueManager.ForceSpawnNow() when calling
/// - Plays an AudioClip assigned in Inspector when calling
/// - Respects existing queue state (does not allow calling if customers present)
/// </summary>
public class TablePhoneInteraction : MonoBehaviour
{
    private const string ActiveCustomerBlockedMessage = "You already have a customer. Serve them first.";

    [Header("Interaction")]
    [SerializeField] private KeyCode interactionKey = KeyCode.P;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private PickupToastUIToolkit pickupToast;

    [Header("UI")]
    [SerializeField] private UIDocument hostUiDocument;
    [SerializeField] private Sprite phoneSprite;

    [Header("Audio")]
    [SerializeField, Tooltip("Sound played when calling. Assign in Inspector.")] private AudioClip callAudio;

    private RestaurantNpcQueueManager queueManager;
    private AudioSource audioSource;
    private bool playerInRange;
    private bool spawnPending = false;
    private Coroutine spawnCoroutine = null;
    private const float MinSpawnDelay = 5f;

    // UI
    private VisualElement overlayRoot;
    private VisualElement panelRoot;
    private Button callButton;
    private Button closeButton;

    private void Start()
    {
        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (pickupToast == null)
            pickupToast = FindFirstObjectByType<PickupToastUIToolkit>();

        if (queueManager == null)
            queueManager = FindFirstObjectByType<RestaurantNpcQueueManager>();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerInRange = true;
        ShowPromptIfAvailable();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerInRange = false;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag) || !playerInRange)
            return;

        ShowPromptIfAvailable();
    }

    private void Update()
    {
        ResolveReferences();

        if (!playerInRange)
            return;

        if (Input.GetKeyDown(interactionKey))
        {
            if (!CanUsePhone(out string blockedMessage))
            {
                if (!string.IsNullOrEmpty(blockedMessage))
                    ShowToast(blockedMessage);

                return;
            }

            OpenPanel();
        }
    }

    private bool CanUsePhone(out string blockedMessage)
    {
        blockedMessage = string.Empty;

        if (queueManager == null)
            return false;

        if (queueManager.HasActiveCustomerInScene)
        {
            blockedMessage = ActiveCustomerBlockedMessage;
            return false;
        }

        return queueManager.IsWaitingForNextNpcSpawn;
    }

    private void ShowPromptIfAvailable()
    {
        if (!CanUsePhone(out string blockedMessage))
        {
            if (!string.IsNullOrEmpty(blockedMessage))
                ShowToast(blockedMessage);

            return;
        }

        ShowToast("Press P to call customer");
    }

    private void ShowToast(string msg)
    {
        if (pickupToast != null)
            pickupToast.Show(msg, 2.5f, 28);
    }

    private void OpenPanel()
    {
        BuildPanelIfNeeded();
        if (overlayRoot == null)
        {
            ShowToast("Could not open phone panel");
            return;
        }

        overlayRoot.style.display = DisplayStyle.Flex;
    }

    private void ClosePanel()
    {
        if (overlayRoot != null)
            overlayRoot.style.display = DisplayStyle.None;
    }

    private void BuildPanelIfNeeded()
    {
        if (overlayRoot != null)
            return;

        ResolveHostDocument();
        if (hostUiDocument == null)
            return;

        VisualElement hostRoot = hostUiDocument.rootVisualElement;
        if (hostRoot == null)
            return;

        overlayRoot = new VisualElement { name = "PhoneOverlay" };
        overlayRoot.style.position = Position.Absolute;
        overlayRoot.style.left = 0;
        overlayRoot.style.top = 0;
        overlayRoot.style.right = 0;
        overlayRoot.style.bottom = 0;
        overlayRoot.style.backgroundColor = new Color(0f, 0f, 0f, 0.45f);
        overlayRoot.style.justifyContent = Justify.Center;
        overlayRoot.style.alignItems = Align.Center;
        overlayRoot.style.display = DisplayStyle.None;
        overlayRoot.pickingMode = PickingMode.Position;

        panelRoot = new VisualElement { name = "PhonePanel" };
        panelRoot.style.width = 480;
        panelRoot.style.height = 320;
        panelRoot.style.paddingTop = 18;
        panelRoot.style.paddingBottom = 18;
        panelRoot.style.paddingLeft = 18;
        panelRoot.style.paddingRight = 18;
        panelRoot.style.backgroundColor = new Color(0.12f, 0.09f, 0.07f, 0.97f);
        panelRoot.style.borderTopLeftRadius = 12;
        panelRoot.style.borderTopRightRadius = 12;
        panelRoot.style.borderBottomLeftRadius = 12;
        panelRoot.style.borderBottomRightRadius = 12;

        // Phone image
        Image phoneImage = new Image { name = "PhoneImage" };
        phoneImage.style.width = 128;
        phoneImage.style.height = 128;
        phoneImage.style.marginBottom = 14;
        phoneImage.scaleMode = ScaleMode.ScaleToFit;
        if (phoneSprite != null)
            phoneImage.sprite = phoneSprite;

        Label title = new Label("Call Customer") { name = "PhoneTitle" };
        title.style.fontSize = 28;
        title.style.unityTextAlign = TextAnchor.MiddleCenter;
        title.style.color = new Color(0.98f, 0.95f, 0.83f, 1f);
        title.style.marginBottom = 8;

        callButton = CreateButton("CallCustomerButton", "Call Customer", 56, 300);
        callButton.clicked += OnCallCustomerClicked;

        closeButton = CreateButton("ClosePhoneButton", "Close", 46, 200);
        closeButton.clicked += ClosePanel;

        panelRoot.Add(title);
        panelRoot.Add(phoneImage);
        panelRoot.Add(callButton);
        panelRoot.Add(closeButton);

        overlayRoot.Add(panelRoot);
        hostRoot.Add(overlayRoot);
    }

    private Button CreateButton(string name, string text, int height, int width)
    {
        Button button = new Button { name = name, text = text };
        button.style.height = height;
        button.style.width = width;
        button.style.alignSelf = Align.Center;
        button.style.fontSize = 20;
        button.style.marginBottom = 8;
        button.style.backgroundColor = new Color(0.2f, 0.6f, 0.2f, 1f);
        button.style.color = Color.white;
        return button;
    }

    private void OnCallCustomerClicked()
    {
        ResolveReferences();

        if (!CanUsePhone(out string blockedMessage))
        {
            if (!string.IsNullOrEmpty(blockedMessage))
                ShowToast(blockedMessage);

            return;
        }

        if (callAudio != null && audioSource != null)
            audioSource.PlayOneShot(callAudio);

        if (queueManager != null)
        {
            // Start a delayed spawn so NPCs don't appear immediately.
            // Prefer syncing to the audio length but enforce a minimum delay.
            if (!spawnPending)
            {
                spawnCoroutine = StartCoroutine(DelayedSpawnRoutine());
            }
            ShowToast("Calling customer...");
        }
        else
        {
            ShowToast("No queue manager found");
        }
    }

    private IEnumerator DelayedSpawnRoutine()
    {
        spawnPending = true;

        float delay = MinSpawnDelay;
        if (callAudio != null)
        {
            try
            {
                float audioLen = callAudio.length;
                if (audioLen > 0f)
                    delay = Mathf.Max(MinSpawnDelay, audioLen);
            }
            catch
            {
                delay = MinSpawnDelay;
            }
        }

        yield return new WaitForSeconds(delay);

        // Invoke existing spawn logic (do not modify spawn implementation).
        if (queueManager != null)
            queueManager.ForceSpawnNow();

        spawnPending = false;
        spawnCoroutine = null;
    }

    private void ResolveHostDocument()
    {
        if (hostUiDocument != null)
            return;

        UIDocument[] docs = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
        for (int i = 0; i < docs.Length; i++)
        {
            UIDocument doc = docs[i];
            if (doc == null || !doc.isActiveAndEnabled || doc.rootVisualElement == null)
                continue;

            hostUiDocument = doc;
            break;
        }
    }
}
