using UnityEngine;

public class CarRight : MonoBehaviour
{
    private float moveSpeed = 5f;

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    private void Update()
    {
        // Move horizontally to the right
        transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);

        // Destroy if off-screen (optional optimization)
        if (transform.position.x > 15f)
        {
            Destroy(gameObject);
        }
    }
}
