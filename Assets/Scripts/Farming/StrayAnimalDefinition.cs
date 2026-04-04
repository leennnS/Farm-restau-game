using UnityEngine;

/// <summary>
/// Defines a stray animal type (pig, wolf, etc) and its behavior stats.
/// Create different instances for different animals.
/// </summary>
[CreateAssetMenu(menuName = "Farming/Stray Animal Definition", fileName = "New_StrayAnimal")]
public class StrayAnimalDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] public string animalName = "Stray Pig";
    [SerializeField] public Sprite animalSprite;
    [SerializeField] public Color animalColor = Color.white;

    [Header("Movement")]
    [SerializeField, Range(0.5f, 3f)] public float moveSpeed = 1f;
    [SerializeField, Range(1f, 5f)] public float minWalkTime = 1.5f;
    [SerializeField, Range(1f, 5f)] public float maxWalkTime = 3f;
    [SerializeField, Range(0.5f, 3f)] public float minIdleTime = 1f;
    [SerializeField, Range(0.5f, 3f)] public float maxIdleTime = 2.5f;

    [Header("Crop Destruction")]
    [SerializeField, Range(0.1f, 2f)] public float cropDetectionRadius = 1f;
    [SerializeField, Range(0f, 1f)] public float destructionChancePerSecond = 0.5f; // Chance to destroy crop each second in range

    [Header("Persistence")]
    [SerializeField, Range(5f, 120f)] public float lifeTimeDuration = 30f; // How long the animal stays on farm (seconds)
    [SerializeField] public bool despawnWhenNoMoreCrops = true;
}
