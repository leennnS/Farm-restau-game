using UnityEngine;

public class MarketMusicController : MonoBehaviour
{
    [SerializeField] private AudioClip marketMusic;
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
        audioSource.clip = marketMusic;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f; // 2D audio

        // Play music
        if (marketMusic != null)
        {
            audioSource.Play();
        }
    }
}
