
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
        Debug.Log($"[ItemFlyToPlayer] Initialized! Target: {target.gameObject.name}, RiseTarget: {riseTarget}");
    }

    private void Update()
    {
        if (target == null)
        {
            Debug.Log("[ItemFlyToPlayer] Target is null, destroying");
            Destroy(gameObject);
            return;
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
}