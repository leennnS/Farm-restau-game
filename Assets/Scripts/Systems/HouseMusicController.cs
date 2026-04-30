using UnityEngine;

public class HouseMusicController : MonoBehaviour
{
    [SerializeField] private AudioClip houseMusic;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;
        audioSource.clip = houseMusic;

        if (audioSource.clip != null)
            audioSource.Play();
    }
}