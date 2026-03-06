using UnityEngine;

public class AnimalWander : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float minMoveSpeed = 0.8f;
    [SerializeField] private float maxMoveSpeed = 1.5f;
    [SerializeField] private float stoppingDistance = 0.05f;

    [Header("Walk / Idle Time")]
    [SerializeField] private float minWalkTime = 1f;
    [SerializeField] private float maxWalkTime = 3f;
    [SerializeField] private float minIdleTime = 1f;
    [SerializeField] private float maxIdleTime = 2.5f;

    [Header("Wander Bounds")]
    [SerializeField] private Transform areaCenter;
    [SerializeField] private Vector2 areaSize = new Vector2(3f, 3f);

    [Header("References")]
    [SerializeField] private Animator animator;

    private Vector3 targetPosition;
    private float currentMoveSpeed;
    private float stateTimer;
    private bool isWalking;
    private float moveX = 1f; // keep last facing direction, start facing right

    private void Start()
    {
        currentMoveSpeed = Random.Range(minMoveSpeed, maxMoveSpeed);

        if (Random.value > 0.5f)
            PickWalkState();
        else
            PickIdleState();

        stateTimer += Random.Range(0f, 1.5f);
        UpdateAnimation();
    }

    private void Update()
    {
        stateTimer -= Time.deltaTime;

        if (isWalking)
        {
            MoveToTarget();

            bool reachedTarget = Vector3.Distance(transform.position, targetPosition) <= stoppingDistance;

            if (reachedTarget || stateTimer <= 0f)
            {
                PickIdleState();
            }
        }
        else
        {
            if (stateTimer <= 0f)
            {
                PickWalkState();
            }
        }

        UpdateAnimation();
    }

    private void PickWalkState()
    {
        isWalking = true;
        stateTimer = Random.Range(minWalkTime, maxWalkTime);
        currentMoveSpeed = Random.Range(minMoveSpeed, maxMoveSpeed);
        targetPosition = GetRandomPointInBounds();

        Vector3 direction = (targetPosition - transform.position).normalized;

        if (Mathf.Abs(direction.x) > 0.01f)
            moveX = direction.x;
    }

    private void PickIdleState()
    {
        isWalking = false;
        stateTimer = Random.Range(minIdleTime, maxIdleTime);
    }

    private void MoveToTarget()
    {
        Vector3 direction = (targetPosition - transform.position).normalized;

        if (Mathf.Abs(direction.x) > 0.01f)
            moveX = direction.x;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            currentMoveSpeed * Time.deltaTime
        );
    }

    private Vector3 GetRandomPointInBounds()
    {
        Vector3 center = areaCenter != null ? areaCenter.position : transform.position;

        float randomX = Random.Range(-areaSize.x / 2f, areaSize.x / 2f);
        float randomY = Random.Range(-areaSize.y / 2f, areaSize.y / 2f);

        return new Vector3(center.x + randomX, center.y + randomY, transform.position.z);
    }

    private void UpdateAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("isWalking", isWalking);
            animator.SetFloat("moveX", moveX);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = areaCenter != null ? areaCenter.position : transform.position;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, new Vector3(areaSize.x, areaSize.y, 0f));
    }
}