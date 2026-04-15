using UnityEngine;
using UnityEngine.Rendering.Universal;

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

    private bool isLit = false;
    private bool isHeldByPlayer = false;
    private Transform playerTransform;

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

        // Start unlit
        SetLanternLit(false);
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
                if (distance < pickupDistance && Input.GetKeyDown(pickupKey))
                {
                    PickUpLantern(player.transform);
                }
            }
        }
    }

    public void PickUpLantern(Transform player)
    {
        isHeldByPlayer = true;
        playerTransform = player;
        Debug.Log("[Lantern] Picked up by player!");
    }

    public void DropLantern()
    {
        isHeldByPlayer = false;
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
