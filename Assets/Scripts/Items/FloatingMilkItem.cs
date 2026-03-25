using UnityEngine;

public class FloatingMilkItem : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float pickupDistance = 0.5f;

    [Header("Float Animation")]
    [SerializeField] private float floatHeight = 0.5f;
    [SerializeField] private float floatSpeed = 2f;

    private Transform playerTransform;
    private Vector3 startPosition;
    private float floatTimer = 0f;
    private bool movingToPlayer = false;

    private void Start()
    {
        // Auto-find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        startPosition = transform.position;
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }

        if (playerTransform == null)
            return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        // If close enough, pick up
        if (distanceToPlayer < pickupDistance)
        {
            Destroy(gameObject);
            return;
        }

        // Move towards player
        Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
        transform.position += directionToPlayer * moveSpeed * Time.deltaTime;

        // Add gentle floating animation
        floatTimer += Time.deltaTime;
        float floatOffset = Mathf.Sin(floatTimer * floatSpeed) * floatHeight * 0.1f;
        transform.position += Vector3.up * floatOffset * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Also pick up on collision with player
        if (collision.CompareTag("Player"))
        {
            Destroy(gameObject);
        }
    }
}
