using UnityEngine;

/// <summary>
/// Limits how far fruit can fall below the tree collider bounds.
/// Attached dynamically to fruit pickups when they spawn.
/// </summary>
public class FruitFallLimiter : MonoBehaviour
{
    private Bounds _treeBounds;
    private Rigidbody2D _rb2d;
    private float _bottomLimit;

    public void SetTreeBounds(Bounds bounds, float fallBelowOffset = 0f)
    {
        _treeBounds = bounds;
        _bottomLimit = _treeBounds.min.y - fallBelowOffset; // Allow fruits to fall below by offset amount
        _rb2d = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (_rb2d == null)
            return;

        // Check if fruit has fallen below the tree's bottom
        if (transform.position.y < _bottomLimit)
        {
            // Stop it at the bottom of the tree bounds
            Vector3 lockedPos = new Vector3(transform.position.x, _bottomLimit, transform.position.z);
            _rb2d.linearVelocity = Vector2.zero;
            _rb2d.gravityScale = 0f;
            transform.position = lockedPos;
        }
    }
}
