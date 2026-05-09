using UnityEngine;

public class HouseDeliveryDropZone : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private KeyCode dropKey = KeyCode.E;
    [SerializeField] private string playerTag = "Player";
    [SerializeField, Min(0.25f)] private float dropDistance = 2f;
    [SerializeField] private string promptMessage = "Press E to put the books down";
    [SerializeField] private string droppedMessage = "Books placed";

    [Header("Placed Visual")]
    [SerializeField] private Transform placedItemPoint;
    [SerializeField] private SpriteRenderer placedItemRenderer;
    [SerializeField] private Sprite fallbackPlacedSprite;
    [SerializeField] private Vector3 placedItemScale = Vector3.one;
    [SerializeField, Min(0.05f)] private float placedItemTargetWorldHeight = 0.55f;
    [SerializeField] private int placedItemSortingOrder = 120;

    [Header("References")]
    [SerializeField] private PickupToastUIToolkit pickupToast;

    private Transform _playerTransform;
    private PlayerCarryVisualController _carryVisual;
    private bool _promptShowing;
    private bool _hasPlacedItem;

    private void Awake()
    {
        ResolveReferences();
        EnsurePlacedRenderer();
        SetPlacedVisualVisible(false);
    }

    private void Update()
    {
        ResolveReferences();

        if (_playerTransform == null || _carryVisual == null || !_carryVisual.HasHeldItem || _hasPlacedItem)
        {
            HidePrompt();
            return;
        }

        bool inRange = Vector2.Distance(_playerTransform.position, transform.position) <= dropDistance;
        if (!inRange)
        {
            HidePrompt();
            return;
        }

        ShowPrompt();

        if (Input.GetKeyDown(dropKey))
            DropHeldItem();
    }

    private void DropHeldItem()
    {
        if (_carryVisual == null || !_carryVisual.HasHeldItem)
            return;

        EnsurePlacedRenderer();
        if (placedItemRenderer != null)
        {
            Transform targetParent = placedItemPoint != null ? placedItemPoint : transform;
            placedItemRenderer.transform.SetParent(targetParent, false);
            placedItemRenderer.transform.localPosition = Vector3.zero;
            placedItemRenderer.sprite = _carryVisual.HeldItemSprite != null ? _carryVisual.HeldItemSprite : fallbackPlacedSprite;
            placedItemRenderer.sortingOrder = placedItemSortingOrder;
            placedItemRenderer.transform.localScale = ResolvePlacedItemScale(targetParent, placedItemRenderer.sprite);
            SetPlacedVisualVisible(true);
        }

        string itemName = !string.IsNullOrWhiteSpace(_carryVisual.HeldItemName) ? _carryVisual.HeldItemName : "delivery";
        ClearAllPlayerCarryVisuals();
        _hasPlacedItem = true;
        HidePrompt();
        pickupToast?.Show($"{droppedMessage}: {itemName}");
    }

    private void ClearAllPlayerCarryVisuals()
    {
        PlayerCarryVisualController[] carryVisuals = FindObjectsByType<PlayerCarryVisualController>(FindObjectsSortMode.None);
        for (int i = 0; i < carryVisuals.Length; i++)
        {
            if (carryVisuals[i] != null)
                carryVisuals[i].ClearCarry();
        }
    }

    private void EnsurePlacedRenderer()
    {
        if (placedItemRenderer != null)
            return;

        GameObject visual = new GameObject("PlacedDeliveryBooks");
        visual.transform.SetParent(placedItemPoint != null ? placedItemPoint : transform, false);
        visual.transform.localPosition = Vector3.zero;
        placedItemRenderer = visual.AddComponent<SpriteRenderer>();
    }

    private void SetPlacedVisualVisible(bool visible)
    {
        if (placedItemRenderer != null)
            placedItemRenderer.enabled = visible;
    }

    private Vector3 ResolvePlacedItemScale(Transform parent, Sprite sprite)
    {
        if (sprite == null || sprite.bounds.size.y <= 0f)
            return placedItemScale;

        float parentScaleY = parent != null ? parent.lossyScale.y : 1f;
        if (Mathf.Approximately(parentScaleY, 0f))
            parentScaleY = 1f;

        float targetLocalHeight = placedItemTargetWorldHeight / Mathf.Abs(parentScaleY);
        float uniformScale = targetLocalHeight / sprite.bounds.size.y;
        return new Vector3(uniformScale, uniformScale, placedItemScale.z);
    }

    private void ShowPrompt()
    {
        if (_promptShowing)
            return;

        _promptShowing = true;
        pickupToast?.ShowPersistent(promptMessage, 24);
    }

    private void HidePrompt()
    {
        if (!_promptShowing)
            return;

        _promptShowing = false;
        pickupToast?.Hide();
    }

    private void ResolveReferences()
    {
        if (_playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                _playerTransform = player.transform;
                _carryVisual = player.GetComponent<PlayerCarryVisualController>();
            }
        }

        if (_carryVisual == null && _playerTransform != null)
            _carryVisual = _playerTransform.GetComponent<PlayerCarryVisualController>();

        if (pickupToast == null)
            pickupToast = FindFirstObjectByType<PickupToastUIToolkit>();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, dropDistance);
    }
}
