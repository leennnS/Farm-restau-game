using UnityEngine;

public class PlayerCarryVisualController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer playerSpriteRenderer;
    [SerializeField] private CharacterController2D movementController;

    [Header("Carry Animation Sprites")]
    [SerializeField] private Sprite[] carryDownSprites;
    [SerializeField] private Sprite[] carryUpSprites;
    [SerializeField] private Sprite[] carryLeftSprites;
    [SerializeField] private Sprite[] carryRightSprites;
    [SerializeField, Min(1f)] private float framesPerSecond = 8f;

    [Header("Carry State")]
    [SerializeField] private bool holdUntilCleared = false;
    [SerializeField, Min(0.1f)] private float defaultCarryDuration = 2.5f;

    [Header("Held Item Visual")]
    [SerializeField] private Vector3 heldItemLocalOffset = new Vector3(0f, 3.2f, 0f);
    [SerializeField] private Vector3 heldItemLocalScale = Vector3.one;
    [SerializeField] private bool scaleHeldItemWithPlayerHeight = true;
    [SerializeField, Min(0.05f)] private float heldItemPlayerHeightRatio = 0.45f;
    [SerializeField, Min(0.05f)] private float heldItemTargetWorldHeight = 2.2f;
    [SerializeField] private int heldItemSortingOrder = 250;

    private bool _isCarrying;
    private float _carryEndTime;
    private Vector2 _lastDirection = Vector2.down;
    private SpriteRenderer _heldItemRenderer;
    private Sprite _heldItemSprite;
    private string _heldItemName;

    public bool IsCarrying => _isCarrying;
    public bool HasHeldItem => _isCarrying && _heldItemSprite != null;
    public Sprite HeldItemSprite => _heldItemSprite;
    public string HeldItemName => _heldItemName;

    private void Awake()
    {
        ResolveReferences();
    }

    private void LateUpdate()
    {
        if (!_isCarrying)
            return;

        if (!holdUntilCleared && Time.time >= _carryEndTime)
        {
            ClearCarry();
            return;
        }

        ApplyCarrySprite();
    }

    public void ShowCarry(float duration)
    {
        ResolveReferences();

        _isCarrying = true;
        _carryEndTime = Time.time + Mathf.Max(0.1f, duration > 0f ? duration : defaultCarryDuration);
        ApplyCarrySprite();
    }

    public void ShowCarry()
    {
        ShowCarry(defaultCarryDuration);
    }

    public void StartCarryingItem(Sprite itemSprite, string itemName, bool keepUntilCleared = true)
    {
        ResolveReferences();

        _heldItemSprite = itemSprite;
        _heldItemName = itemName;
        holdUntilCleared = keepUntilCleared;
        _isCarrying = true;
        _carryEndTime = Time.time + defaultCarryDuration;

        EnsureHeldItemRenderer();
        ApplyHeldItemVisual();
        ApplyCarrySprite();
    }

    public void ClearCarry()
    {
        _isCarrying = false;
        _heldItemSprite = null;
        _heldItemName = null;

        if (_heldItemRenderer != null)
        {
            _heldItemRenderer.sprite = null;
            _heldItemRenderer.enabled = false;
        }
    }

    public void SetHoldUntilCleared(bool hold)
    {
        holdUntilCleared = hold;
    }

    public void ConfigureCarrySprites(Sprite[] downSprites, Sprite[] upSprites, Sprite[] leftSprites, Sprite[] rightSprites, float animationFramesPerSecond)
    {
        if (downSprites != null && downSprites.Length > 0)
            carryDownSprites = downSprites;

        if (upSprites != null && upSprites.Length > 0)
            carryUpSprites = upSprites;

        if (leftSprites != null && leftSprites.Length > 0)
            carryLeftSprites = leftSprites;

        if (rightSprites != null && rightSprites.Length > 0)
            carryRightSprites = rightSprites;

        if (animationFramesPerSecond > 0f)
            framesPerSecond = animationFramesPerSecond;
    }

    private void ApplyCarrySprite()
    {
        if (playerSpriteRenderer == null)
            return;

        Vector2 direction = ResolveDirection();
        Sprite[] frames = ResolveFrames(direction);
        if (frames == null || frames.Length == 0)
            return;

        bool isMoving = movementController != null && movementController.moving;
        int frameIndex = isMoving ? Mathf.FloorToInt(Time.time * framesPerSecond) % frames.Length : 0;
        if (frames[frameIndex] != null)
            playerSpriteRenderer.sprite = frames[frameIndex];

        ApplyHeldItemVisual();
    }

    private void EnsureHeldItemRenderer()
    {
        if (_heldItemRenderer != null)
            return;

        Transform existing = transform.Find("CarriedDeliveryVisual");
        if (existing != null)
            _heldItemRenderer = existing.GetComponent<SpriteRenderer>();

        if (_heldItemRenderer == null)
        {
            GameObject visual = new GameObject("CarriedDeliveryVisual");
            visual.transform.SetParent(transform, false);
            _heldItemRenderer = visual.AddComponent<SpriteRenderer>();
        }
    }

    private void ApplyHeldItemVisual()
    {
        if (_heldItemSprite == null)
            return;

        EnsureHeldItemRenderer();
        if (_heldItemRenderer == null)
            return;

        _heldItemRenderer.sprite = _heldItemSprite;
        _heldItemRenderer.enabled = true;
        if (playerSpriteRenderer != null)
        {
            _heldItemRenderer.sortingLayerID = playerSpriteRenderer.sortingLayerID;
            _heldItemRenderer.sortingOrder = Mathf.Max(heldItemSortingOrder, playerSpriteRenderer.sortingOrder + 10);
        }
        else
        {
            _heldItemRenderer.sortingOrder = heldItemSortingOrder;
        }

        _heldItemRenderer.transform.localPosition = heldItemLocalOffset;
        _heldItemRenderer.transform.localScale = ResolveHeldItemScale();
    }

    private Vector3 ResolveHeldItemScale()
    {
        if (_heldItemSprite == null || _heldItemSprite.bounds.size.y <= 0f)
            return heldItemLocalScale;

        float parentScaleY = transform.lossyScale.y;
        if (Mathf.Approximately(parentScaleY, 0f))
            parentScaleY = 1f;

        float targetWorldHeight = heldItemTargetWorldHeight;
        if (scaleHeldItemWithPlayerHeight && playerSpriteRenderer != null && playerSpriteRenderer.bounds.size.y > 0f)
            targetWorldHeight = playerSpriteRenderer.bounds.size.y * heldItemPlayerHeightRatio;

        float targetLocalHeight = targetWorldHeight / Mathf.Abs(parentScaleY);
        float uniformScale = targetLocalHeight / _heldItemSprite.bounds.size.y;
        return new Vector3(uniformScale, uniformScale, heldItemLocalScale.z);
    }

    private Vector2 ResolveDirection()
    {
        if (movementController != null && movementController.moving)
        {
            _lastDirection = movementController.lastmotionVector;
        }
        else if (movementController != null && movementController.lastmotionVector.sqrMagnitude > 0.001f)
        {
            _lastDirection = movementController.lastmotionVector;
        }

        if (_lastDirection.sqrMagnitude <= 0.001f)
            _lastDirection = Vector2.down;

        return _lastDirection.normalized;
    }

    private Sprite[] ResolveFrames(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return direction.x < 0f ? FirstNonEmpty(carryLeftSprites, carryRightSprites, carryDownSprites) : FirstNonEmpty(carryRightSprites, carryLeftSprites, carryDownSprites);

        return direction.y > 0f ? FirstNonEmpty(carryUpSprites, carryDownSprites, carryRightSprites) : FirstNonEmpty(carryDownSprites, carryUpSprites, carryRightSprites);
    }

    private static Sprite[] FirstNonEmpty(params Sprite[][] options)
    {
        for (int i = 0; i < options.Length; i++)
        {
            Sprite[] option = options[i];
            if (option != null && option.Length > 0)
                return option;
        }

        return null;
    }

    private void ResolveReferences()
    {
        if (playerSpriteRenderer == null)
            playerSpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (movementController == null)
            movementController = GetComponent<CharacterController2D>();
    }
}
