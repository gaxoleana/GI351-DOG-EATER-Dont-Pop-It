using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// สปอว์น Obstacle จากขวาไปซ้ายอย่างเดียว แยกชนิดตามช่วงความสูง (Bird, Plane, Boss)
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    [Header("References")]
    public Transform playerTransform;
    public GumController gum;

    [Header("Prefabs")]
    public GameObject warningLinePrefab;
    public GameObject birdPrefab;
    public GameObject planePrefab;

    [Header("Boss")]
    [Tooltip("Prefab ของ AlienBoss (ต้องมี component AlienBoss ติดอยู่) — จะ spawn ครั้งเดียวตอนเข้า bossAltitude")]
    public GameObject alienBossPrefab;

    [Tooltip("ตำแหน่ง spawn ของ Alien Boss (ถ้าไม่ใส่ จะ spawn ที่ตำแหน่งผู้เล่นบวก offset เริ่มต้นของตัว Alien เอง)")]
    public Transform bossSpawnPoint;

    [Header("Altitude Thresholds")]
    public float birdMinAltitude = 100f;
    public float planeMinAltitude = 1000f;
    public float bossAltitude = 2000f;

    [Header("Interval Scaling")]
    public float maxSpawnInterval = 5f;
    public float minSpawnInterval = 1.2f;

    [Header("Warning & Spawn Position")]
    public float warningDuration = 1.0f;

    [Tooltip("ระยะ Offset Y ต่ำสุด (ตั้งเป็น 0 = ระดับเดียวกับตัวละคร)")]
    public float minOffsetY = 0f;

    [Tooltip("ระยะ Offset Y สูงสุด (ตั้งเป็นค่าบวก = เกิดเหนือหัวตัวละคร)")]
    public float maxOffsetY = 4f;

    [Tooltip("พิกัด X นอกจอฝั่งขวาสำหรับสปอว์น")]
    public float spawnXRight = 12f;

    [Header("Runtime Status")]
    public bool isBossPhase = false;

    private float nextSpawnTimer;

    void Start()
    {
        if (playerTransform == null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null) playerTransform = player.transform;
        }

        if (gum == null) gum = FindAnyObjectByType<GumController>();

        ResetSpawnTimer();
    }

    void Update()
    {
        if (playerTransform == null || gum == null) return;
        if (gum.currentState != GumController.GumState.Normal) return;

        float currentAltitude = Mathf.Max(0f, playerTransform.position.y);

        // 1. เช็กเข้าสู่ช่วง Boss Fight หลัง 2000m (หยุดการสปอว์นอุปสรรคปกติ)
        if (currentAltitude >= bossAltitude)
        {
            if (!isBossPhase)
            {
                isBossPhase = true;
                Debug.Log("⚠️ เข้าสู่พื้นที่ Alien Boss Fight (2000m+)");
                SpawnAlienBoss();
            }
            return;
        }

        // 2. ลูปสปอว์นอุปสรรคปกติ (เริ่มทำงานตั้งแต่ 100m ขึ้นไป)
        if (currentAltitude >= birdMinAltitude)
        {
            nextSpawnTimer -= Time.deltaTime;
            if (nextSpawnTimer <= 0f)
            {
                StartCoroutine(SpawnObstacleSequence(currentAltitude));
                ResetSpawnTimer();
            }
        }
    }

    private void ResetSpawnTimer()
    {
        float currentAltitude = playerTransform != null ? Mathf.Max(0f, playerTransform.position.y) : 0f;
        float progressRatio = Mathf.Clamp01((currentAltitude - birdMinAltitude) / (bossAltitude - birdMinAltitude));

        float currentInterval = Mathf.Lerp(maxSpawnInterval, minSpawnInterval, progressRatio);
        nextSpawnTimer = currentInterval;
    }

    /// <summary>
    /// สปอว์น Alien Boss ครั้งเดียวตอนเข้า bossAltitude — ตัวมันเองจะไล่ตามผู้เล่นเองผ่าน AlienBoss.cs
    /// ไม่ต้องมี logic ดูแลต่อจากตรงนี้อีก
    /// </summary>
    private void SpawnAlienBoss()
    {
        if (alienBossPrefab == null)
        {
            Debug.LogWarning("[ObstacleSpawner] ยังไม่ได้ใส่ Alien Boss Prefab");
            return;
        }

        Vector3 spawnPos = bossSpawnPoint != null
            ? bossSpawnPoint.position
            : playerTransform.position; // AlienBoss จะขยับตัวเองไปตำแหน่งที่ถูกต้องในเฟรมแรกอยู่แล้ว

        GameObject bossObj = Instantiate(alienBossPrefab, spawnPos, Quaternion.identity);

        AlienBoss boss = bossObj.GetComponent<AlienBoss>();
        if (boss != null && boss.playerTransform == null)
        {
            boss.playerTransform = playerTransform;
        }
    }

    private IEnumerator SpawnObstacleSequence(float currentAltitude)
    {
        // 1. คัดเลือกชนิด Prefab ตามช่วงความสูง
        List<GameObject> availablePrefabs = new List<GameObject>();

        if (currentAltitude >= birdMinAltitude && currentAltitude < bossAltitude)
        {
            availablePrefabs.Add(birdPrefab);
        }
        if (currentAltitude >= planeMinAltitude && currentAltitude < bossAltitude)
        {
            availablePrefabs.Add(planePrefab);
        }

        if (availablePrefabs.Count == 0) yield break;

        GameObject selectedPrefab = availablePrefabs[Random.Range(0, availablePrefabs.Count)];

        // 2. สุ่มระยะ Offset Y สัมพันธ์กับตัวละคร (เช่น สุ่มเกิดช่วงเหนือหัว หรือระดับตัว)
        // สุ่มระยะ Offset Y จาก minOffsetY ถึง maxOffsetY
        float offsetY = Random.Range(minOffsetY, maxOffsetY);

        // 3. สร้างเส้น Warning Line ณ ตำแหน่งเริ่มต้น
        GameObject warningLine = null;
        if (warningLinePrefab != null)
        {
            Vector3 initialPos = new Vector3(0f, playerTransform.position.y + offsetY, 0f);
            warningLine = Instantiate(warningLinePrefab, initialPos, Quaternion.identity);
        }

        // 4. วนลูปให้เส้น Warning วิ่งตามตำแหน่ง Y ของ Player ตลอดช่วง warningDuration
        float timer = 0f;
        while (timer < warningDuration)
        {
            timer += Time.deltaTime;

            if (warningLine != null && playerTransform != null)
            {
                // อัปเดตพิกัด Y ให้ขยับตาม Player Real-time
                Vector3 trackedPos = warningLine.transform.position;
                trackedPos.y = playerTransform.position.y + offsetY;
                warningLine.transform.position = trackedPos;
            }

            yield return null; // รอ Frame ถัดไป
        }

        // 5. บันทึกตำแหน่ง Y ล่าสุด ณ จังหวะหมดเวลาเตือน แล้วทำลายเส้นเตือนทิ้ง
        float finalSpawnY = playerTransform.position.y + offsetY;
        if (warningLine != null)
        {
            finalSpawnY = warningLine.transform.position.y; // ล็อกค่า Y ล่าสุดของเส้นเตือน
            Destroy(warningLine);
        }

        // 6. สปอว์น Obstacle จริง ณ พิกัด finalSpawnY แล้วปล่อยวิ่งจากขวาไปซ้ายตามปกติ
        if (selectedPrefab != null && gum != null && gum.currentState == GumController.GumState.Normal)
        {
            Vector3 spawnPos = new Vector3(spawnXRight, finalSpawnY, 0f);
            GameObject obsObj = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);

            Obstacle obs = obsObj.GetComponent<Obstacle>();
            if (obs != null)
            {
                obs.moveDirection = Vector2.left; // วิ่งตามแนวราบปกติ
            }

            SpriteRenderer sr = obsObj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.flipX = true;
            }
        }
    }
}