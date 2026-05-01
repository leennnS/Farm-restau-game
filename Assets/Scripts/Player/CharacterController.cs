using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CharacterController2D : MonoBehaviour
{
    Rigidbody2D myRigidBody;
    Animator animator;
    AudioSource walkAudioSource;

    public float speed = 15f;

    [Header("Walk SFX")]
    [SerializeField] private AudioClip walkSound;
    [SerializeField, Range(0f, 1f)] private float walkVolume = 0.65f;

    Vector2 motionVector;
    public Vector2 lastmotionVector;
    public bool moving;

    void Start()
    {
        myRigidBody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        SetupWalkAudioSource();
    }

    void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        motionVector = new Vector2(horizontal, vertical).normalized;

        animator.SetFloat("horizontal", motionVector.x);
        animator.SetFloat("vertical", motionVector.y);

        moving = motionVector != Vector2.zero;
        animator.SetBool("moving", moving);

        if (moving)
        {
            lastmotionVector = motionVector;
            animator.SetFloat("lastHorizontal", motionVector.x);
            animator.SetFloat("lastVertical", motionVector.y);
        }

        UpdateWalkAudio();
    }

    void FixedUpdate()
    {
        // Scale movement speed based on character scale for consistent perceived speed
        float scaledSpeed = speed * transform.localScale.x;
        myRigidBody.MovePosition(myRigidBody.position + motionVector * scaledSpeed * Time.fixedDeltaTime);
    }

    void OnDisable()
    {
        if (walkAudioSource != null && walkAudioSource.isPlaying)
            walkAudioSource.Stop();
    }

    private void SetupWalkAudioSource()
    {
        walkAudioSource = gameObject.AddComponent<AudioSource>();
        walkAudioSource.playOnAwake = false;
        walkAudioSource.loop = true;
        walkAudioSource.spatialBlend = 0f;
        walkAudioSource.volume = walkVolume;
        walkAudioSource.clip = walkSound;

        if (AudioSettingsManager.HasInstance)
            AudioSettingsManager.Instance.RefreshAudioSource(walkAudioSource);
    }

    private void UpdateWalkAudio()
    {
        if (walkAudioSource == null)
            return;

        if (walkAudioSource.clip != walkSound)
            walkAudioSource.clip = walkSound;

        bool shouldPlay = moving && walkSound != null && Time.timeScale > 0.001f;

        if (shouldPlay)
        {
            if (!walkAudioSource.isPlaying)
                walkAudioSource.Play();
        }
        else if (walkAudioSource.isPlaying)
        {
            walkAudioSource.Stop();
        }
    }
}
