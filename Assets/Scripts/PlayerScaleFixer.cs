using UnityEngine;

/// <summary>
/// Temporarily enforces a desired localScale on the GameObject for a short duration
/// to guard against race conditions where other scripts change scale after scene load.
/// Destroys itself when done.
/// </summary>
public class PlayerScaleFixer : MonoBehaviour
{
    public Vector3 targetScale = new Vector3(0.5f, 0.5f, 1f);
    public float duration = 0.5f; // seconds

    float elapsed = 0f;

    void Start()
    {
        if (targetScale == Vector3.zero) targetScale = new Vector3(0.5f, 0.5f, 1f);
        transform.localScale = targetScale;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        transform.localScale = targetScale;
        if (elapsed >= duration)
            Destroy(this);
    }
}
