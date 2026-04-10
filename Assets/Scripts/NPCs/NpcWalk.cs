using UnityEngine;

public class NPCWalker : MonoBehaviour
{
    public float speed = 2f;
    public Transform turnPoint;
    public Transform queuePoint;
    public float stoppingDistance = 0.05f;

    private bool reachedTurnPoint = false;
    private bool reachedQueue = false;

    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private RestaurantNpcQueueManager queueManager;
    private Transform assignedQueueSpot;
    private Transform exitPoint;
    private bool managedByQueue;
    private bool leaveViaTurnPoint;
    private readonly System.Collections.Generic.List<Vector3> leaveWaypoints = new System.Collections.Generic.List<Vector3>(3);
    private int leaveWaypointIndex;

    private enum MovementState
    {
        ToTurnPoint,
        ToQueueSpot,
        Waiting,
        Leaving,
        Done
    }

    private MovementState state = MovementState.ToTurnPoint;

    public bool IsWaitingInQueue => state == MovementState.Waiting;
    public bool IsManagedByQueue => managedByQueue;
    public Transform AssignedQueueSpot => assignedQueueSpot;

    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (!managedByQueue)
            state = MovementState.ToTurnPoint;
    }

    void Update()
    {
        if (managedByQueue)
        {
            UpdateManagedQueueMovement();
            return;
        }

        UpdateLegacyMovement();
    }

    public void ConfigureForQueue(RestaurantNpcQueueManager manager, Transform initialQueueSpot, Transform leavePoint, Transform entryTurnPoint)
    {
        queueManager = manager;
        assignedQueueSpot = initialQueueSpot;
        exitPoint = leavePoint;
        turnPoint = entryTurnPoint;
        managedByQueue = true;

        reachedTurnPoint = false;
        reachedQueue = false;

        if (turnPoint == null)
            state = MovementState.ToQueueSpot;
        else
            state = MovementState.ToTurnPoint;

        ResumeAnimator();
    }

    public void SetQueueSpot(Transform newSpot)
    {
        if (newSpot == null)
            return;

        assignedQueueSpot = newSpot;

        if (state == MovementState.Waiting || state == MovementState.ToQueueSpot)
        {
            state = MovementState.ToQueueSpot;
            ResumeAnimator();
        }
    }

    public void BeginLeavingQueue(bool useTurnPointLane = false)
    {
        if (!managedByQueue)
            return;

        leaveViaTurnPoint = useTurnPointLane;
        BuildLeaveWaypoints();
        state = MovementState.Leaving;
        ResumeAnimator();
    }

    private void BuildLeaveWaypoints()
    {
        leaveWaypoints.Clear();
        leaveWaypointIndex = 0;

        if (exitPoint == null)
            return;

        Vector3 current = transform.position;
        float z = current.z;

        if (leaveViaTurnPoint && turnPoint != null)
        {
            // Front queue lane: go right to turn-lane x, then up, then to final exit.
            leaveWaypoints.Add(new Vector3(turnPoint.position.x, current.y, z));
            leaveWaypoints.Add(new Vector3(turnPoint.position.x, exitPoint.position.y, z));
        }
        else
        {
            // Other queue lanes: go straight up first, then to final exit.
            leaveWaypoints.Add(new Vector3(current.x, exitPoint.position.y, z));
        }

        leaveWaypoints.Add(new Vector3(exitPoint.position.x, exitPoint.position.y, z));
    }

    private void UpdateManagedQueueMovement()
    {
        switch (state)
        {
            case MovementState.ToTurnPoint:
                if (turnPoint == null)
                {
                    state = MovementState.ToQueueSpot;
                    break;
                }

                if (MoveTowards(turnPoint.position, stoppingDistance))
                {
                    reachedTurnPoint = true;

                    if (animator != null)
                        animator.SetBool("ReachedTurnPoint", true);

                    FaceLeft();

                    state = MovementState.ToQueueSpot;
                }
                break;

            case MovementState.ToQueueSpot:
                if (assignedQueueSpot == null)
                    return;

                FaceTowards(assignedQueueSpot.position);

                if (MoveTowards(assignedQueueSpot.position, stoppingDistance))
                {
                    reachedQueue = true;
                    state = MovementState.Waiting;
                    PauseAnimator();
                }
                break;

            case MovementState.Waiting:
                break;

            case MovementState.Leaving:
                if (exitPoint == null)
                {
                    state = MovementState.Done;
                    queueManager?.NotifyNpcExited(this);
                    Destroy(gameObject);
                    return;
                }

                if (leaveWaypoints.Count == 0)
                    BuildLeaveWaypoints();

                if (leaveWaypointIndex >= leaveWaypoints.Count)
                {
                    state = MovementState.Done;
                    queueManager?.NotifyNpcExited(this);
                    Destroy(gameObject);
                    return;
                }

                Vector3 nextPoint = leaveWaypoints[leaveWaypointIndex];
                FaceTowards(nextPoint);

                if (MoveTowards(nextPoint, stoppingDistance))
                {
                    leaveWaypointIndex++;

                    if (leaveWaypointIndex >= leaveWaypoints.Count)
                    {
                        state = MovementState.Done;
                        queueManager?.NotifyNpcExited(this);
                        Destroy(gameObject);
                    }
                }
                break;
        }
    }

    private void UpdateLegacyMovement()
    {
        if (turnPoint == null || queuePoint == null)
        {

            return;
        }

        // STEP 1: Move DOWN to turn point
        if (!reachedTurnPoint)
        {
            if (MoveTowards(turnPoint.position, stoppingDistance))
            {
                reachedTurnPoint = true;

                if (animator != null)
                    animator.SetBool("ReachedTurnPoint", true);
            }
        }

        // STEP 2: Move LEFT to queue
        else if (!reachedQueue)
        {
            if (MoveTowards(queuePoint.position, stoppingDistance))
            {
                reachedQueue = true;



                // STOP animation
                PauseAnimator();
            }
        }

        // STEP 3: Stay still
        else
        {
            // Do nothing → NPC stands in queue
        }
    }

    private bool MoveTowards(Vector3 targetPosition, float stopDistance)
    {
        transform.position = Vector2.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.deltaTime
        );

        float distance = Vector2.Distance(transform.position, targetPosition);
        if (distance <= stopDistance)
        {
            transform.position = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
            return true;
        }

        return false;
    }

    private void PauseAnimator()
    {
        if (animator != null)
            animator.speed = 0f;
    }

    private void ResumeAnimator()
    {
        if (animator != null && animator.speed <= 0f)
            animator.speed = 1f;
    }

    private void FaceLeft()
    {
        if (spriteRenderer != null)
            spriteRenderer.flipX = false;
    }

    private void FaceTowards(Vector3 targetPosition)
    {
        if (spriteRenderer == null)
            return;

        float dx = targetPosition.x - transform.position.x;
        if (Mathf.Abs(dx) < 0.01f)
            return;

        // This project's sprites face left when flipX is false.
        spriteRenderer.flipX = dx > 0f;
    }
}