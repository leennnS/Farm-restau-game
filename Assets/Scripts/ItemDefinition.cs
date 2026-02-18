using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item")]
public class ItemDefinition : ScriptableObject
{
    public string displayName;
    public Sprite icon;
    public int maxStack = 99;
}
