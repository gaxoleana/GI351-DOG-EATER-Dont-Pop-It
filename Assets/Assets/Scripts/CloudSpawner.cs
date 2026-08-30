using UnityEngine;

public class CloudSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private GameObject[] cloudPrefabs;

    [Header("Spawn Height Settings")]
    [SerializeField] private float spawnYDistance = 15f;    // Distance below camera to start spawning
    [SerializeField] private float spawnIntervalY = 6f;     // Gap between height levels
    [SerializeField] private float destroyYDistance = 25f;  // Distance above camera to delete

    [Header("Horizontal Cluster Settings")]
    [SerializeField] private int minCloudsPerLevel = 2;     // Min clouds spawned at same height
    [SerializeField] private int maxCloudsPerLevel = 4;     // Max clouds spawned at same height
    [SerializeField] private float minX = -10f;             // Left boundary
    [SerializeField] private float maxX = 10f;              // Right boundary

    private float nextSpawnY;

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (cameraTransform != null)
            nextSpawnY = cameraTransform.position.y - spawnYDistance;
    }

    private void Update()
    {
        if (cameraTransform == null || cloudPrefabs.Length == 0) return;

        // Spawn a level wave of clouds below camera frame as player falls
        while (cameraTransform.position.y - spawnYDistance < nextSpawnY)
        {
            SpawnCloudLevel(nextSpawnY);
            nextSpawnY -= spawnIntervalY + Random.Range(-1f, 1f);
        }

        CleanupOldClouds();
    }

    private void SpawnCloudLevel(float yPos)
    {
        int cloudCount = Random.Range(minCloudsPerLevel, maxCloudsPerLevel + 1);

        for (int i = 0; i < cloudCount; i++)
        {
            GameObject randomPrefab = cloudPrefabs[Random.Range(0, cloudPrefabs.Length)];

            float slightYOffset = Random.Range(-0.8f, 0.8f);
            float randomX = Random.Range(minX, maxX);

            Vector3 spawnPosition = new Vector3(randomX, yPos + slightYOffset, 0f);

            GameObject newCloud = Instantiate(randomPrefab, spawnPosition, Quaternion.identity);

            // Keep the original prefab scale and apply random variance
            float randomMultiplier = Random.Range(0.8f, 1.3f);
            Vector3 baseScale = randomPrefab.transform.localScale;

            newCloud.transform.localScale = new Vector3(
                (Random.value > 0.5f ? baseScale.x : -baseScale.x) * randomMultiplier,
                baseScale.y * randomMultiplier,
                baseScale.z
            );

            newCloud.transform.SetParent(transform);
        }
    }

    private void CleanupOldClouds()
    {
        foreach (Transform child in transform)
        {
            if (child.position.y > cameraTransform.position.y + destroyYDistance)
            {
                Destroy(child.gameObject);
            }
        }
    }
}