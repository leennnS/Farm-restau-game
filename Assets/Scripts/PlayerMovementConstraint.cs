using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerMovementConstraint : MonoBehaviour
{
    [Tooltip("Width multiplier applied to sprite.bounds.x when computing default collider size for constraint calculations.")]
    public float widthMultiplier = 0.45f;
    [Tooltip("Height multiplier applied to sprite.bounds.y when computing default collider size for constraint calculations.")]
    public float heightMultiplier = 0.275f;

    GameObject gridRoot;
    Bounds floorBounds;
    bool haveFloorBounds = false;

    Collider2D playerCollider;
    ContactFilter2D contactFilter;
    Collider2D[] overlapResults = new Collider2D[16];

    Rigidbody2D rb;

    Vector3 previousPosition;

    void Awake()
    {
        playerCollider = GetComponent<Collider2D>();
        if (playerCollider == null)
            Debug.LogWarning("PlayerMovementConstraint: Player has no Collider2D. Overlap detection will be limited.");

        // Build contact filter to detect non-trigger colliders
        contactFilter = new ContactFilter2D();
        contactFilter.useTriggers = false;
        contactFilter.SetLayerMask(Physics2D.AllLayers);

        previousPosition = transform.position;

        // Find Grid root and compute floor bounds
        var g = GameObject.Find("Grid");
        if (g != null)
        {
            gridRoot = g;
            ComputeFloorBounds();
        }
        else
        {
            Debug.LogWarning("PlayerMovementConstraint: 'Grid' GameObject not found in scene. Floor bounds will not be applied.");
        }

        rb = GetComponent<Rigidbody2D>();
    }

    void ComputeFloorBounds()
    {
        bool started = false;
        Vector3 min = Vector3.zero, max = Vector3.zero;

        var srs = gridRoot.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in srs)
        {
            if (sr.sprite == null)
                continue;

            // Use sprite.bounds (local) and transform it to world
            Bounds sb = sr.sprite.bounds;
            Vector3 worldCenter = sr.transform.TransformPoint(sb.center);
            Vector3 worldSize = Vector3.Scale(sb.size, sr.transform.lossyScale);
            Vector3 worldExtents = worldSize * 0.5f;

            Vector3 sMin = worldCenter - worldExtents;
            Vector3 sMax = worldCenter + worldExtents;

            if (!started)
            {
                min = sMin;
                max = sMax;
                started = true;
            }
            else
            {
                min = Vector3.Min(min, sMin);
                max = Vector3.Max(max, sMax);
            }
        }

        if (started)
        {
            floorBounds = new Bounds((min + max) * 0.5f, max - min);
            haveFloorBounds = true;
        }
        else
        {
            haveFloorBounds = false;
            Debug.LogWarning("PlayerMovementConstraint: no SpriteRenderers with sprites found under 'Grid' to compute floor bounds.");
        }
    }

    void FixedUpdate()
    {
        // Use Rigidbody2D when available for physics-safe movement adjustments
        if (rb != null)
        {
            Vector2 current = rb.position;
            Vector2 desired = current;

            if (haveFloorBounds)
            {
                Vector3 half = floorBounds.extents;
                Vector3 center = floorBounds.center;

                float left = center.x - half.x;
                float right = center.x + half.x;
                float bottom = center.y - half.y;
                float top = center.y + half.y;

                desired.x = Mathf.Clamp(current.x, left, right);
                desired.y = Mathf.Clamp(current.y, bottom, top);
            }

            Vector2 move = desired - current;

            // If movement would intersect colliders under Grid, prevent it using Rigidbody2D.Cast
            bool blocked = false;
            if (playerCollider != null && move.sqrMagnitude > 0.000001f)
            {
                // Cast along movement vector to see if collision would occur
                RaycastHit2D[] hits = new RaycastHit2D[8];
                int hitCount = rb.Cast(move.normalized, contactFilter, hits, move.magnitude);
                if (hitCount > 0)
                {
                    // Ensure hits are from Grid colliders
                    for (int i = 0; i < hitCount; i++)
                    {
                        var h = hits[i];
                        if (h.collider != null && gridRoot != null && h.collider.transform.IsChildOf(gridRoot.transform))
                        {
                            blocked = true;
                            break;
                        }
                    }
                }
            }

            if (blocked)
            {
                // stop movement and keep previous position
                rb.linearVelocity = Vector2.zero;
                rb.position = previousPosition;
            }
            else
            {
                // Move safely to clamped position
                if ((desired - current).sqrMagnitude > 0.000001f)
                    rb.MovePosition(desired);
            }

            // Additionally, if overlapping any Grid colliders after move, revert
            if (playerCollider != null && gridRoot != null)
            {
                int count = playerCollider.Overlap(contactFilter, overlapResults);
                for (int i = 0; i < count; i++)
                {
                    var c = overlapResults[i];
                    if (c == null) continue;
                    if (c.transform.IsChildOf(gridRoot.transform))
                    {
                        rb.position = previousPosition;
                        break;
                    }
                }
            }

            previousPosition = rb.position;
        }
        else
        {
            // Fallback to transform-based adjustments if no Rigidbody2D
            Vector3 current = transform.position;

            if (haveFloorBounds)
            {
                Vector3 clamped = current;
                Vector3 half = floorBounds.extents;
                Vector3 center = floorBounds.center;

                float left = center.x - half.x;
                float right = center.x + half.x;
                float bottom = center.y - half.y;
                float top = center.y + half.y;

                clamped.x = Mathf.Clamp(current.x, left, right);
                clamped.y = Mathf.Clamp(current.y, bottom, top);

                if (clamped != current)
                    transform.position = clamped;
            }

            bool overlappingGridCollider = false;
            if (playerCollider != null && gridRoot != null)
            {
                int count = playerCollider.Overlap(contactFilter, overlapResults);
                for (int i = 0; i < count; i++)
                {
                    var c = overlapResults[i];
                    if (c == null) continue;
                    if (c.transform.IsChildOf(gridRoot.transform))
                    {
                        overlappingGridCollider = true;
                        break;
                    }
                }
            }

            if (overlappingGridCollider)
                transform.position = previousPosition;

            previousPosition = transform.position;
        }
    }
}
