using UnityEngine;

public class NPCWalker : MonoBehaviour
{
    public float speed = 2f;
    public Transform turnPoint;
    public Transform queuePoint;

    private bool reachedTurnPoint = false;
    private bool reachedQueue = false;

    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (turnPoint == null || queuePoint == null)
        {

            return;
        }

        // STEP 1: Move DOWN to turn point
        if (!reachedTurnPoint)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                turnPoint.position,
                speed * Time.deltaTime
            );

            if (Vector2.Distance(transform.position, turnPoint.position) < 0.05f)
            {
                reachedTurnPoint = true;

                if (animator != null)
                    animator.SetBool("ReachedTurnPoint", true);
            }
        }

        // STEP 2: Move LEFT to queue
        else if (!reachedQueue)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                queuePoint.position,
                speed * Time.deltaTime
            );

            if (Vector2.Distance(transform.position, queuePoint.position) < 0.05f)
            {
                reachedQueue = true;



                // STOP animation
                if (animator != null)
                {
                    animator.speed = 0f;
                }
            }
        }

        // STEP 3: Stay still
        else
        {
            // Do nothing → NPC stands in queue
        }
    }
}