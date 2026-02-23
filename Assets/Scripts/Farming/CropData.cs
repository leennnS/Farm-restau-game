using UnityEngine;

/// <summary>
/// Runtime data for a single planted crop at a specific tile location.
/// Tracks growth stage, time progress, watered status.
/// </summary>
public struct CropData
{
    public string cropId;         // Reference to CropDefinition
    public int currentStage;      // 0 = seedling, TotalStages-1 = mature
    public int dayProgress;       // Progress toward next stage (0 to daysPerStage[stage])
    public bool wasWateredToday;  // Whether this crop was watered today

    public CropData(string id, int stage = 0)
    {
        cropId = id;
        currentStage = stage;
        dayProgress = 0;
        wasWateredToday = false;
    }

    public bool IsMature(CropDefinition cropDef)
    {
        if (cropDef == null) return false;
        return currentStage >= cropDef.TotalStages - 1;
    }

    public void AdvanceDay(CropDefinition cropDef, bool isWatered)
    {
        if (cropDef == null || IsMature(cropDef)) return;

        // If crop requires watering and wasn't watered, don't grow
        if (cropDef.requiresWatering && !isWatered)
            return;

        dayProgress++;
        int daysNeeded = cropDef.GetDaysToNextStage(currentStage);

        if (dayProgress >= daysNeeded)
        {
            dayProgress = 0;
            currentStage++;
            if (currentStage >= cropDef.TotalStages)
                currentStage = cropDef.TotalStages - 1; // Cap at mature
        }

        wasWateredToday = false; // Reset for next day
    }
}

/// <summary>
/// Serializable wrapper for saving/loading crop data
/// </summary>
[System.Serializable]
public struct CropDataSerializable
{
    public int cellX;
    public int cellY;
    public string cropId;
    public int currentStage;
    public int dayProgress;
    public bool wasWateredToday;
}
