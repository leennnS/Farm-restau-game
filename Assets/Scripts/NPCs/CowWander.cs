using UnityEngine;

public class CowWander : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float minMoveSpeed = 0.2f;
    [SerializeField] private float maxMoveSpeed = 0.45f;
    [SerializeField] private float stoppingDistance = 0.05f;
    [SerializeField] private float minTargetDistance = 1.2f;

    [Header("Walk / Idle Time")]
    [SerializeField] private float minWalkTime = 2f;
    [SerializeField] private float maxWalkTime = 4f;
    [SerializeField] private float minIdleTime = 4f;
    [SerializeField] private float maxIdleTime = 8f;

    [Header("Wander Bounds")]
    [SerializeField] private Transform areaCenter;
    [SerializeField] private Vector2 areaSize = new Vector2(8f, 6f);

    [Header("References")]
    [SerializeField] private Animator animator;

    private Vector3 targetPosition;
    private float currentMoveSpeed;
    private float stateTimer;
    private bool isWalking;

    private Vector2 moveDirection = Vector2.down;
    private Vector2 lastMoveDirection = Vector2.down;

    private void Start()
    {
        currentMoveSpeed = Random.Range(minMoveSpeed, maxMoveSpeed);

        if (Random.value > 0.7f)
            PickWalkState();
        else
            PickIdleState();

        stateTimer += Random.Range(0f, 3f);
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

        targetPosition = GetRandomPointInBoundsFarEnough();

        Vector2 dir = (targetPosition - transform.position).normalized;
        moveDirection = GetCardinalDirection(dir);
        lastMoveDirection = moveDirection;
    }

    private void PickIdleState()
    {
        isWalking = false;
        stateTimer = Random.Range(minIdleTime, maxIdleTime);
        moveDirection = Vector2.zero;
    }

    private void MoveToTarget()
    {
        Vector3 toTarget = targetPosition - transform.position;
        Vector2 dir = toTarget.normalized;

        if (dir.sqrMagnitude > 0.001f)
        {
            moveDirection = GetCardinalDirection(dir);
            lastMoveDirection = moveDirection;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            currentMoveSpeed * Time.deltaTime
        );
    }

    private Vector3 GetRandomPointInBoundsFarEnough()
    {
        Vector3 center = areaCenter != null ? areaCenter.position : transform.position;

        for (int i = 0; i < 20; i++)
        {
            float randomX = Random.Range(-areaSize.x / 2f, areaSize.x / 2f);
            float randomY = Random.Range(-areaSize.y / 2f, areaSize.y / 2f);

            Vector3 point = new Vector3(center.x + randomX, center.y + randomY, transform.position.z);

            if (Vector3.Distance(transform.position, point) >= minTargetDistance)
                return point;
        }

        return transform.position;
    }

    private Vector2 GetCardinalDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return direction.x > 0 ? Vector2.right : Vector2.left;
        else
            return direction.y > 0 ? Vector2.up : Vector2.down;
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        animator.SetBool("isWalking", isWalking);

        if (isWalking)
        {
            animator.SetFloat("moveX", moveDirection.x);
            animator.SetFloat("moveY", moveDirection.y);
        }
        else
        {
            animator.SetFloat("moveX", lastMoveDirection.x);
            animator.SetFloat("moveY", lastMoveDirection.y);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = areaCenter != null ? areaCenter.position : transform.position;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, new Vector3(areaSize.x, areaSize.y, 0f));
    }
}