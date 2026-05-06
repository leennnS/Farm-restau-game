using UnityEngine;

public class CarSpawnerRight : MonoBehaviour
{
    [SerializeField] private GameObject[] carPrefabs;
    [SerializeField] private float spawnHeight = 0f;
    [SerializeField] private float spawnX = -15f;
    [SerializeField] private float spawnCooldown = 3f;
    [SerializeField] private float carMoveSpeed = 10f;
    [SerializeField] private AudioClip honkSound;

    private float spawnTimer = -1f;  // Start at -1 so it spawns immediately

    private void Start()
    {
        // Removed debug logs - spawning starts immediately
    }

    private void Update()
    {
        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            SpawnCar();
            spawnTimer = spawnCooldown;
        }
    }

    private void SpawnCar()
    {
        Debug.Log("Attempting to spawn car...");
        
        if (carPrefabs.Length == 0)
        {
            Debug.LogError("No prefabs assigned!");
            return;
        }
        
        // Randomly select one of the car prefabs
        int randomIndex = Random.Range(0, carPrefabs.Length);
        Debug.Log("Random index: " + randomIndex + " out of " + carPrefabs.Length);
        
        if (carPrefabs[randomIndex] != null)
        {
            Debug.Log("Spawning car from prefab: " + carPrefabs[randomIndex].name);
            // Instantiate the car at spawn position
            GameObject car = Instantiate(carPrefabs[randomIndex], new Vector3(spawnX, spawnHeight, 0f), Quaternion.identity);
            Debug.Log("Car spawned at: " + car.transform.position);
            
            // Add the CarRight movement script if it doesn't have one
            CarRight carScript = car.GetComponent<CarRight>();
            if (carScript == null)
            {
                carScript = car.AddComponent<CarRight>();
            }
            carScript.SetMoveSpeed(carMoveSpeed);

            CarNearMissEffects nearMiss = car.GetComponent<CarNearMissEffects>();
            if (nearMiss != null)
                nearMiss.SetHonkClip(honkSound);
        }
        else
        {
            Debug.LogError("Prefab is NULL at index " + randomIndex);
        }
    }
}
