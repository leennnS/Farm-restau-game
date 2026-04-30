using UnityEngine;

public class FarmMusicController : MonoBehaviour
{
    [SerializeField] private AudioClip farmMusic;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;
        audioSource.clip = farmMusic;

        if (audioSource.clip != null)
            audioSource.Play();
    }
}