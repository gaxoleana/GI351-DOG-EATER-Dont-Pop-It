using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawn เมฆ (background prop) แบบสุ่ม เริ่มทำงานหลังผู้เล่นขึ้นถึง startAltitude
/// เมฆจะ spawn อยู่เหนือ view ปัจจุบัน แล้วถูกลบทิ้งอัตโนมัติเมื่อตกไปอยู่หลังผู้เล่นไกลเกินไป
/// </summary>
public class CloudSpawnerInGame : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Transform ของ player ใช้คำนวณความสูงและตำแหน่ง spawn")]
    public Transform playerTransform;

    [Header("Activation")]
    [Tooltip("เริ่ม spawn เมฆหลังผู้เล่นขึ้นถึงความสูงเท่านี้ (เมตร)")]
    public float startAltitude = 400f;

    [Header("Prefabs")]
    [Tooltip("เมฆหลายแบบ จะสุ่มเลือกทุกครั้งที่ spawn")]
    public GameObject[] cloudPrefabs;

    [Header("Spawn Timing")]
    [Tooltip("ช่วงเวลาต่ำสุด-สูงสุดระหว่างการ spawn แต่ละครั้ง (วินาที)")]
    public float spawnIntervalMin = 1.5f;
    public float spawnIntervalMax = 3f;

    [Header("Spawn Position")]
    [Tooltip("ระยะ X ซ้าย-ขวา เทียบกับตำแหน่ง X ปัจจุบันของผู้เล่น")]
    public float spawnXRangeMin = -6f;
    public float spawnXRangeMax = 6f;

    [Tooltip("spawn ล่วงหน้าเหนือผู้เล่นกี่หน่วย (ควรมากกว่าขอบจอบนเล็กน้อย กันเห็น pop-in)")]
    public float spawnAheadDistance = 12f;

    [Header("Cleanup")]
    [Tooltip("ลบเมฆทิ้งเมื่อตกไปอยู่หลังผู้เล่นไกลเกินระยะนี้ (หน่วยเดียวกับโลกเกม)")]
    public float despawnBehindDistance = 15f;

    [Header("Optional Drift")]
    [Tooltip("ให้เมฆลอยเลื่อนแนวนอนช้า ๆ เพิ่ม feel ลมพัด (0 = นิ่ง)")]
    public float driftSpeed = 0.3f;

    [Header("Random Scale")]
    [Tooltip("สุ่ม scale ของเมฆแต่ละก้อน ให้ดูมีระยะใกล้-ไกล ไม่ซ้ำกันหมด")]
    public bool randomizeScale = true;

    [Tooltip("scale ต่ำสุด")]
    public float minScale = 0.7f;

    [Tooltip("scale สูงสุด")]
    public float maxScale = 1.4f;

    private float startY;
    private float nextSpawnTimer;
    private readonly List<GameObject> activeClouds = new List<GameObject>();

    void Start()
    {
        if (playerTransform != null)
        {
            startY = playerTransform.position.y;
        }
        else
        {
            Debug.LogWarning("[CloudSpawner] ยังไม่ได้ผูก Player Transform ใน Inspector");
        }

        ScheduleNextSpawn();
    }

    void Update()
    {
        if (playerTransform == null) return;

        float altitude = GetCurrentAltitude();
        if (altitude < startAltitude) return; // ยังไม่ถึงจุดเริ่ม spawn

        nextSpawnTimer -= Time.deltaTime;
        if (nextSpawnTimer <= 0f)
        {
            SpawnCloud();
            ScheduleNextSpawn();
        }

        CleanupClouds();
    }

    private float GetCurrentAltitude()
    {
        return Mathf.Max(0f, playerTransform.position.y - startY);
    }

    private void ScheduleNextSpawn()
    {
        nextSpawnTimer = Random.Range(spawnIntervalMin, spawnIntervalMax);
    }

    private void SpawnCloud()
    {
        if (cloudPrefabs == null || cloudPrefabs.Length == 0)
        {
            Debug.LogWarning("[CloudSpawner] ยังไม่ได้ใส่ Cloud Prefabs");
            return;
        }

        GameObject prefab = cloudPrefabs[Random.Range(0, cloudPrefabs.Length)];

        float spawnX = playerTransform.position.x + Random.Range(spawnXRangeMin, spawnXRangeMax);
        float spawnY = playerTransform.position.y + spawnAheadDistance;

        GameObject cloud = Instantiate(prefab, new Vector3(spawnX, spawnY, 0f), Quaternion.identity, transform);

        if (randomizeScale)
        {
            float scale = Random.Range(minScale, maxScale);
            cloud.transform.localScale = prefab.transform.localScale * scale;
        }

        // ทิศทาง drift สุ่มซ้ายหรือขวา ให้ดูเป็นธรรมชาติ ไม่ล่องลอยทางเดียวกันหมด
        if (driftSpeed > 0f)
        {
            CloudDrift drift = cloud.AddComponent<CloudDrift>();
            drift.speed = driftSpeed * (Random.value < 0.5f ? 1f : -1f);
        }

        activeClouds.Add(cloud);
    }

    private void CleanupClouds()
    {
        float cutoffY = playerTransform.position.y - despawnBehindDistance;

        for (int i = activeClouds.Count - 1; i >= 0; i--)
        {
            GameObject cloud = activeClouds[i];

            if (cloud == null)
            {
                activeClouds.RemoveAt(i);
                continue;
            }

            if (cloud.transform.position.y < cutoffY)
            {
                Destroy(cloud);
                activeClouds.RemoveAt(i);
            }
        }
    }
}

/// <summary>
/// component เล็ก ๆ ติดกับเมฆแต่ละก้อนเพื่อให้ลอยเลื่อนแนวนอนช้า ๆ
/// เพิ่มเข้าไปอัตโนมัติจาก CloudSpawner เมื่อ driftSpeed > 0
/// </summary>
public class CloudDrift : MonoBehaviour
{
    public float speed = 0.3f;

    void Update()
    {
        transform.position += Vector3.right * speed * Time.deltaTime;
    }
}