using UnityEngine;

/// <summary>
/// Simple egg item script with metadata
/// Can be extended for special egg behaviors (healing, cooking, etc.)
/// </summary>
public class EggItem : MonoBehaviour
{
    [SerializeField] private string itemName = "Egg";
    [SerializeField] private string itemDescription = "A fresh chicken egg. Great for cooking or selling!";
    [SerializeField] private Sprite itemIcon;
    [SerializeField] private int sellPrice = 75;

    public string ItemName => itemName;
    public string ItemDescription => itemDescription;
    public Sprite ItemIcon => itemIcon;
    public int SellPrice => sellPrice;
}
