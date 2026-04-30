using UnityEngine;

/// <summary>
/// Manages background music for the Restaurant scene.
/// Attach to an empty GameObject in the Restaurant scene.
/// Assign the AudioClip via the Inspector.
/// </summary>
public class RestaurantMusicController : MonoBehaviour
{
    [SerializeField] private AudioClip restaurantMusic;
    private AudioSource audioSource;

    private void Start()
    {
        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configure AudioSource
        audioSource.clip = restaurantMusic;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f; // 0 = 2D, 1 = 3D

        // Play the music
        if (restaurantMusic != null)
        {
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("RestaurantMusicController: restaurantMusic clip is not assigned!", this);
        }
    }
}
