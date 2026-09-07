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

    [Tooltip("ระยะสุ่มแนวตั้งต่ำสุดจากตำแหน่งผู้เล่น")]
    public float minAttackOffsetY = -4f;

    [Tooltip("ระยะสุ่มแนวตั้งสูงสุดจากตำแหน่งผู้เล่น")]
    public float maxAttackOffsetY = 12f;

    [Tooltip("ความเร็วที่ Alien เคลื่อนเข้าหาระดับโจมตี (หน่วยต่อวินาที)")]
    public float attackMoveSpeed = 8f;

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
                    StartCoroutine(FireStandardLaserSequence());
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
        return Random.value < 0.25f ? AttackType.Ultimate : AttackType.Normal;
    }

    private void ScheduleNextLaser()
    {
        nextLaserTimer = Random.Range(minLaserInterval, maxLaserInterval);
    }

    private IEnumerator FireStandardLaserSequence()
    {
        if (playerTransform == null) yield break;

        isAttacking = true;

        float fixedX = fixedRightX;
        Vector3 startPos = new Vector3(fixedX, transform.position.y, transform.position.z);
        float attackOffsetY = Random.Range(minAttackOffsetY, maxAttackOffsetY);
        float attackY = transform.position.y;
        GameObject warningLine = null;

        if (laserWarningLinePrefab != null)
        {
            warningLine = Instantiate(laserWarningLinePrefab, startPos, Quaternion.identity);
        }

        // Alien ไล่ตาม Player + offset แบบค่อย ๆ ขยับ และเส้นเตือนอยู่ระดับเดียวกับ Alien เสมอ
        float timer = 0f;
        while (timer < laserTrackDuration)
        {
            timer += Time.deltaTime;
            attackY = playerTransform.position.y + attackOffsetY;
            MoveAlienToAttackHeight(fixedX, attackY);

            if (warningLine != null)
            {
                warningLine.transform.position = transform.position;
            }

            yield return null;
        }

        // ล็อกเป้าหมายสุดท้าย แล้วให้ Alien กับเส้นเตือนหยุดระดับเดียวกันก่อนยิง
        attackY = playerTransform.position.y + attackOffsetY;

        while (!Mathf.Approximately(transform.position.y, attackY))
        {
            MoveAlienToAttackHeight(fixedX, attackY);

            if (warningLine != null)
            {
                warningLine.transform.position = transform.position;
            }

            yield return null;
        }

        attackY = transform.position.y;
        if (warningLine != null)
        {
            warningLine.transform.position = transform.position;
        }

        yield return new WaitForSeconds(laserHoldDuration);

        if (warningLine != null)
        {
            Destroy(warningLine);
        }

        FreezeLaserBeam(fixedX, attackY);

        // รีเซ็ตความเร็ว Y ไม่ให้ Alien กระตุกตอนกลับเข้าสภาวะขยับปกติ
        yVelocity = 0f;
        isAttacking = false;
    }

    private void MoveAlienToAttackHeight(float x, float targetY)
    {
        float nextY = Mathf.MoveTowards(transform.position.y, targetY, attackMoveSpeed * Time.deltaTime);
        transform.position = new Vector3(x, nextY, transform.position.z);
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