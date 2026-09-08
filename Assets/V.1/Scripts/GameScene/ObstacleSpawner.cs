using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// สปอว์น Obstacle ตามช่วงความสูง (Bird, Plane, Boss) โดยให้นกสุ่มฝั่งและทิศทาง
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    [Header("References")]
    public Transform playerTransform;
    public GumController gum;

    [Header("Prefabs")]
    public GameObject warningLinePrefab;
    public GameObject warningLinePlanePrefab;
    public GameObject cautionLeftPrefab;
    public GameObject cautionRightPrefab;
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
    public float cautionBlinkInterval = 0.15f;

    [Tooltip("ระยะ Offset Y ต่ำสุด (ตั้งเป็น 0 = ระดับเดียวกับตัวละคร)")]
    public float minOffsetY = 0f;

    [Tooltip("ระยะ Offset Y สูงสุด (ตั้งเป็นค่าบวก = เกิดเหนือหัวตัวละคร)")]
    public float maxOffsetY = 4f;

    [Tooltip("พิกัด X นอกจอฝั่งขวาสำหรับสปอว์น")]
    public float spawnXRight = 12f;

    [Header("Caution Position")]
    public float cautionXRight = 4f;
    public float cautionXLeft = -4f;

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

        if (isBossPhase)
        {
            isBossPhase = false;
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

        // 2. สุ่มระยะ Offset Y และเลือกฝั่งเกิดก่อนสร้างสัญญาณเตือน
        // สุ่มระยะ Offset Y จาก minOffsetY ถึง maxOffsetY
        float offsetY = Random.Range(minOffsetY, maxOffsetY);
        bool isBird = selectedPrefab == birdPrefab;
        bool spawnFromRight = !isBird || Random.value < 0.5f;
        float spawnX = spawnFromRight ? spawnXRight : -spawnXRight;

        // 3. สร้างเส้น Warning Line ตามชนิดของ Obstacle
        GameObject warningLine = null;
        float finalSpawnY = playerTransform.position.y + offsetY;
        GameObject warningPrefab = isBird ? warningLinePrefab : warningLinePlanePrefab;
        if (warningPrefab != null)
        {
            Vector3 initialPos = new Vector3(0f, finalSpawnY, 0f);
            warningLine = Instantiate(warningPrefab, initialPos, Quaternion.identity);
        }

        GameObject cautionPrefab = spawnFromRight ? cautionRightPrefab : cautionLeftPrefab;
        GameObject caution = null;
        if (cautionPrefab != null)
        {
            float cautionX = spawnFromRight ? cautionXRight : cautionXLeft;
            caution = Instantiate(cautionPrefab, new Vector3(cautionX, finalSpawnY, 0f), Quaternion.identity, transform);
            caution.SetActive(false);
        }

        // 4. รอช่วงเวลาเตือน พร้อมกระพริบ Caution ฝั่งเดียวกับจุดเกิด
        float timer = 0f;
        float blinkTimer = 0f;
        bool cautionVisible = false;
        while (timer < warningDuration)
        {
            timer += Time.deltaTime;
            blinkTimer += Time.deltaTime;
            if (caution != null && blinkTimer >= cautionBlinkInterval)
            {
                blinkTimer = 0f;
                cautionVisible = !cautionVisible;
                caution.SetActive(cautionVisible);
            }
            yield return null; // รอ Frame ถัดไป
        }

        // 5. ลบเส้นเตือน โดยใช้ตำแหน่งเดิมเป็นจุดเกิด Obstacle
        if (warningLine != null)
        {
            Destroy(warningLine);
        }
        if (caution != null)
        {
            Destroy(caution);
        }

        // 6. สปอว์น Obstacle จริง โดยให้นกสุ่มฝั่งและทิศทางทุกครั้งที่เกิด
        if (selectedPrefab != null && gum != null && gum.currentState == GumController.GumState.Normal)
        {
            Vector2 moveDirection = spawnFromRight ? Vector2.left : Vector2.right;

            Vector3 spawnPos = new Vector3(spawnX, finalSpawnY, 0f);
            GameObject obsObj = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);

            Obstacle obs = obsObj.GetComponent<Obstacle>();
            if (obs != null)
            {
                obs.moveDirection = moveDirection;
            }

            if (isBird)
            {
                SpriteRenderer birdRenderer = obsObj.GetComponent<SpriteRenderer>();
                if (birdRenderer != null)
                {
                    birdRenderer.flipX = !spawnFromRight;
                }
            }
        }
    }
}