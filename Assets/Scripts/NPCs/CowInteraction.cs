using UnityEngine;

public class CowInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private ItemDefinition milkItemDefinition;
    [SerializeField] private int milkQuantity = 1;
    [SerializeField] private float milkCooldown = 2f;

    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private InventoryController playerInventory;

    [Header("Feedback")]
    [SerializeField] private PickupToastUIToolkit pickupToast;

    private float timeSinceLastMilk;
    private GameObject milkFlyEffectPrefab;

    private void Start()
    {
        timeSinceLastMilk = milkCooldown;

        // Load milk prefab from Resources
        milkFlyEffectPrefab = Resources.Load<GameObject>("Prefabs/Items/MilkFlyEffect");
        if (milkFlyEffectPrefab == null)
        {
            Debug.LogError("[Cow] Failed to load MilkFlyEffect prefab from Resources!");
        }
        else
        {
            Debug.Log("[Cow] Successfully loaded MilkFlyEffect prefab");
        }

        if (pickupToast == null)
            pickupToast = FindFirstObjectByType<PickupToastUIToolkit>();

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }

        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<InventoryController>();
    }

    private void Update()
    {
        if (playerTransform == null || playerInventory == null || milkItemDefinition == null)
            return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer <= interactionDistance && Input.GetKeyDown(interactionKey))
        {
            MilkCow();
        }

        timeSinceLastMilk += Time.deltaTime;
    }

    private void MilkCow()
    {
        if (timeSinceLastMilk < milkCooldown)
            return;

        if (playerInventory.TryAdd(milkItemDefinition, milkQuantity))
        {
            timeSinceLastMilk = 0f;
            Debug.Log("[Cow] Milk added to inventory!");

            // UI popup
            if (pickupToast != null)
                pickupToast.Show($"+{milkQuantity} {milkItemDefinition.displayName}");

            // Flying milk effect
            if (milkFlyEffectPrefab != null && playerTransform != null)
            {
                Debug.Log("[Cow] Spawning milk fly effect prefab...");
                GameObject fx = Instantiate(milkFlyEffectPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                ItemFlyToPlayer fly = fx.GetComponent<ItemFlyToPlayer>();

                if (fly != null)
                {
                    Debug.Log($"[Cow] Found ItemFlyToPlayer! Initializing with player at {playerTransform.position}");
                    fly.Initialize(playerTransform);
                }
                else
                {
                    Debug.LogError("[Cow] Milk prefab does NOT have ItemFlyToPlayer script!");
                }
            }
            else
            {
                if (milkFlyEffectPrefab == null)
                    Debug.LogError("[Cow] Milk Fly Effect Prefab NOT assigned!");
                if (playerTransform == null)
                    Debug.LogError("[Cow] Player Transform NOT found!");
            }
        }
        else
        {
            Debug.Log("Inventory full!");
        }
    }
}