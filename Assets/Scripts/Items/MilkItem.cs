using UnityEngine;

public class MilkItem : MonoBehaviour
{
    [SerializeField] private string itemName = "Milk";
    [SerializeField] private string itemDescription = "Fresh milk from a cow. Can be used for cooking.";
    [SerializeField] private Sprite itemIcon;
    [SerializeField] private int sellPrice = 100;

    public string ItemName => itemName;
    public string ItemDescription => itemDescription;
    public Sprite ItemIcon => itemIcon;
    public int SellPrice => sellPrice;
}
