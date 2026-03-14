using UnityEngine;

public class MarketInventoryBridge : MonoBehaviour
{
    private InventoryController inventoryController;

    private void Start()
    {
        // Find the InventoryController in the scene
        inventoryController = FindFirstObjectByType<InventoryController>();

        if (inventoryController == null)
            Debug.LogError("[MarketInventoryBridge] InventoryController not found in scene!");
    }

    public void ReceivePurchase(MarketItemEntry item, int amount)
    {
        Debug.Log($"[MarketInventoryBridge] Purchased: {item.itemName} x{amount}");

        if (inventoryController == null)
        {
            Debug.LogError("[MarketInventoryBridge] Cannot add item - InventoryController not found!");
            return;
        }

        if (item.itemDefinition == null)
        {
            Debug.LogError($"[MarketInventoryBridge] MarketItemEntry '{item.itemName}' has no ItemDefinition assigned!");
            return;
        }

        // Add the item to the real inventory
        bool success = inventoryController.TryAdd(item.itemDefinition, amount);

        if (success)
        {
            Debug.Log($"[MarketInventoryBridge] Successfully added {item.itemName} x{amount} to inventory!");
        }
        else
        {
            Debug.LogWarning($"[MarketInventoryBridge] Failed to add {item.itemName} x{amount} - inventory may be full!");
        }
    }
}