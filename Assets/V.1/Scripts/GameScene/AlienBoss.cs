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

    [Tooltip("เวลาที่ Alien ใช้เล็งระดับใหม่ของแต่ละนัดในช่วงอันติ")]
    public float ultimateTrackDuration = 0.2f;

    [Tooltip("เวลาล็อกระดับก่อนยิงแต่ละนัดในช่วงอันติ")]
    public float ultimateHoldDuration = 0.05f;

    [Tooltip("ช่วงห่างระหว่างเลเซอร์แต่ละนัดในช่วงอันติ (วินาที)")]
    public float ultimateLaserInterval = 0.1f;

    [Header("Ultimate Warning")]
    [Tooltip("Prefab/child Angry ที่จะกระพริบก่อนเริ่ม Ultimate")]
    public Transform angryWarningTransform;

    [Tooltip("ระยะเวลาเตือนก่อนยิง Ultimate")]
    public float ultimateWarningDuration = 1f;

    [Tooltip("ช่วงเวลาสลับเปิด/ปิด Angry ระหว่างกระพริบ")]
    public float ultimateWarningBlinkInterval = 0.15f;

    [Tooltip("ระยะสุ่มแนวตั้งต่ำสุดจากตำแหน่งผู้เล่น")]
    public float minAttackOffsetY = -4f;

    [Tooltip("ระยะสุ่มแนวตั้งสูงสุดจากตำแหน่งผู้เล่น")]
    public float maxAttackOffsetY = 12f;

    [Tooltip("ความเร็วที่ Alien เคลื่อนเข้าหาระดับโจมตี (หน่วยต่อวินาที)")]
    public float attackMoveSpeed = 8f;

    [Tooltip("ความสูงต่ำสุดของ Player ก่อน Alien จะถูกทำลาย โดย 100 world units = 1 km")]
    public float despawnAltitude = 2000f;

    private float yVelocity;
    private float bobTimer;
    private float nextLaserTimer;
    private bool isAttacking;
    private bool isUltimateWarning;

    void Start()
    {
        if (playerTransform == null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null) playerTransform = player.transform;
        }

        SetAngryWarning(false);
        ScheduleNextLaser();
    }

    void Update()
    {
        if (playerTransform == null) return;

        if (playerTransform.position.y < despawnAltitude)
        {
            Destroy(gameObject);
            return;
        }

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
        if (isAttacking || isUltimateWarning) return;

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

        float attackOffsetY = Random.Range(minAttackOffsetY, maxAttackOffsetY);
        float fixedX = fixedRightX;
        Vector3 startPos = new Vector3(fixedX, transform.position.y, transform.position.z);
        float trackedY = startPos.y;
        GameObject warningLine = null;

        if (laserWarningLinePrefab != null)
        {
            warningLine = Instantiate(laserWarningLinePrefab, startPos, Quaternion.identity);
        }

        // Match V0: track the player's predicted attack height during the warning phase.
        float timer = 0f;
        while (timer < laserTrackDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / laserTrackDuration);
            float targetY = playerTransform.position.y + attackOffsetY;
            trackedY = Mathf.Lerp(startPos.y, targetY, progress);

            Vector3 position = new Vector3(fixedX, trackedY, transform.position.z);
            transform.position = position;

            if (warningLine != null)
            {
                warningLine.transform.position = position;
            }

            yield return null;
        }

        Vector3 lockedPosition = new Vector3(fixedX, trackedY, transform.position.z);
        transform.position = lockedPosition;
        if (warningLine != null)
        {
            warningLine.transform.position = lockedPosition;
        }

        yield return new WaitForSeconds(laserHoldDuration);

        if (warningLine != null)
        {
            Destroy(warningLine);
        }

        FreezeLaserBeam(fixedX, trackedY);

        // รีเซ็ตความเร็ว Y ไม่ให้ Alien กระตุกตอนกลับเข้าสภาวะขยับปกติ
        yVelocity = 0f;
        isAttacking = false;
    }

    private IEnumerator FireUltimateLaserSequence()
    {
        if (playerTransform == null) yield break;

        yield return StartCoroutine(PlayUltimateWarning());

        isAttacking = true;

        float fixedX = fixedRightX;
        for (int shot = 0; shot < ultimateLaserCount; shot++)
        {
            float attackOffsetY = Random.Range(minAttackOffsetY, maxAttackOffsetY);
            float targetY = playerTransform.position.y + attackOffsetY;
            float startY = transform.position.y;
            float trackedY = startY;
            float timer = 0f;

            // Each shot gets its own locked target height. No warning line is shown.
            while (timer < ultimateTrackDuration)
            {
                timer += Time.deltaTime;
                float progress = Mathf.Clamp01(timer / ultimateTrackDuration);
                trackedY = Mathf.Lerp(startY, targetY, progress);
                transform.position = new Vector3(fixedX, trackedY, transform.position.z);
                yield return null;
            }

            transform.position = new Vector3(fixedX, targetY, transform.position.z);
            yield return new WaitForSeconds(ultimateHoldDuration);

            FreezeLaserBeam(fixedX, targetY);

            if (shot < ultimateLaserCount - 1)
            {
                yield return new WaitForSeconds(ultimateLaserInterval);
            }
        }

        yVelocity = 0f;
        isAttacking = false;
    }

    private IEnumerator PlayUltimateWarning()
    {
        if (angryWarningTransform == null || ultimateWarningDuration <= 0f)
        {
            SetAngryWarning(false);
            isUltimateWarning = false;
            yield break;
        }

        isUltimateWarning = true;
        float elapsed = 0f;
        float blinkInterval = Mathf.Max(0.01f, ultimateWarningBlinkInterval);
        bool isVisible = false;

        while (elapsed < ultimateWarningDuration)
        {
            isVisible = !isVisible;
            SetAngryWarning(isVisible);

            float waitTime = Mathf.Min(blinkInterval, ultimateWarningDuration - elapsed);
            yield return new WaitForSeconds(waitTime);
            elapsed += waitTime;
        }

        SetAngryWarning(false);
        isUltimateWarning = false;
    }

    private void SetAngryWarning(bool visible)
    {
        if (angryWarningTransform != null)
        {
            angryWarningTransform.gameObject.SetActive(visible);
        }
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

        NeonLaserFadeEffect fadeEffect = beam.AddComponent<NeonLaserFadeEffect>();
        fadeEffect.fadeDuration = laserActiveDuration;
    }
}