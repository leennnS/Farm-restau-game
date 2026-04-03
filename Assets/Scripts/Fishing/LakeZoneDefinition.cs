using UnityEngine;

/// <summary>
/// Defines a fishing zone in the lake - what creatures can be caught here and their probabilities.
/// You can have multiple zones (deep water, shallows, rocky area, etc.)
/// Create different LakeZoneDefinition ScriptableObjects for each area.
/// </summary>
[CreateAssetMenu(menuName = "Fishing/Lake Zone Definition", fileName = "New_LakeZone")]
public class LakeZoneDefinition : ScriptableObject
{
    [Header("Zone Info")]
    [SerializeField] public string zoneName = "Lake";
    [TextArea(2, 3)]
    [SerializeField] public string zoneDescription = "A peaceful fishing spot";

    [Header("Catchable Pool")]
    [SerializeField] public CatchableEntry[] catchablePool;

    [Header("Zone Difficulty Modifier")]
    [SerializeField, Range(0f, 1f)] public float difficultyModifier = 1f; // Multiplies creature difficulty

    [Header("Time/Weather Effects (Optional)")]
    [SerializeField] public bool affectedByTime = false;
    [SerializeField] public bool affectedByWeather = false;
}

/// <summary>
/// Entry in a zone's catchable pool. Defines a catchable and its spawn weight/rarity.
/// </summary>
[System.Serializable]
public class CatchableEntry
{
    [SerializeField] public CatchableDefinition catchable;
    [SerializeField, Range(0.1f, 100f)] public float spawnWeight = 1f; // Higher = more likely to catch
    [SerializeField] public bool enabled = true;
}
