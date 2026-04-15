using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

/// <summary>
/// Improved lantern system with directional lighting.
/// Light follows player and shines in direction they're facing.
/// Can be picked up, lit/unlit, and carried across scenes.
/// </summary>
public class ImprovedLanternController : MonoBehaviour
{
    [SerializeField]
    private Light2D lanternLight;

    [SerializeField]
    private SpriteRenderer lanternSprite;

    [SerializeField]
    private Transform lightAnchor;

    [SerializeField]
    private float pickupDistance = 2f;

    [SerializeField]
    private float lightFollowDistance = 0.5f;

    [SerializeField]
    private Vector3 heldOffset = new Vector3(0f, 0.2f, -0.5f);

    [SerializeField]
    private KeyCode pickupKey = KeyCode.E;

    [SerializeField]
    private KeyCode toggleKey = KeyCode.F;

    [SerializeField]
    private KeyCode dropKey = KeyCode.G;

    [SerializeField]
    private bool allowToggleWhileHeld = false;

    // Directional light settings
    [SerializeField]
    private float directionalLightArcAngle = 120f;

    [SerializeField]
    private float lightIntensity = 1.8f;

    [SerializeField]
    private float lightRange = 6f;

    private bool isLit = false;
    private bool isHeldByPlayer = false;
    private Transform playerTransform;
    private CharacterController2D playerController;
    private Collider2D lanternCollider;

    private static ImprovedLanternController _instance;

    public bool IsLit => isLit;
    public bool IsHeldByPlayer => isHeldByPlayer;

    private void Awake()
    {
        // Singleton - persist across scenes
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

        lanternCollider = GetComponent<Collider2D>();

        // Create light anchor if doesn't exist
        if (lightAnchor == null)
        {
            GameObject anchorObj = new GameObject("LightAnchor");
            anchorObj.transform.SetParent(transform);
            anchorObj.transform.localPosition = Vector3.forward * 0.1f;
            lightAnchor = anchorObj.transform;

            if (lanternLight != null)
            {
                lanternLight.transform.SetParent(lightAnchor);
                lanternLight.transform.localPosition = Vector3.zero;
            }
        }

        // Start dark and unlit
        SetLanternLit(false);

        Debug.Log("[Lantern] Initialized at position: " + transform.position);
    }

    private void Update()
    {
        if (isHeldByPlayer)
        {
            // Reacquire player after scene loads while preserving held state.
            if (playerTransform == null || playerController == null)
            {
                CharacterController2D newPlayer = FindFirstObjectByType<CharacterController2D>();
                if (newPlayer != null)
                {
                    playerTransform = newPlayer.transform;
                    playerController = newPlayer;
                }
            }

            if (playerTransform != null)
            {
                // Snap to player so it never lags behind.
                transform.position = playerTransform.position + heldOffset;
            }

            // Rotate light to face player's facing direction
            if (playerController != null)
            {
                Vector2 facingDirection = playerController.lastmotionVector;
                if (facingDirection != Vector2.zero)
                {
                    float angle = Mathf.Atan2(facingDirection.y, facingDirection.x) * Mathf.Rad2Deg;
                    if (lightAnchor != null)
                    {
                        lightAnchor.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
                    }
                }
            }

            // Optional toggle while held (off by default so E is not hijacked in gameplay)
            if (allowToggleWhileHeld && Input.GetKeyDown(toggleKey))
            {
                SetLanternLit(!isLit);
            }

            // Drop lantern and walk away.
            if (Input.GetKeyDown(dropKey))
            {
                DropLantern();
            }
        }
        else
        {
            // Check if player is nearby
            CharacterController2D player = FindFirstObjectByType<CharacterController2D>();
            if (player != null)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);

                if (distance < pickupDistance && Input.GetKeyDown(pickupKey))
                {
                    PickUpLantern(player.transform, player);
                }
            }
        }
    }

    /// <summary>
    /// Activation sequence - lantern flickers to life as story event
    /// </summary>
    public IEnumerator PlayActivationSequence()
    {
        Debug.Log("[Lantern] Playing activation sequence...");

        // Flicker effect
        for (int i = 0; i < 3; i++)
        {
            SetLanternLit(true);
            yield return new WaitForSeconds(0.2f);
            SetLanternLit(false);
            yield return new WaitForSeconds(0.2f);
        }

        // Final light on
        SetLanternLit(true);
        yield return new WaitForSeconds(0.5f);
    }

    public void PickUpLantern(Transform player, CharacterController2D controller)
    {
        isHeldByPlayer = true;
        playerTransform = player;
        playerController = controller;

        if (lanternCollider != null)
        {
            lanternCollider.enabled = false;
        }

        Debug.Log("[Lantern] Picked up by player!");

        // Update sprite to show it's held
        if (lanternSprite != null)
        {
            lanternSprite.color = Color.white;
        }
    }

    public void DropLantern()
    {
        isHeldByPlayer = false;
        playerTransform = null;
        playerController = null;

        if (lanternCollider != null)
        {
            lanternCollider.enabled = true;
        }

        Debug.Log("[Lantern] Dropped at " + transform.position);
    }

    public void SetLanternLit(bool lit)
    {
        isLit = lit;

        if (lanternLight != null)
        {
            lanternLight.enabled = lit;

            // Configure directional properties
            if (lit)
            {
                lanternLight.pointLightInnerAngle = directionalLightArcAngle;
                lanternLight.pointLightOuterAngle = directionalLightArcAngle;
                lanternLight.intensity = lightIntensity;
                lanternLight.pointLightOuterRadius = lightRange;
            }
        }

        // Visual feedback on sprite
        if (lanternSprite != null)
        {
            lanternSprite.color = lit ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);
        }

        Debug.Log($"[Lantern] Light {(lit ? "ON" : "OFF")}");
    }

    /// <summary>
    /// Call from farm scene to place lantern
    /// </summary>
    public void PlaceInScene(Vector3 position)
    {
        DropLantern();
        transform.position = position;
        SetLanternLit(false);
        Debug.Log($"[Lantern] Placed at {position}");
    }
}
