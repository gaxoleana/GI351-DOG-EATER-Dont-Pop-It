using UnityEngine;
using System.Collections;

/// <summary>
/// Alien Boss — อยู่ติดขอบขวาของจอตลอดเวลา ลอยตามแกน Y ของผู้เล่น แล้วยิงเลเซอร์
/// </summary>
public class AlienBoss : MonoBehaviour
{
    private enum AttackType
    {
        Normal,
        Double,
        Ultimate
    }

    [Header("References")]
    [Tooltip("Transform ของผู้เล่น ใช้คำนวณตำแหน่งที่จะไล่ตาม")]
    public Transform playerTransform;

    [Header("Position Settings")]
    [SerializeField] private float fixedRightX = 4.0f; // ระยะ X ทางขวาที่ต้องการให้ Alien ยืนตำแหน่ง
    [SerializeField] private float followSmoothTimeY = 0.25f; // ความนุ่มนวลในการตามแกน Y
    public float offsetY = 0f; // Offset แนวตั้งเพิ่มเติม

    [Header("Idle Bob (ลอยขึ้นลงเบา ๆ)")]
    public bool enableBob = true;
    public float bobAmplitude = 0.3f;
    public float bobSpeed = 2f;

    [Header("Laser Attack")]
    public GameObject laserWarningLinePrefab;
    public GameObject laserBeamPrefab;

    [Tooltip("ช่วงเวลาต่ำสุด-สูงสุดระหว่างการยิงเลเซอร์แต่ละครั้ง (วินาที)")]
    public float minLaserInterval = 2f;
    public float maxLaserInterval = 4f;

    [Tooltip("Phase 1: ระยะเวลาที่เส้นเตือนไล่ตามผู้เล่น real-time (วินาที)")]
    public float laserTrackDuration = 1.0f;

    [Tooltip("Phase 2: หลังจบ track แล้วเส้นเตือนจะหยุดนิ่งล็อกตำแหน่งไว้กี่วินาที ก่อนเลเซอร์ยิง")]
    public float laserHoldDuration = 0.8f;

    [Tooltip("ระยะเวลาที่เลเซอร์ (collider จริง) ค้างอยู่บนจอก่อนจะ หายไป (วินาที) — ขยายเวลาเพิ่มขึ้นตรงนี้")]
    public float laserActiveDuration = 0.8f;

    [Tooltip("จำนวนเลเซอร์ที่ยิงติดกันในช่วงอันติ")]
    public int ultimateLaserCount = 3;

    [Tooltip("ช่วงห่างระหว่างเลเซอร์แต่ละนัดในช่วงอันติ (วินาที)")]
    public float ultimateLaserInterval = 0.25f;

    [Tooltip("offset แนวตั้งต่ำสุดจากตำแหน่งผู้เล่น")]
    public float minAttackOffsetY = -1f;

    [Tooltip("offset แนวตั้งสูงสุดจากตำแหน่งผู้เล่น")]
    public float maxAttackOffsetY = 1f;

    private float yVelocity;
    private float bobTimer;
    private float nextLaserTimer;
    private bool isAttacking;

    void Start()
    {
        if (playerTransform == null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null) playerTransform = player.transform;
        }

