using UnityEngine;

/// <summary>
/// Handles selection of what creature to catch based on zone pool and RNG.
/// Uses weighted random selection so you can make rarer creatures less common.
/// </summary>
public class CatchableSelector
{
    /// <summary>
    /// Selects a random catchable from a zone's pool based on spawn weights.
    /// </summary>
    public static CatchableDefinition SelectCatchable(LakeZoneDefinition zone)
    {
        if (zone == null || zone.catchablePool == null || zone.catchablePool.Length == 0)
        {
            Debug.LogWarning("CatchableSelector: Zone has no catchables defined!");
            return null;
        }

        // Filter enabled catchables and sum weights
        float totalWeight = 0f;
        System.Collections.Generic.List<(CatchableEntry entry, float weight)> enabledPool =
            new System.Collections.Generic.List<(CatchableEntry, float)>();

        foreach (var entry in zone.catchablePool)
        {
            if (entry.enabled && entry.catchable != null)
            {
                enabledPool.Add((entry, entry.spawnWeight));
                totalWeight += entry.spawnWeight;
            }
        }

        if (enabledPool.Count == 0)
        {
            Debug.LogWarning("CatchableSelector: No enabled catchables in zone!");
            return null;
        }

        // Weighted random selection
        float randomValue = Random.value * totalWeight;
        float accumulated = 0f;

        foreach (var (entry, weight) in enabledPool)
        {
            accumulated += weight;
            if (randomValue <= accumulated)
            {
                return entry.catchable;
            }
        }

        // Fallback (shouldn't reach here)
        return enabledPool[enabledPool.Count - 1].entry.catchable;
    }

    /// <summary>
    /// Gets a catchable by name from a zone (useful for testing/debugging).
    /// </summary>
    public static CatchableDefinition GetCatchableByName(LakeZoneDefinition zone, string name)
    {
        if (zone == null || zone.catchablePool == null)
            return null;

        foreach (var entry in zone.catchablePool)
        {
            if (entry.enabled && entry.catchable != null && entry.catchable.catchableName == name)
            {
                return entry.catchable;
            }
        }

        return null;
    }
}
