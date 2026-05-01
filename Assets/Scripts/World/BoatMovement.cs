using UnityEngine;

/// <summary>
/// Handles boat movement. The boat moves forward continuously when a player is on it.
/// Requires a Rigidbody2D component.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BoatMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float boatSpeed = 10f;
    [SerializeField] private Vector2 moveDirection = Vector2.up; // Direction the boat moves (e.g., up by default)

    [Header("Optional - Directional Indicator")]
    [SerializeField] private bool rotateTowardMovement = false; // If true, boat rotates to face movement direction
    [SerializeField] private float rotationSpeed = 5f;

    [Header("Sprite Changing")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite rightSprite;
    [SerializeField] private Sprite leftSprite;
    [SerializeField] private Sprite upSprite;
    [SerializeField] private Sprite downSprite;

    [Header("Audio")]
    [SerializeField] private AudioClip movingLoopSound;
    [SerializeField, Range(0f, 1f)] private float movingLoopVolume = 0.7f;

    private Rigidbody2D boatRigidbody;
    private AudioSource movingAudioSource;
    private bool isMoving = false;
    private Vector2 currentDirection;

    private void Start()
    {
        boatRigidbody = GetComponent<Rigidbody2D>();
        SetupMovingAudioSource();
        
        // Get SpriteRenderer if not assigned
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
            
        currentDirection = moveDirection.normalized;

        if (boatRigidbody == null)
        {
            Debug.LogError("BoatMovement requires a Rigidbody2D component!");
        }
    }

    private void OnDisable()
    {
        StopMovingLoopSound();
    }

    private void FixedUpdate()
    {
        if (isMoving && boatRigidbody != null)
        {
            // Move boat in the current direction
            boatRigidbody.linearVelocity = currentDirection * boatSpeed;

            // Update sprite based on direction
            UpdateBoatSprite();

            // Optional: Rotate boat to face movement direction
            if (rotateTowardMovement)
            {
                float targetAngle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;
                Quaternion targetRotation = Quaternion.AngleAxis(targetAngle, Vector3.forward);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }
        }
    }

    public void StartMoving()
    {
        isMoving = true;
        PlayMovingLoopSound();
    }

    public void StopMoving()
    {
        isMoving = false;
        StopMovingLoopSound();
        if (boatRigidbody != null)
        {
            boatRigidbody.linearVelocity = Vector2.zero;
        }
    }

    public void SetDirection(Vector2 direction)
    {
        currentDirection = direction.normalized;
    }

    public void SetSpeed(float newSpeed)
    {
        boatSpeed = newSpeed;
    }

    private void UpdateBoatSprite()
    {
        if (spriteRenderer == null)
            return;

        // Determine which sprite to use based on direction
        if (Mathf.Abs(currentDirection.x) > Mathf.Abs(currentDirection.y))
        {
            // Moving more horizontally
            if (currentDirection.x > 0)
            {
                // Moving right
                if (rightSprite != null)
                    spriteRenderer.sprite = rightSprite;
            }
            else if (currentDirection.x < 0)
            {
                // Moving left
                if (leftSprite != null)
                    spriteRenderer.sprite = leftSprite;
            }
        }
        else
        {
            // Moving more vertically
            if (currentDirection.y > 0)
            {
                // Moving up
                if (upSprite != null)
                    spriteRenderer.sprite = upSprite;
            }
            else if (currentDirection.y < 0)
            {
                // Moving down
                if (downSprite != null)
                    spriteRenderer.sprite = downSprite;
            }
        }
    }

    private void SetupMovingAudioSource()
    {
        movingAudioSource = gameObject.AddComponent<AudioSource>();
        movingAudioSource.playOnAwake = false;
        movingAudioSource.loop = true;
        movingAudioSource.spatialBlend = 0f;
        movingAudioSource.volume = movingLoopVolume;
        movingAudioSource.clip = movingLoopSound;

        if (AudioSettingsManager.HasInstance)
            AudioSettingsManager.Instance.RefreshAudioSource(movingAudioSource);
    }

    private void PlayMovingLoopSound()
    {
        if (movingAudioSource == null || movingLoopSound == null)
            return;

        if (movingAudioSource.clip != movingLoopSound)
            movingAudioSource.clip = movingLoopSound;

        if (!movingAudioSource.isPlaying)
            movingAudioSource.Play();
    }

    private void StopMovingLoopSound()
    {
        if (movingAudioSource != null && movingAudioSource.isPlaying)
            movingAudioSource.Stop();
    }

    public bool IsMoving => isMoving;
}
