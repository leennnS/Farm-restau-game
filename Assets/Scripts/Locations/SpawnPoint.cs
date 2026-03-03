using UnityEngine;

/// <summary>
/// A simple spawn point marker. Attach to an empty GameObject where
/// the player should appear in the scene. You can also set a desired
/// player scale for that scene.
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    [Tooltip("Desired player uniform scale when spawning at this point (1 = original size)")]
    public float playerScale = 1f;
}
