using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] carPrefabs;
    [SerializeField] private float spawnHeight = 0f;
    [SerializeField] private float spawnX = 15f;
    [SerializeField] private float spawnCooldown = 3f;
    [SerializeField] private float carMoveSpeed = 10f;
    [SerializeField] private AudioClip honkSound;

    private float spawnTimer = -1f;  // Start at -1 so it spawns immediately

    private void Start()
    {
        // Removed - we want spawning to start immediately, not after cooldown
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
        // Randomly select one of the car prefabs
        if (carPrefabs.Length == 0)
        {
            return;
        }
        
        int randomIndex = Random.Range(0, carPrefabs.Length);
        
        if (carPrefabs[randomIndex] != null)
        {
            // Instantiate the car at spawn position
            GameObject car = Instantiate(carPrefabs[randomIndex], new Vector3(spawnX, spawnHeight, 0f), Quaternion.identity);
            
            // Add the Car movement script if it doesn't have one
            Car carScript = car.GetComponent<Car>();
            if (carScript == null)
            {
                carScript = car.AddComponent<Car>();
            }
            carScript.SetMoveSpeed(carMoveSpeed);

            CarNearMissEffects nearMiss = car.GetComponent<CarNearMissEffects>();
            if (nearMiss != null)
                nearMiss.SetHonkClip(honkSound);
        }
    }
}
