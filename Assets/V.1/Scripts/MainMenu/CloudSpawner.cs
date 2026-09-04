using UnityEngine;

public class CloudSpawnerV1 : MonoBehaviour
{
    public GameObject[] prefabs;
    [Header("Spawn Settings")]
    public float spawnInterval = 5f;

    [Header("Movement Settings")]
    public float moveSpeed = 1f;

    [Header("Position Settings")]
    public float spawnX = -15f;
    public float despawnX = 15f;
    public float minY = -5f;
    public float maxY = 5f;

    [Header("Scale Settings")]
    public float scaleMin = 0.05f;
    public float scaleMax = 0.1f;

    void Start()
    {
        InvokeRepeating(nameof(SpawnPrefab), 0f, spawnInterval);
    }

    void SpawnPrefab()
    {
        if (prefabs.Length == 0) return;

        GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
        float y = Random.Range(minY, maxY);
        Vector3 spawnPos = new Vector3(spawnX, y, 0f);

        GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);

        float randomScale = Random.Range(scaleMin, scaleMax);
        obj.transform.localScale = new Vector3(randomScale, randomScale, 1f);

        obj.AddComponent<MoveRight>().Init(moveSpeed, despawnX);
    }
}