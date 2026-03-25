
using UnityEngine;

public class ItemFlyToPlayer : MonoBehaviour
{
    [SerializeField] private float riseHeight = 0.8f;
    [SerializeField] private float riseSpeed = 4f;
    [SerializeField] private float flySpeed = 8f;
    [SerializeField] private float destroyDistance = 0.15f;

    private Transform target;
    private Vector3 riseTarget;
    private bool rising = true;
    private bool initialized;
    private float targetSearchTimer;

    [SerializeField] private float targetSearchTimeout = 1.5f;

    private void Start()
    {
        // Disable physics so milk doesn't push player
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.gravityScale = 0;
        }

        // Set collider as trigger so it doesn't collide
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
    }

    public void Initialize(Transform playerTarget)
    {
        target = playerTarget;
        riseTarget = transform.position + Vector3.up * riseHeight;
        initialized = true;
        string targetName = target != null ? target.gameObject.name : "<null>";
        Debug.Log($"[ItemFlyToPlayer] Initialized! Target: {targetName}, RiseTarget: {riseTarget}");
    }

    private void Update()
    {
        if (target == null)
        {
            if (!TryResolveTarget())
                return;
        }

        if (!initialized)
        {
            riseTarget = transform.position + Vector3.up * riseHeight;
            initialized = true;
        }

        if (rising)
        {
            transform.position = Vector3.MoveTowards(transform.position, riseTarget, riseSpeed * Time.deltaTime);
            Debug.Log($"[ItemFlyToPlayer] Rising... Current: {transform.position}, Target: {riseTarget}");

            if (Vector3.Distance(transform.position, riseTarget) < 0.02f)
            {
                rising = false;
                Debug.Log("[ItemFlyToPlayer] Finished rising, starting fly to player");
            }
        }
        else
        {
            transform.position = Vector3.MoveTowards(transform.position, target.position, flySpeed * Time.deltaTime);
            Debug.Log($"[ItemFlyToPlayer] Flying to player... Current: {transform.position}, Target: {target.position}");

            if (Vector3.Distance(transform.position, target.position) < destroyDistance)
            {
                Debug.Log("[ItemFlyToPlayer] Reached player, destroying");
                Destroy(gameObject);
            }
        }
    }

    private bool TryResolveTarget()
    {
        targetSearchTimer += Time.deltaTime;
        if (targetSearchTimer > targetSearchTimeout)
        {
            Debug.Log("[ItemFlyToPlayer] Target not found in time, destroying");
            Destroy(gameObject);
            return false;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
            return true;
        }

        return false;
    }
}