using UnityEngine;

public class Car : MonoBehaviour
{
    private float moveSpeed = 5f;
    private CarNearMissEffects nearMissEffects;
    private BoxCollider2D trafficCollider;
    [SerializeField] private float offscreenViewportMargin = 0.35f;
    [SerializeField] private float fallbackDestroyX = -40f;
    [SerializeField] private Vector2 fallbackColliderSize = new Vector2(2.2f, 1.1f);

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
        EnsureNearMissEffects();
    }

    private void Awake()
    {
        EnsureTrafficCollider();
        EnsureNearMissEffects();
    }

    private void Update()
    {
        EnsureNearMissEffects();

        if (nearMissEffects != null && nearMissEffects.EvaluateNow())
            return;

        // Move horizontally to the left
        transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);

        // Destroy only after leaving the camera view with a margin.
        if (IsSafelyOffscreen())
        {
            Destroy(gameObject);
        }
    }

    private void EnsureNearMissEffects()
    {
        if (nearMissEffects == null)
            nearMissEffects = GetComponent<CarNearMissEffects>() ?? gameObject.AddComponent<CarNearMissEffects>();

        nearMissEffects.Configure(Vector2.left);
    }

    private void EnsureTrafficCollider()
    {
        Collider2D existingCollider = GetComponent<Collider2D>();
        if (existingCollider != null)
        {
            existingCollider.isTrigger = false;
        }
        else
        {
            trafficCollider = gameObject.AddComponent<BoxCollider2D>();
            trafficCollider.isTrigger = false;
            trafficCollider.size = ResolveColliderSize();
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private Vector2 ResolveColliderSize()
    {
        SpriteRenderer renderer = GetComponentInChildren<SpriteRenderer>();
        if (renderer == null || renderer.sprite == null)
            return fallbackColliderSize;

        Bounds localBounds = renderer.sprite.bounds;
        Vector3 scale = renderer.transform.lossyScale;
        Vector3 rootScale = transform.lossyScale;

        float rootX = Mathf.Approximately(rootScale.x, 0f) ? 1f : Mathf.Abs(rootScale.x);
        float rootY = Mathf.Approximately(rootScale.y, 0f) ? 1f : Mathf.Abs(rootScale.y);
        float width = Mathf.Abs(localBounds.size.x * scale.x / rootX);
        float height = Mathf.Abs(localBounds.size.y * scale.y / rootY);

        return new Vector2(Mathf.Max(0.3f, width), Mathf.Max(0.3f, height));
    }

    private bool IsSafelyOffscreen()
    {
        Camera camera = Camera.main;
        if (camera == null)
            return transform.position.x < fallbackDestroyX;

        Vector3 viewportPosition = camera.WorldToViewportPoint(transform.position);
        return viewportPosition.z > 0f && viewportPosition.x < -offscreenViewportMargin;
    }
}
