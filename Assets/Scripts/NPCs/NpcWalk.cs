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
    private Vector2 lastFacingDirection = Vector2.left;
    private bool hasMoveXParam;
    private bool hasMoveYParam;
    private bool hasHorizontalParam;
    private bool hasVerticalParam;
    private bool hasLastHorizontalParam;
    private bool hasLastVerticalParam;
    private bool hasReachedTurnPointParam;
    private bool hasWalkUpState;
    private bool hasWalkDownState;
    private bool hasWalkLeftState;
    private bool hasWalkUpStateLower;
    private bool hasWalkDownStateLower;
    private bool hasWalkLeftStateLower;
    private int walkUpStateHash;
    private int walkDownStateHash;
    private int walkLeftStateHash;
    private int walkUpStateHashLower;
    private int walkDownStateHashLower;
    private int walkLeftStateHashLower;
    private int lastPlayedDirectionalStateHash;

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
        CacheDirectionAnimatorParams();

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

                // Keep entry lane consistent: move vertically first, then horizontal.
                Vector3 turnVerticalTarget = new Vector3(transform.position.x, turnPoint.position.y, transform.position.z);
                FaceTowards(turnVerticalTarget);

                if (MoveTowards(turnVerticalTarget, stoppingDistance))
                {
                    reachedTurnPoint = true;

                    if (animator != null && hasReachedTurnPointParam)
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

        // STEP 1: Move DOWN to turn point (keep X fixed so NPC does not turn early)
        if (!reachedTurnPoint)
        {
            Vector3 verticalTarget = new Vector3(transform.position.x, turnPoint.position.y, transform.position.z);
            FaceTowards(verticalTarget);

            if (MoveTowards(verticalTarget, stoppingDistance))
            {
                reachedTurnPoint = true;

                if (animator != null)
                    animator.SetBool("ReachedTurnPoint", true);
            }
        }

        // STEP 2: Move horizontally to queue after reaching turn point
        else if (!reachedQueue)
        {
            Vector3 horizontalTarget = new Vector3(queuePoint.position.x, transform.position.y, transform.position.z);
            FaceTowards(horizontalTarget);

            if (MoveTowards(horizontalTarget, stoppingDistance))
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
        ApplyFacingVisuals(Vector2.left);
    }

    private void FaceTowards(Vector3 targetPosition)
    {
        Vector2 delta = targetPosition - transform.position;
        if (delta.sqrMagnitude <= 0.0001f)
            return;

        Vector2 facing;
        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            facing = new Vector2(Mathf.Sign(delta.x), 0f);
        else
            facing = new Vector2(0f, Mathf.Sign(delta.y));

        ApplyFacingVisuals(facing);
    }

    private void ApplyFacingVisuals(Vector2 facing)
    {
        if (facing.sqrMagnitude <= 0.0001f)
            return;

        lastFacingDirection = facing;

        if (spriteRenderer != null && Mathf.Abs(facing.x) > 0.001f)
        {
            // This project's sprites face left when flipX is false.
            spriteRenderer.flipX = facing.x > 0f;
        }
        else if (spriteRenderer != null && Mathf.Abs(facing.y) > 0.001f)
        {
            // Active NPC controller has no up/down directional clips,
            // so keep a stable non-right-facing visual during vertical motion.
            spriteRenderer.flipX = false;
        }

        if (animator == null)
            return;

        bool playedDirectionalState = TryPlayDirectionalState(facing);

        if (!playedDirectionalState && hasReachedTurnPointParam)
        {
            // In this controller, ReachedTurnPoint drives WalkLeft vs WalkDown state.
            // Keep it synced to current axis so vertical movement does not stay left-facing.
            bool useHorizontalState = Mathf.Abs(facing.x) > Mathf.Abs(facing.y);
            animator.SetBool("ReachedTurnPoint", useHorizontalState);
        }

        if (hasMoveXParam) animator.SetFloat("moveX", facing.x);
        if (hasMoveYParam) animator.SetFloat("moveY", facing.y);

        if (hasHorizontalParam) animator.SetFloat("horizontal", facing.x);
        if (hasVerticalParam) animator.SetFloat("vertical", facing.y);

        if (hasLastHorizontalParam) animator.SetFloat("lastHorizontal", facing.x);
        if (hasLastVerticalParam) animator.SetFloat("lastVertical", facing.y);
    }

    private void CacheDirectionAnimatorParams()
    {
        if (animator == null)
            return;

        hasMoveXParam = HasAnimatorFloat("moveX");
        hasMoveYParam = HasAnimatorFloat("moveY");
        hasHorizontalParam = HasAnimatorFloat("horizontal");
        hasVerticalParam = HasAnimatorFloat("vertical");
        hasLastHorizontalParam = HasAnimatorFloat("lastHorizontal");
        hasLastVerticalParam = HasAnimatorFloat("lastVertical");
        hasReachedTurnPointParam = HasAnimatorBool("ReachedTurnPoint");

        walkUpStateHash = Animator.StringToHash("Base Layer.WalkUp");
        walkDownStateHash = Animator.StringToHash("Base Layer.WalkDown");
        walkLeftStateHash = Animator.StringToHash("Base Layer.WalkLeft");
        walkUpStateHashLower = Animator.StringToHash("Base Layer.walk_up");
        walkDownStateHashLower = Animator.StringToHash("Base Layer.walk_down");
        walkLeftStateHashLower = Animator.StringToHash("Base Layer.walk_left");

        hasWalkUpState = animator.HasState(0, walkUpStateHash);
        hasWalkDownState = animator.HasState(0, walkDownStateHash);
        hasWalkLeftState = animator.HasState(0, walkLeftStateHash);
        hasWalkUpStateLower = animator.HasState(0, walkUpStateHashLower);
        hasWalkDownStateLower = animator.HasState(0, walkDownStateHashLower);
        hasWalkLeftStateLower = animator.HasState(0, walkLeftStateHashLower);
    }

    private bool TryPlayDirectionalState(Vector2 facing)
    {
        if (animator == null)
            return false;

        int desiredStateHash = 0;

        if (Mathf.Abs(facing.y) > Mathf.Abs(facing.x))
        {
            if (facing.y > 0f && hasWalkUpState)
                desiredStateHash = walkUpStateHash;
            else if (facing.y > 0f && hasWalkUpStateLower)
                desiredStateHash = walkUpStateHashLower;
            else if (facing.y < 0f && hasWalkDownState)
                desiredStateHash = walkDownStateHash;
            else if (facing.y < 0f && hasWalkDownStateLower)
                desiredStateHash = walkDownStateHashLower;
        }
        else if (hasWalkLeftState)
        {
            // Left clip is also used for right via flipX.
            desiredStateHash = walkLeftStateHash;
        }
        else if (hasWalkLeftStateLower)
        {
            // Left clip is also used for right via flipX.
            desiredStateHash = walkLeftStateHashLower;
        }

        if (desiredStateHash == 0)
            return false;

        if (desiredStateHash == lastPlayedDirectionalStateHash)
            return true;

        animator.Play(desiredStateHash, 0, 0f);
        lastPlayedDirectionalStateHash = desiredStateHash;
        return true;
    }

    private bool HasAnimatorFloat(string paramName)
    {
        if (animator == null)
            return false;

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter p = parameters[i];
            if (p.type == AnimatorControllerParameterType.Float && p.name == paramName)
                return true;
        }

        return false;
    }

    private bool HasAnimatorBool(string paramName)
    {
        if (animator == null)
            return false;

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter p = parameters[i];
            if (p.type == AnimatorControllerParameterType.Bool && p.name == paramName)
                return true;
        }

        return false;
    }
}