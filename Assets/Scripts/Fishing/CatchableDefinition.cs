using UnityEngine;

/// <summary>
/// Defines a single catchable creature/fish/item in the fishing system.
/// ScriptableObject so you can create different creatures in the inspector.
/// All sprite fields are optional - leave null if you'll assign them later.
/// </summary>
[CreateAssetMenu(menuName = "Fishing/Catchable Definition", fileName = "New_Catchable")]
public class CatchableDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] public string catchableName = "Fish";
    [SerializeField] public string description = "A common fish";
    [TextArea(2, 4)]
    [SerializeField] public string catchFlavor = "You caught a fish!";

    [Header("Inventory")]
    [SerializeField] public ItemDefinition inventoryItem; // The item to add when caught

    [Header("Rarity & Value")]
    [SerializeField] public CatchableRarity rarity = CatchableRarity.Common;
    [SerializeField] public int sellPrice = 50;

    [Header("Visuals")]
    [SerializeField] public Texture2D catchableIcon;
    [SerializeField] public Texture2D catchUICatchableImage; // Shown in result screen
    [SerializeField] public Color uiAccentColor = Color.cyan; // Used for UI highlights

    [Header("Fishing Difficulty")]
    [SerializeField, Range(0f, 1f)] public float difficultyScore = 0.5f; // 0=very easy, 1=very hard
    [SerializeField, Range(0.5f, 5f)] public float biteDelayMin = 1.5f; // How long until it bites
    [SerializeField, Range(0.5f, 5f)] public float biteDelayMax = 3f;

    [Header("Catch Phase Behavior")]
    [SerializeField] public FishBehaviorType behaviorType = FishBehaviorType.Standard;
    [SerializeField, Range(0f, 1f)] public float tensionIncreaseRate = 0.1f; // How fast tension climbs when player pulls
    [SerializeField, Range(0f, 1f)] public float tensionDecayRate = 0.05f; // How fast tension drops when player rests
    [SerializeField, Range(0f, 1f)] public float creaturePullStrength = 0.08f; // How hard creature resists - BALANCED

    [Header("Catch Window")]
    [SerializeField, Range(0.2f, 1f)] public float successWindow = 0.5f; // Timing window for initial reaction
    [SerializeField, Range(1f, 15f)] public float catchDurationMin = 2f; // How long catch phase lasts - SHORTER
    [SerializeField, Range(1f, 15f)] public float catchDurationMax = 4f; // SHORTER - easier to win

    [Header("Behavior Variations")]
    [SerializeField] public bool canFakeOut = false; // Fake bite before real bite
    [SerializeField, Range(0f, 1f)] public float fakeOutChance = 0.3f;
    [SerializeField] public bool canDive = false; // Creature dives/dashes mid-catch
    [SerializeField, Range(0.1f, 1f)] public float diveIntensity = 0.5f; // How much tension spike on dive

    [Header("Inventory")]
    [SerializeField] public bool stackable = true;
    [SerializeField] public int maxStackSize = 999;

    public override string ToString()
    {
        return $"{catchableName} (Rarity: {rarity}, Difficulty: {difficultyScore:P0})";
    }
}

public enum CatchableRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

public enum FishBehaviorType
{
    Standard,      // Consistent tension, normal resistance
    FastDarter,    // Quick movements, erratic tension changes
    SlowHeavy,     // Slow, strong resistance, high tension
    Elusive,       // Frequently tries to escape, loose tension
    Aggressive,    // Will dive/rush, sudden tension spikes
    Cunning        // Can fake out, tricky timing
}
