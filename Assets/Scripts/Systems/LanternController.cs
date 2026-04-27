using UnityEngine;
using UnityEngine.Rendering.Universal;
using TMPro;

/// <summary>
/// Lantern that the player can pick up and carry between scenes.
/// Can be turned on/off and persists with the player.
/// </summary>
public class LanternController : MonoBehaviour
{
    [SerializeField]
    private Light2D lanternLight;

    [SerializeField]
    private SpriteRenderer lanternSprite;

    [SerializeField]
    private KeyCode pickupKey = KeyCode.E;

    [SerializeField]
    private KeyCode toggleKey = KeyCode.E;

    [SerializeField]
    private float pickupDistance = 2f;

    [SerializeField]
    private PickupToastUIToolkit toastUI;

    [SerializeField]
    private string pickupPromptTemplate = "Press {0} to pick up lantern";

    [SerializeField]
    private float pickupPromptRepeatDelay = 1.2f;

    [SerializeField]
    private Vector3 pickupPromptOffset = new Vector3(0f, 1.1f, 0f);

    private bool isLit = false;
    private bool isHeldByPlayer = false;
    private Transform playerTransform;
    private bool wasPlayerInPickupRange = false;
    private float nextPickupPromptTime = 0f;
    private GameObject pickupHintObject;
    private TextMeshPro pickupHintText;

    private static LanternController _instance;

    public bool IsLit => isLit;
    public bool IsHeldByPlayer => isHeldByPlayer;

    private void Awake()
    {
        // Singleton pattern - persist across scenes
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
        if (lanternLight == null)
            lanternLight = GetComponentInChildren<Light2D>();

        if (lanternSprite == null)
            lanternSprite = GetComponentInChildren<SpriteRenderer>();

        if (toastUI == null)
            toastUI = FindFirstObjectByType<PickupToastUIToolkit>();

        // Start unlit
        SetLanternLit(false);

        CreatePickupHintIfNeeded();
    }

    private void Update()
    {
        if (isHeldByPlayer)
        {
            // Follow player
            if (playerTransform != null)
            {
                transform.position = playerTransform.position + Vector3.forward * 0.1f;
            }

            // Toggle lantern while holding
            if (Input.GetKeyDown(toggleKey))
            {
                SetLanternLit(!isLit);
            }
        }
        else
        {
            // Player nearby - show pickup prompt
            CharacterController2D player = FindFirstObjectByType<CharacterController2D>();
            if (player != null)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                bool isPlayerInPickupRange = distance < pickupDistance;

                if (isPlayerInPickupRange)
                {
                    ShowPickupHintText();

                    if (!wasPlayerInPickupRange || Time.time >= nextPickupPromptTime)
                    {
                        ShowPickupPrompt();
                    }

                    if (Input.GetKeyDown(pickupKey))
                    {
                        PickUpLantern(player.transform);
                    }
                }
                else
                {
                    HidePickupHintText();
                }

                wasPlayerInPickupRange = isPlayerInPickupRange;
            }
            else
            {
                wasPlayerInPickupRange = false;
                HidePickupHintText();
            }
        }
    }

    private void ShowPickupPrompt()
    {
        if (toastUI == null)
            return;

        toastUI.Show(string.Format(pickupPromptTemplate, pickupKey));
        nextPickupPromptTime = Time.time + pickupPromptRepeatDelay;
    }

    private void CreatePickupHintIfNeeded()
    {
        if (pickupHintObject != null)
            return;

        pickupHintObject = new GameObject("LanternPickupHint");
        pickupHintObject.transform.SetParent(transform);
        pickupHintObject.transform.localPosition = pickupPromptOffset;

        pickupHintText = pickupHintObject.AddComponent<TextMeshPro>();
        pickupHintText.text = string.Format(pickupPromptTemplate, pickupKey);
        pickupHintText.fontSize = 3f;
        pickupHintText.alignment = TextAlignmentOptions.Center;
        pickupHintText.color = Color.white;
        pickupHintText.outlineWidth = 0.2f;
        pickupHintText.outlineColor = Color.black;

        pickupHintObject.SetActive(false);
    }

    private void ShowPickupHintText()
    {
        if (toastUI != null)
            return;

        if (pickupHintObject == null)
            CreatePickupHintIfNeeded();

        if (pickupHintText != null)
            pickupHintText.text = string.Format(pickupPromptTemplate, pickupKey);

        if (pickupHintObject != null)
            pickupHintObject.SetActive(true);
    }

    private void HidePickupHintText()
    {
        if (pickupHintObject != null)
            pickupHintObject.SetActive(false);
    }

    public void PickUpLantern(Transform player)
    {
        isHeldByPlayer = true;
        wasPlayerInPickupRange = false;
        HidePickupHintText();
        playerTransform = player;
        Debug.Log("[Lantern] Picked up by player!");
    }

    public void DropLantern()
    {
        isHeldByPlayer = false;
        wasPlayerInPickupRange = false;
        HidePickupHintText();
        playerTransform = null;
        Debug.Log("[Lantern] Dropped");
    }

    public void SetLanternLit(bool lit)
    {
        isLit = lit;

        if (lanternLight != null)
            lanternLight.enabled = lit;

        if (lanternSprite != null)
        {
            // Brighten sprite when lit
            lanternSprite.color = lit ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
        }

        Debug.Log($"[Lantern] {(lit ? "Turned on" : "Turned off")}");
    }

    /// <summary>
    /// Call this from farm scene to place lantern on ground
    /// </summary>
    public void PlaceInFarmScene(Vector3 position)
    {
        DropLantern();
        transform.position = position;
        Debug.Log($"[Lantern] Placed in farm at {position}");
    }
}