        ScheduleNextLaser();
    }

    void Update()
    {
        if (playerTransform == null) return;

        // คงเหลือเฉพาะระบบจับเวลาเลเซอร์ไว้ใน Update
        HandleLaserTimer();
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;

        // ขยับตาม Player ใน LateUpdate หลังกล้องและ Player สรุปตำแหน่งเฟรมนี้แล้ว
        if (!isAttacking)
        {
            FollowPlayer();
        }
    }

    private void FollowPlayer()
    {
        if (playerTransform == null || isAttacking) return;

        float bobOffset = 0f;
        if (enableBob)
        {
            bobTimer += Time.deltaTime * bobSpeed;
            bobOffset = Mathf.Sin(bobTimer) * bobAmplitude;
        }

        // คำนวณความสูงเป้าหมายแกน Y
        float targetY = playerTransform.position.y + offsetY + bobOffset;

        // ขยับเฉพาะแกน Y ด้วย SmoothDamp
        float currentY = Mathf.SmoothDamp(transform.position.y, targetY, ref yVelocity, followSmoothTimeY);

        // ตรึงแกน X ไว้ทางขวา (fixedRightX) ตลอดเวลา
        transform.position = new Vector3(fixedRightX, currentY, transform.position.z);
    }

    private void HandleLaserTimer()
    {
        if (isAttacking) return;

        nextLaserTimer -= Time.deltaTime;
        if (nextLaserTimer <= 0f)
        {
            switch (GetRandomAttackType())
            {
                case AttackType.Normal:
                    StartCoroutine(FireStandardLaserSequence(1));
                    break;
                case AttackType.Double:
                    StartCoroutine(FireStandardLaserSequence(2));
                    break;
                case AttackType.Ultimate:
                    StartCoroutine(FireUltimateLaserSequence());
                    break;
            }

            ScheduleNextLaser();
        }
    }

    private AttackType GetRandomAttackType()
    {
        return (AttackType)Random.Range(0, 3);
    }

    private void ScheduleNextLaser()
    {
        nextLaserTimer = Random.Range(minLaserInterval, maxLaserInterval);
    }

    private IEnumerator FireStandardLaserSequence(int laserCount)
    {
        if (playerTransform == null) yield break;

        isAttacking = true;

        float fixedX = fixedRightX;
        Vector3 startPos = new Vector3(fixedX, transform.position.y, transform.position.z);
        float[] attackYs = new float[laserCount];
        GameObject[] warningLines = new GameObject[laserCount];

        for (int index = 0; index < laserCount; index++)
        {
            attackYs[index] = playerTransform.position.y + Random.Range(minAttackOffsetY, maxAttackOffsetY);
            if (laserWarningLinePrefab != null)
            {
                warningLines[index] = Instantiate(laserWarningLinePrefab, startPos, Quaternion.identity);
            }
        }

        // ล็อกเป้าหมายตามเวลาที่กำหนดก่อนยิง
        float trackedY = startPos.y;
        float timer = 0f;
        while (timer < laserTrackDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / laserTrackDuration);

            float targetY = playerTransform.position.y;
            trackedY = Mathf.Lerp(startPos.y, targetY, progress);

            Vector3 pos = new Vector3(fixedX, trackedY, transform.position.z);
            transform.position = pos;

            for (int index = 0; index < laserCount; index++)
            {
                if (warningLines[index] != null)
                {
                    Vector3 warningPosition = new Vector3(fixedX, attackYs[index], transform.position.z);
                    warningLines[index].transform.position = warningPosition;
                }
            }

            yield return null;
        }

        yield return new WaitForSeconds(laserHoldDuration);

        for (int index = 0; index < laserCount; index++)
        {
            if (warningLines[index] != null)
            {
                Destroy(warningLines[index]);
            }

            FreezeLaserBeam(fixedX, attackYs[index]);
        }

        // รีเซ็ตความเร็ว Y ไม่ให้ Alien กระตุกตอนกลับเข้าสภาวะขยับปกติ
        yVelocity = 0f;
        isAttacking = false;
    }

    private IEnumerator FireUltimateLaserSequence()
    {
        if (playerTransform == null) yield break;

        isAttacking = true;

        for (int shot = 0; shot < ultimateLaserCount; shot++)
        {
            float attackOffsetY = Random.Range(minAttackOffsetY, maxAttackOffsetY);
            float targetY = playerTransform.position.y + attackOffsetY;

            // อันติยิงทันทีโดยไม่มี warning line หรือช่วงล็อกเป้า
            FreezeLaserBeam(fixedRightX, targetY);

            if (shot < ultimateLaserCount - 1)
            {
                yield return new WaitForSeconds(ultimateLaserInterval);
            }
        }

        isAttacking = false;
    }

    private void FreezeLaserBeam(float x, float y)
    {
        if (laserBeamPrefab == null)
        {
            Debug.LogWarning("[AlienBoss] ยังไม่ได้ใส่ Laser Beam Prefab");
            return;
        }

        GameObject beam = Instantiate(laserBeamPrefab, new Vector3(x, y, 0f), Quaternion.identity);

        Obstacle obs = beam.GetComponent<Obstacle>();
        if (obs != null)
        {
            obs.moveDirection = Vector2.zero;
            obs.speed = 0f;
            obs.lifeTime = laserActiveDuration; // ค้างเลเซอร์ไว้ตามระยะเวลาที่ตั้งไว้
        }
    }
}