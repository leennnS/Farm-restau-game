using UnityEngine;

/// <summary>
/// Represents a planting hole on the ground where a tree can be planted.
/// Tracks whether a seed has been planted in this hole.
/// </summary>
public class PlantingHole : MonoBehaviour
{
    private Vector3 _position;
    private bool _hasSeed = false;

    public void Initialize(Vector3 position)
    {
        _position = position;
        transform.position = position;
        _hasSeed = false;
    }

    public Vector3 GetPosition() => _position;

    public bool HasSeed() => _hasSeed;

    public void PlantSeed()
    {
        _hasSeed = true;
    }
}
