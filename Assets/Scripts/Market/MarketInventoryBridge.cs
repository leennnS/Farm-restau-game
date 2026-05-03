using UnityEngine;

public class MarketInventoryBridge : MonoBehaviour
{
    private InventoryController inventoryController;

    private void Start()
    {
        ResolveInventoryController();

        if (inventoryController == null)
            Debug.LogError("[MarketInventoryBridge] InventoryController not found in scene!");
    }

    public bool TryReceivePurchase(MarketItemEntry item, int amount, out string message)
    {
        message = string.Empty;
        Debug.Log($"[MarketInventoryBridge] Purchased: {item.itemName} x{amount}");

        ResolveInventoryController();

        if (inventoryController == null)
        {
            Debug.LogError("[MarketInventoryBridge] Cannot add item - InventoryController not found!");
            message = "Inventory not found.";
            return false;
        }

        if (item.itemDefinition == null)
        {
            Debug.LogError($"[MarketInventoryBridge] MarketItemEntry '{item.itemName}' has no ItemDefinition assigned!");
            message = $"{item.itemName} is not configured correctly.";
            return false;
        }

        // Add the item to the real inventory
        bool success = inventoryController.TryAdd(item.itemDefinition, amount);

        if (success)
        {
            Debug.Log($"[MarketInventoryBridge] Successfully added {item.itemName} x{amount} to inventory!");
            message = $"Purchased {item.itemName} x{amount}.";
            return true;
        }

        Debug.LogWarning($"[MarketInventoryBridge] Failed to add {item.itemName} x{amount} - inventory may be full!");
        message = $"Inventory full. Could not add {item.itemName}.";
        return false;
    }

    private void ResolveInventoryController()
    {
        if (inventoryController != null)
            return;

        inventoryController = InventoryController.Instance;
        if (inventoryController == null)
            inventoryController = FindFirstObjectByType<InventoryController>();
    }
}
