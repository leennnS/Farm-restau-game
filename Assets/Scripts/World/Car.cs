using UnityEngine;

public class Car : MonoBehaviour
{
    private float moveSpeed = 5f;

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    private void Update()
    {
        // Move horizontally to the left
        transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);

        // Destroy if off-screen (optional optimization)
        if (transform.position.x < -15f)
        {
            Destroy(gameObject);
        }
    }
}
