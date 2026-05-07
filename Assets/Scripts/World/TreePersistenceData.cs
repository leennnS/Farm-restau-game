using UnityEngine;

[System.Serializable]
public struct TreeDataSerializable
{
    public float positionX;
    public float positionY;
    public float positionZ;
    public string treeKey;
    public int growthStage;
    public int daysSinceLastPick;
    public int daysSinceSpriteChange;
    public bool spriteShowsFruit;
    public int daysSinceLastStageAdvance;
}

public class RuntimePlantedTree : MonoBehaviour
{
}