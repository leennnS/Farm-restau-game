using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CharacterController2D : MonoBehaviour
{
    Rigidbody2D myRigidBody;
    Animator animator;
    AudioSource walkAudioSource;

    private const float MinimumRuntimeSpeed = 14f;

    public float speed = MinimumRuntimeSpeed;

    [Header("Walk SFX")]
    [SerializeField] private AudioClip walkSound;
    [SerializeField, Range(0f, 1f)] private float walkVolume = 0.65f;

    Vector2 motionVector;
    public Vector2 lastmotionVector;
    public bool moving;
    private bool movementLocked;

    void Awake()
    {
        // Ensure player persists across scene transitions
        DontDestroyOnLoad(gameObject);
        Debug.Log($"[CharacterController2D] Awake - Player '{gameObject.name}' in scene '{gameObject.scene.name}'. DontDestroyOnLoad set.");
    }

    void Start()
    {
        if (speed < MinimumRuntimeSpeed)
            speed = MinimumRuntimeSpeed;

        myRigidBody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        SetupWalkAudioSource();
    }

    void Update()
    {
        if (movementLocked)
        {
            StopMovement();
            return;
        }

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
        if (movementLocked)
            return;

        myRigidBody.MovePosition(myRigidBody.position + motionVector * speed * Time.fixedDeltaTime);
    }

    public void SetMovementLocked(bool locked)
    {
        movementLocked = locked;

        if (locked)
            StopMovement();
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

    private void StopMovement()
    {
        motionVector = Vector2.zero;
        moving = false;

        if (myRigidBody != null)
            myRigidBody.linearVelocity = Vector2.zero;

        if (animator != null)
        {
            animator.SetFloat("horizontal", 0f);
            animator.SetFloat("vertical", 0f);
            animator.SetBool("moving", false);
        }

        if (walkAudioSource != null && walkAudioSource.isPlaying)
            walkAudioSource.Stop();
    }
}
