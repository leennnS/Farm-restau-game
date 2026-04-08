using UnityEngine;

/// <summary>
/// Handles boat movement. The boat moves forward continuously when a player is on it.
/// Requires a Rigidbody2D component.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BoatMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float boatSpeed = 10f;
    [SerializeField] private Vector2 moveDirection = Vector2.up; // Direction the boat moves (e.g., up by default)

    [Header("Optional - Directional Indicator")]
    [SerializeField] private bool rotateTowardMovement = false; // If true, boat rotates to face movement direction
    [SerializeField] private float rotationSpeed = 5f;

    private Rigidbody2D boatRigidbody;
    private bool isMoving = false;
    private Vector2 currentDirection;

    private void Start()
    {
        boatRigidbody = GetComponent<Rigidbody2D>();
        currentDirection = moveDirection.normalized;

        if (boatRigidbody == null)
        {
            Debug.LogError("BoatMovement requires a Rigidbody2D component!");
        }
    }

    private void FixedUpdate()
    {
        if (isMoving && boatRigidbody != null)
        {
            // Move boat in the current direction
            boatRigidbody.linearVelocity = currentDirection * boatSpeed;

            // Optional: Rotate boat to face movement direction
            if (rotateTowardMovement)
            {
                float targetAngle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;
                Quaternion targetRotation = Quaternion.AngleAxis(targetAngle, Vector3.forward);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }
        }
    }

    public void StartMoving()
    {
        isMoving = true;
    }

    public void StopMoving()
    {
        isMoving = false;
        if (boatRigidbody != null)
        {
            boatRigidbody.linearVelocity = Vector2.zero;
        }
    }

    public void SetDirection(Vector2 direction)
    {
        currentDirection = direction.normalized;
    }

    public void SetSpeed(float newSpeed)
    {
        boatSpeed = newSpeed;
    }

    public bool IsMoving => isMoving;
}
