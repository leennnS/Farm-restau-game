using UnityEngine;

/// <summary>
/// Extended ItemDefinition for watering can with empty/full visual states.
/// Shows different sprites based on water durability level.
/// </summary>
[CreateAssetMenu(menuName = "Inventory/Watering Can")]
public class WateringCanItem : ItemDefinition
{
    [Header("Watering Can Sprites")]
    [SerializeField] public Sprite fullSprite;    // Shown when can has water
    [SerializeField] public Sprite emptySprite;   // Shown when can is empty

    /// <summary>
    /// Get the appropriate sprite based on current water durability.
    /// </summary>
    public Sprite GetSpriteForDurability(int currentDurability, int maxDurability)
    {
        if (currentDurability > 0)
            return fullSprite != null ? fullSprite : icon;
        else
            return emptySprite != null ? emptySprite : icon;
    }
}
