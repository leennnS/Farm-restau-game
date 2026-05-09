using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class CarNearMissEffects : MonoBehaviour
{
    private const int IconTextureSize = 32;

    private static Sprite s_warningSprite;
    private static AudioClip s_honkClip;
    private static float s_nextGlobalHonkTime;

    [Header("Near Miss")]
    [SerializeField] private float triggerDistance = 4.5f;
    [SerializeField] private float sameLaneVerticalDistance = 3.0f;
    [SerializeField] private float screenTriggerDistance = 170f;
    [SerializeField] private float stopDistance = 1.55f;
    [SerializeField] private float resumeDistance = 2.35f;
    [SerializeField] private float perCarCooldown = 2f;
    [SerializeField] private float globalHonkCooldown = 0.45f;

    [Header("Traffic Obstacles")]
    [SerializeField] private bool stopForDeliveryVans = true;
    [SerializeField] private float deliveryVanStopDistance = 5.5f;
    [SerializeField] private float deliveryVanResumeDistance = 6.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip honkClip;
    [SerializeField] private float honkVolume = 1.25f;
    [SerializeField] private float honkPitchMin = 0.96f;
    [SerializeField] private float honkPitchMax = 1.03f;

    [Header("Camera Shake")]
    [SerializeField] private float shakeDuration = 0.16f;
    [SerializeField] private float shakeStrength = 0.07f;

    [Header("Warning Icon")]
    [SerializeField] private float warningDuration = 0.65f;
    [SerializeField] private Vector3 warningOffset = new Vector3(0f, 1.35f, 0f);

    [Header("Headlights")]
    [SerializeField] private bool addHeadlights = true;
    [SerializeField] private float headlightOuterRadius = 3.2f;
    [SerializeField] private float headlightInnerRadius = 0.75f;
    [SerializeField] private float headlightIntensity = 1.15f;
    [SerializeField] private Vector2 headlightLocalOffset = new Vector2(0.72f, -0.05f);

    private Transform _player;
    private Collider2D _playerCollider;
    private SpriteRenderer _playerRenderer;
    private Collider2D _carCollider;
    private SpriteRenderer _carRenderer;
    private AudioSource _audioSource;
    private Vector2 _moveDirection = Vector2.left;
    private float _nextCarTriggerTime;
    private bool _hasTriggeredNearMiss;
    private Light2D _headlight;
    private bool _shouldStopForPlayer;
    private bool _shouldStopForDeliveryVan;
    private int _lastEvaluationFrame = -1;

    public bool ShouldStopForPlayer => _shouldStopForPlayer || _shouldStopForDeliveryVan;

    public bool EvaluateNow()
    {
        if (_lastEvaluationFrame == Time.frameCount)
            return _shouldStopForPlayer;

        _lastEvaluationFrame = Time.frameCount;
        ResolvePlayer();
        UpdateHeadlightPosition();
        CheckDeliveryVanObstacle();
        CheckNearMiss();
        return ShouldStopForPlayer;
    }

    public void Configure(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0.001f)
            _moveDirection = direction.normalized;

        EnsureHeadlight();
        UpdateHeadlightPosition();
    }

    public void SetHonkClip(AudioClip clip)
    {
        honkClip = clip;
    }

    private void Awake()
    {
        EnsureWarningSprite();
        EnsureAudio();
        _carCollider = GetComponent<Collider2D>();
        _carRenderer = GetComponentInChildren<SpriteRenderer>();
        ResolvePlayer();
        EnsureHeadlight();
    }

    private void Update()
    {
        EvaluateNow();
    }

    private void CheckNearMiss()
    {
        if (_player == null)
            return;

        if (Time.time < _nextCarTriggerTime)
            return;

        RefreshVisualReferences();

        Vector2 carPoint = GetClosestCarPointToPlayer();
        Vector2 playerPoint = GetClosestPlayerPointToCar();
        Vector2 toPlayer = playerPoint - carPoint;
        float verticalDistance = Mathf.Abs(_player.position.y - transform.position.y);
        float horizontalDistance = Mathf.Abs(_player.position.x - transform.position.x);
        float distance = toPlayer.magnitude;
        bool visualOverlap = AreVisualBoundsOverlapping();
        bool screenClose = IsScreenCloseToPlayer();

        bool closeEnough = visualOverlap
            || screenClose
            || verticalDistance <= sameLaneVerticalDistance
            && (distance <= triggerDistance || horizontalDistance <= triggerDistance);

        bool shouldStop = closeEnough
            || visualOverlap
            || verticalDistance <= sameLaneVerticalDistance && horizontalDistance <= stopDistance;

        bool shouldResume = !visualOverlap
            && !screenClose
            && (verticalDistance > sameLaneVerticalDistance || horizontalDistance > resumeDistance);

        if (shouldStop)
            _shouldStopForPlayer = true;
        else if (shouldResume)
            _shouldStopForPlayer = false;

        if (!closeEnough && !_shouldStopForPlayer)
        {
            if (distance > triggerDistance + 1.2f)
                _hasTriggeredNearMiss = false;

            return;
        }

        float inFrontAmount = Vector2.Dot(_moveDirection, toPlayer.normalized);
        if (inFrontAmount < -0.25f && _hasTriggeredNearMiss)
            return;

        _hasTriggeredNearMiss = true;
        _nextCarTriggerTime = Time.time + perCarCooldown;
        PlayNearMiss();
    }

    private void CheckDeliveryVanObstacle()
    {
        if (!stopForDeliveryVans)
            return;

        RefreshVisualReferences();

        DeliveryVanInteraction[] vans = FindObjectsByType<DeliveryVanInteraction>(FindObjectsSortMode.None);
        bool obstacleAhead = false;

        for (int i = 0; i < vans.Length; i++)
        {
            DeliveryVanInteraction van = vans[i];
            if (van == null || !van.BlocksTraffic || van.gameObject == gameObject)
                continue;

            Bounds vanBounds = van.GetTrafficBounds();
            Bounds carBounds = GetCarTrafficBounds();

            if (!AreBoundsInSameLane(carBounds, vanBounds))
                continue;

            float gapAhead = GetHorizontalGapAhead(carBounds, vanBounds);
            if (gapAhead >= -0.2f && gapAhead <= deliveryVanStopDistance)
            {
                obstacleAhead = true;
                break;
            }
        }

        if (obstacleAhead)
        {
            _shouldStopForDeliveryVan = true;
            return;
        }

        if (_shouldStopForDeliveryVan && IsClearOfDeliveryVans())
            _shouldStopForDeliveryVan = false;
    }

    private bool IsClearOfDeliveryVans()
    {
        DeliveryVanInteraction[] vans = FindObjectsByType<DeliveryVanInteraction>(FindObjectsSortMode.None);

        for (int i = 0; i < vans.Length; i++)
        {
            DeliveryVanInteraction van = vans[i];
            if (van == null || !van.BlocksTraffic || van.gameObject == gameObject)
                continue;

            Bounds vanBounds = van.GetTrafficBounds();
            Bounds carBounds = GetCarTrafficBounds();

            if (!AreBoundsInSameLane(carBounds, vanBounds))
                continue;

            float gapAhead = GetHorizontalGapAhead(carBounds, vanBounds);
            if (gapAhead >= -0.2f && gapAhead <= deliveryVanResumeDistance)
                return false;
        }

        return true;
    }

    private Bounds GetCarTrafficBounds()
    {
        if (_carCollider != null)
            return _carCollider.bounds;

        if (_carRenderer != null)
            return _carRenderer.bounds;

        return new Bounds(transform.position, Vector3.one);
    }

    private bool AreBoundsInSameLane(Bounds carBounds, Bounds obstacleBounds)
    {
        float carMinY = carBounds.min.y;
        float carMaxY = carBounds.max.y;
        float obstacleMinY = obstacleBounds.min.y;
        float obstacleMaxY = obstacleBounds.max.y;
        float overlap = Mathf.Min(carMaxY, obstacleMaxY) - Mathf.Max(carMinY, obstacleMinY);

        if (overlap > 0f)
            return true;

        float verticalCenterDistance = Mathf.Abs(carBounds.center.y - obstacleBounds.center.y);
        return verticalCenterDistance <= sameLaneVerticalDistance;
    }

    private float GetHorizontalGapAhead(Bounds carBounds, Bounds obstacleBounds)
    {
        if (_moveDirection.x >= 0f)
            return obstacleBounds.min.x - carBounds.max.x;

        return carBounds.min.x - obstacleBounds.max.x;
    }

    private void PlayNearMiss()
    {
        PlayHonk();
        PlayerWarningIcon.Show(_player, warningOffset, warningDuration);
        CameraNearMissShake.Shake(shakeDuration, shakeStrength);
    }

    private void PlayHonk()
    {
        if (_audioSource == null)
            return;

        if (Time.time < s_nextGlobalHonkTime)
            return;

        s_nextGlobalHonkTime = Time.time + globalHonkCooldown;
        _audioSource.pitch = Random.Range(honkPitchMin, honkPitchMax);
        AudioClip clipToPlay = honkClip != null ? honkClip : s_honkClip;
        if (clipToPlay != null)
            _audioSource.PlayOneShot(clipToPlay, honkVolume);
    }

    private void EnsureAudio()
    {
        if (s_honkClip == null)
            s_honkClip = CreateHonkClip();

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        _audioSource.volume = 1f;
        _audioSource.spatialBlend = 0f;
        _audioSource.minDistance = 2f;
        _audioSource.maxDistance = 12f;
    }

    private void EnsureHeadlight()
    {
        if (!addHeadlights || _headlight != null)
            return;

        GameObject lightGo = new GameObject("RuntimeHeadlight");
        lightGo.transform.SetParent(transform, false);

        _headlight = lightGo.AddComponent<Light2D>();
        _headlight.lightType = Light2D.LightType.Point;
        _headlight.color = new Color(1f, 0.88f, 0.58f, 1f);
        _headlight.intensity = headlightIntensity;
        _headlight.pointLightInnerRadius = headlightInnerRadius;
        _headlight.pointLightOuterRadius = headlightOuterRadius;
    }

    private void UpdateHeadlightPosition()
    {
        if (_headlight == null)
            return;

        float xSign = _moveDirection.x >= 0f ? 1f : -1f;
        _headlight.transform.localPosition = new Vector3(headlightLocalOffset.x * xSign, headlightLocalOffset.y, -0.1f);
    }

    private void ResolvePlayer()
    {
        if (_player != null)
            return;

        GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null)
        {
            _player = playerGo.transform;
            CachePlayerVisuals();
            return;
        }

        CharacterController2D controller = FindFirstObjectByType<CharacterController2D>();
        if (controller != null)
        {
            _player = controller.transform;
            CachePlayerVisuals();
            return;
        }

        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject go = allObjects[i];
            if (go == null || go.transform == transform)
                continue;

            if (go.name.IndexOf("player", System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            _player = go.transform;
            CachePlayerVisuals();
            return;
        }
    }

    private void CachePlayerVisuals()
    {
        if (_player == null)
            return;

        _playerCollider = _player.GetComponent<Collider2D>();
        _playerRenderer = _player.GetComponentInChildren<SpriteRenderer>();
    }

    private Vector2 GetClosestCarPointToPlayer()
    {
        Vector3 target = _player != null ? _player.position : transform.position;

        if (_carCollider != null)
            return _carCollider.ClosestPoint(target);

        if (_carRenderer != null)
            return _carRenderer.bounds.ClosestPoint(target);

        return transform.position;
    }

    private Vector2 GetClosestPlayerPointToCar()
    {
        Vector3 target = transform.position;

        if (_playerCollider != null)
            return _playerCollider.ClosestPoint(target);

        if (_playerRenderer != null)
            return _playerRenderer.bounds.ClosestPoint(target);

        return _player != null ? _player.position : transform.position;
    }

    private void RefreshVisualReferences()
    {
        if (_carCollider == null)
            _carCollider = GetComponent<Collider2D>();

        if (_carRenderer == null)
            _carRenderer = GetComponentInChildren<SpriteRenderer>();

        if (_player != null && _playerCollider == null)
            _playerCollider = _player.GetComponent<Collider2D>();

        if (_player != null && _playerRenderer == null)
            _playerRenderer = _player.GetComponentInChildren<SpriteRenderer>();
    }

    private bool AreVisualBoundsOverlapping()
    {
        if (_carRenderer == null || _playerRenderer == null)
            return false;

        Bounds carBounds = _carRenderer.bounds;
        Bounds playerBounds = _playerRenderer.bounds;
        carBounds.Expand(new Vector3(0.35f, 0.35f, 0f));
        playerBounds.Expand(new Vector3(0.35f, 0.35f, 0f));
        return carBounds.Intersects(playerBounds);
    }

    private bool IsScreenCloseToPlayer()
    {
        if (_player == null || Camera.main == null)
            return false;

        Vector3 carScreen = Camera.main.WorldToScreenPoint(transform.position);
        Vector3 playerScreen = Camera.main.WorldToScreenPoint(_player.position);

        if (carScreen.z < 0f || playerScreen.z < 0f)
            return false;

        return Vector2.Distance(carScreen, playerScreen) <= screenTriggerDistance;
    }

    private static void EnsureWarningSprite()
    {
        if (s_warningSprite != null)
            return;

        Texture2D texture = new Texture2D(IconTextureSize, IconTextureSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Color clear = new Color(0f, 0f, 0f, 0f);
        Color[] pixels = texture.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = clear;

        texture.SetPixels(pixels);

        Color bubble = new Color(1f, 0.94f, 0.72f, 1f);
        Color edge = new Color(0.37f, 0.22f, 0.08f, 1f);
        Color mark = new Color(1f, 0.45f, 0.05f, 1f);

        DrawCircle(texture, 16, 17, 13, edge);
        DrawCircle(texture, 16, 17, 11, bubble);
        DrawLine(texture, 16, 24, 16, 13, mark, 4);
        DrawCircle(texture, 16, 8, 2, mark);

        texture.Apply();
        s_warningSprite = Sprite.Create(texture, new Rect(0, 0, IconTextureSize, IconTextureSize), new Vector2(0.5f, 0.5f), IconTextureSize);
    }

    private static AudioClip CreateHonkClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.48f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float pulseA = SmoothPulse(t, 0.02f, 0.18f);
            float pulseB = SmoothPulse(t, 0.27f, 0.17f);
            float envelope = Mathf.Max(pulseA, pulseB);
            float wobble = Mathf.Sin(t * 28f) * 9f;
            float toneA = Mathf.Sin((330f + wobble) * Mathf.PI * 2f * t);
            float toneB = Mathf.Sin((392f + wobble) * Mathf.PI * 2f * t);
            float softEdge = Mathf.Sin((660f + wobble) * Mathf.PI * 2f * t) * 0.12f;
            samples[i] = (toneA * 0.55f + toneB * 0.33f + softEdge) * envelope * 0.82f;
        }

        AudioClip clip = AudioClip.Create("RuntimeCarHonk", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static float SmoothPulse(float time, float start, float length)
    {
        if (time < start || time > start + length)
            return 0f;

        float local = (time - start) / length;
        float attack = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(local / 0.18f));
        float release = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - local) / 0.35f));
        return attack * release;
    }

    private static void DrawCircle(Texture2D texture, int cx, int cy, int radius, Color color)
    {
        int r2 = radius * radius;
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (x * x + y * y <= r2)
                    SetPixel(texture, cx + x, cy + y, color);
            }
        }
    }

    private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color color, int thickness)
    {
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            for (int y = -thickness / 2; y <= thickness / 2; y++)
            {
                for (int x = -thickness / 2; x <= thickness / 2; x++)
                    SetPixel(texture, x0 + x, y0 + y, color);
            }

            if (x0 == x1 && y0 == y1)
                break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    private static void SetPixel(Texture2D texture, int x, int y, Color color)
    {
        if (x < 0 || x >= IconTextureSize || y < 0 || y >= IconTextureSize)
            return;

        texture.SetPixel(x, y, color);
    }

    private class PlayerWarningIcon : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Transform _target;
        private Vector3 _offset;
        private float _hideTime;
        private float _seed;

        public static void Show(Transform target, Vector3 offset, float duration)
        {
            if (target == null)
                return;

            PlayerWarningIcon icon = target.GetComponentInChildren<PlayerWarningIcon>();
            if (icon == null)
            {
                GameObject go = new GameObject("NearMissWarningIcon");
                go.transform.SetParent(target, false);
                icon = go.AddComponent<PlayerWarningIcon>();
            }

            icon.Initialize(target, offset, duration);
        }

        private void Initialize(Transform target, Vector3 offset, float duration)
        {
            EnsureWarningSprite();

            _target = target;
            _offset = offset;
            _hideTime = Time.time + Mathf.Max(0.1f, duration);
            _seed = Random.Range(0f, 10f);

            if (_renderer == null)
                _renderer = gameObject.AddComponent<SpriteRenderer>();

            _renderer.sprite = s_warningSprite;
            _renderer.sortingOrder = 200;
            _renderer.enabled = true;
            transform.localScale = Vector3.one * 0.45f;
        }

        private void LateUpdate()
        {
            if (_target == null || Time.time >= _hideTime)
            {
                if (_renderer != null)
                    _renderer.enabled = false;

                return;
            }

            float bob = Mathf.Sin((Time.time + _seed) * 8f) * 0.05f;
            transform.localPosition = _offset + Vector3.up * bob;
        }
    }

    private class CameraNearMissShake : MonoBehaviour
    {
        private static CameraNearMissShake s_instance;

        private Coroutine _routine;
        private Vector3 _originalLocalPosition;

        public static void Shake(float duration, float strength)
        {
            Camera camera = Camera.main;
            if (camera == null)
                return;

            if (s_instance == null || s_instance.gameObject != camera.gameObject)
                s_instance = camera.gameObject.GetComponent<CameraNearMissShake>() ?? camera.gameObject.AddComponent<CameraNearMissShake>();

            if (s_instance._routine != null)
                s_instance.StopCoroutine(s_instance._routine);

            s_instance._routine = s_instance.StartCoroutine(s_instance.ShakeRoutine(duration, strength));
        }

        private IEnumerator ShakeRoutine(float duration, float strength)
        {
            _originalLocalPosition = transform.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float fade = 1f - Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, duration));
                Vector2 random = Random.insideUnitCircle * (strength * fade);
                transform.localPosition = _originalLocalPosition + new Vector3(random.x, random.y, 0f);
                yield return null;
            }

            transform.localPosition = _originalLocalPosition;
            _routine = null;
        }
    }
}
