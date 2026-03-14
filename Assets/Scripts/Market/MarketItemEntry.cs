using System;
using UnityEngine;

[Serializable]
public class MarketItemEntry
{
    public string itemName;
    public Sprite icon;
    public int price = 10;
    [TextArea] public string description;
    public bool available = true;
    public ItemDefinition itemDefinition;  // Reference to the actual inventory item
}