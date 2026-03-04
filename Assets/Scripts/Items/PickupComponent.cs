using UnityEngine;

public class PickupComponent : MonoBehaviour
{
    private Transform player;

    [Header("Magnet Settings")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float pickupDistance = 1.5f;
    [SerializeField] private float collectDistance = 0.1f;

    [Header("Lifetime")]
    [SerializeField] private float ttl = 10f;

    [Header("Item")]
    public ItemDefinition item;
    public int count = 1;

    private InventoryController inv;
    private PickupToastUIToolkit pickupToast;
    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        // Find player by tag
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        player = playerGO != null ? playerGO.transform : null;

        // Find inventory controller in scene
        inv = FindFirstObjectByType<InventoryController>();

        // Find pickup toast UI for notifications
        pickupToast = FindFirstObjectByType<PickupToastUIToolkit>();

        // Show sprite in world (optional)
        if (sr != null && item != null && item.icon != null)
            sr.sprite = item.icon;
    }

    public void Set(ItemDefinition newItem, int newCount)
    {
        item = newItem;
        count = newCount;

        if (sr != null && item != null && item.icon != null)
            sr.sprite = item.icon;
    }

    public void SetTimeToLive(float newTTL)
    {
        ttl = newTTL;
    }

    private void Update()
    {
        if (player == null || inv == null || item == null) return;

        ttl -= Time.deltaTime;
        if (ttl <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > pickupDistance)
            return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            player.position,
            speed * Time.deltaTime
        );

        if (distance <= collectDistance)
        {
            bool added = inv.TryAdd(item, count);

            if (added)
            {
                // Show pickup notification
                if (pickupToast != null && item != null)
                    pickupToast.Show($"+{count} {item.displayName}");

                Destroy(gameObject);
            }
            else
                Debug.Log("Inventory full (could not add).");
        }
    }
}
