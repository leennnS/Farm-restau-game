using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Defines a crop: its growth stages, timing, seed/harvest items, and tile graphics.
/// Create via right-click menu: Create > Farming/Crop Definition
/// </summary>
[CreateAssetMenu(menuName = "Farming/Crop Definition")]
public class CropDefinition : ScriptableObject
{
    [Header("Crop Identity")]
    public string cropId;
    public string displayName;
    public Sprite cropIcon;

    [Header("Growth Stages")]
    [Tooltip("Tile graphics for each stage of growth (0 = seedling, last = mature)")]
    public TileBase[] stageTiles;

    [Tooltip("Days required to grow before advancing to next stage")]
    public int[] daysPerStage;

    [Header("Items")]
    [Tooltip("The seed item to consume when planting")]
    public ItemDefinition seedItem;

    [Tooltip("The item to produce when harvesting")]
    public ItemDefinition harvestItem;

    [Tooltip("Amount of harvest item to give when harvesting")]
    [Min(1)]
    public int harvestAmount = 1;

    [Header("Settings")]
    [Tooltip("If true, soil must be watered daily for crop to grow")]
    public bool requiresWatering = false;

    [Tooltip("If true, finished harvest reverts soil to dirt (not harvested)")]
    public bool reveritToSoilAfterHarvest = true;

    public int TotalStages => stageTiles != null ? stageTiles.Length : 0;
    public bool IsValid => !string.IsNullOrEmpty(cropId) && stageTiles != null && stageTiles.Length > 0 && seedItem != null && harvestItem != null;

    public int GetDaysToNextStage(int currentStage)
    {
        if (daysPerStage == null || currentStage < 0 || currentStage >= daysPerStage.Length)
            return 1;
        return daysPerStage[currentStage];
    }

    public TileBase GetStageTile(int stage)
    {
        if (stageTiles == null || stage < 0 || stage >= stageTiles.Length)
            return null;
        return stageTiles[stage];
    }

    private void OnValidate()
    {
        // Ensure arrays match in length
        if (stageTiles != null && daysPerStage != null)
        {
            if (stageTiles.Length != daysPerStage.Length)
            {
                System.Array.Resize(ref daysPerStage, stageTiles.Length);
                for (int i = 0; i < daysPerStage.Length; i++)
                {
                    if (daysPerStage[i] <= 0)
                        daysPerStage[i] = 1;
                }
            }
        }

        if (string.IsNullOrEmpty(cropId))
            cropId = this.name;
    }
}
