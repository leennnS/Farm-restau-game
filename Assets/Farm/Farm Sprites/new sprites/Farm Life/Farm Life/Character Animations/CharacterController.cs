using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CharacterController2D : MonoBehaviour
{
    Rigidbody2D myRigidBody;
    Animator animator;

    public float speed = 15f;

    Vector2 motionVector;
    public Vector2 lastmotionVector;
    public bool moving;

    void Start()
    {
        myRigidBody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        motionVector = new Vector2(horizontal, vertical).normalized;

        animator.SetFloat("horizontal", motionVector.x);
        animator.SetFloat("vertical", motionVector.y);

        moving = motionVector != Vector2.zero;
        animator.SetBool("moving", moving);

        if (moving)
        {
            lastmotionVector = motionVector;
            animator.SetFloat("lastHorizontal", motionVector.x);
            animator.SetFloat("lastVertical", motionVector.y);
        }
    }

    void FixedUpdate()
    {
        myRigidBody.MovePosition(myRigidBody.position + motionVector * speed * Time.fixedDeltaTime);
    }

}
