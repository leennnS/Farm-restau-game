using UnityEngine;

public class RuntimeFarmHarvestPickup : MonoBehaviour
{
}

[System.Serializable]
public struct HarvestPickupDataSerializable
{
    public float positionX;
    public float positionY;
    public float positionZ;
    public string itemKey;
    public int count;
    public float ttlRemaining;
}