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
    public float laserTrackDuration = 0.5f;

    [Tooltip("Phase 2: หลังจบ track แล้วเส้นเตือนจะหยุดนิ่งล็อกตำแหน่งไว้กี่วินาที ก่อนเลเซอร์ยิง")]
    public float laserHoldDuration = 0.3f;

    [Tooltip("ระยะเวลาที่เลเซอร์ (collider จริง) ค้างอยู่บนจอก่อนจะ หายไป (วินาที) — ขยายเวลาเพิ่มขึ้นตรงนี้")]
    public float laserActiveDuration = 0.8f;

    [Tooltip("จำนวนเลเซอร์ที่ยิงติดกันในช่วงอันติ")]
    public int ultimateLaserCount = 3;

    [Tooltip("ระยะสุ่มแนวยิงเหนือ/ใต้ตำแหน่ง Alien ตอนเริ่มยิงสามนัด")]
    public float ultimateSpreadY = 2f;

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

    [Header("Brainwave")]
    public float minBrainwaveDuration = 1f;
    public float maxBrainwaveDuration = 2.5f;
    public float minBrainwaveCooldown = 12f;
    public float maxBrainwaveCooldown = 24f;
    private float brainwaveCooldown;
    private PanicEventManagerVariantNoStop brainwaveManager;
    private SpriteRenderer alienRenderer, angryRenderer;
    private Sprite redAngrySprite, greenAngrySprite;
    private Texture2D greenAngryTexture;
    private bool brainwaveCasting;
    private readonly System.Collections.Generic.List<GameObject> activeLaserBeams = new System.Collections.Generic.List<GameObject>();
    private bool BrainwaveBlocksAttacks => brainwaveCasting ||
        (brainwaveManager != null && brainwaveManager.AlienBrainwaveActive);

    private void PrepareBrainwaveVisual()
    {
        alienRenderer = GetComponent<SpriteRenderer>();
        if (angryWarningTransform == null) return;
        angryRenderer = angryWarningTransform.GetComponentInChildren<SpriteRenderer>(true);
        if (angryRenderer == null || angryRenderer.sprite == null) return;
        redAngrySprite = angryRenderer.sprite;
        var source = redAngrySprite.texture;
        var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
        var previous = RenderTexture.active;
        try
        {
            Graphics.Blit(source, rt);
            RenderTexture.active = rt;
            greenAngryTexture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            greenAngryTexture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            var pixels = greenAngryTexture.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                pixels[i] = new Color(c.g, c.r, c.b, c.a);
            }
            greenAngryTexture.SetPixels(pixels);
            greenAngryTexture.Apply();
            greenAngryTexture.filterMode = source.filterMode;
            Rect rect = redAngrySprite.rect;
            Vector2 pivot = new Vector2(redAngrySprite.pivot.x / rect.width, redAngrySprite.pivot.y / rect.height);
            greenAngrySprite = Sprite.Create(greenAngryTexture, rect, pivot, redAngrySprite.pixelsPerUnit);
        }
        finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt); }
    }

    private void ResetBrainwaveCooldown()
    {
        brainwaveCooldown = Random.Range(Mathf.Min(minBrainwaveCooldown, maxBrainwaveCooldown), Mathf.Max(minBrainwaveCooldown, maxBrainwaveCooldown));
    }

    private void ClearBrainwave()
    {
        if (brainwaveCasting && brainwaveManager != null && brainwaveManager.AlienBrainwaveActive)
            brainwaveManager.CancelForObstacleHit();
        brainwaveCasting = false;
        if (angryRenderer != null && redAngrySprite != null) angryRenderer.sprite = redAngrySprite;
        SetAngryWarning(false);
    }

    private IEnumerator CastBrainwave()
    {
        if (isAttacking || isUltimateWarning || BrainwaveBlocksAttacks) yield break;
        activeLaserBeams.RemoveAll(beam => beam == null);
        if (activeLaserBeams.Count > 0) yield break;
        isAttacking = true;
        brainwaveCasting = true;
        if (angryRenderer != null && greenAngrySprite != null) angryRenderer.sprite = greenAngrySprite;
        SetAngryWarning(true);
        Vector3 origin = transform.position;
        float tell = 0f;
        while (tell < 0.4f)
        {
            if (Time.timeScale > 0f) tell += Time.unscaledDeltaTime;
            transform.position = origin + Vector3.right * Mathf.Sin(Time.unscaledTime * 65f) * 0.065f;
            yield return null;
        }
        float duration = Random.Range(Mathf.Clamp(minBrainwaveDuration, 1f, 2.5f), Mathf.Clamp(maxBrainwaveDuration, 1f, 2.5f));
        if (brainwaveManager != null && brainwaveManager.TryStartAlienBrainwave(duration, alienRenderer != null ? alienRenderer.sprite : null, alienRenderer != null && alienRenderer.flipX))
        {
            while (brainwaveManager.AlienBrainwaveActive)
            {
                transform.position = origin + Vector3.right * Mathf.Sin(Time.unscaledTime * 65f) * 0.065f;
                yield return null;
            }
        }
        transform.position = origin;
        ClearBrainwave();
        isAttacking = false;
        yVelocity = 0f;
        ResetBrainwaveCooldown();
    }

    private float yVelocity;
    private float bobTimer;
    private float nextLaserTimer;
    private bool isAttacking;
    private bool isUltimateWarning;
    private GameObject ultimateShotWarning;
    private GameObject standardShotWarning;

    public void BeginEndingExit()
    {
        StopAllCoroutines();
        ClearBrainwave();
        if (ultimateShotWarning != null) Destroy(ultimateShotWarning);
        if (standardShotWarning != null) Destroy(standardShotWarning);
        SetAngryWarning(false);
        enabled = false;
    }

    void OnDestroy()
    {
        ClearBrainwave();
        if (greenAngrySprite != null) Destroy(greenAngrySprite);
        if (greenAngryTexture != null) Destroy(greenAngryTexture);
        if (ultimateShotWarning != null) Destroy(ultimateShotWarning);
        if (standardShotWarning != null) Destroy(standardShotWarning);
    }

    void Start()
    {
        if (playerTransform == null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null) playerTransform = player.transform;
        }

        PrepareBrainwaveVisual();
        brainwaveManager = FindAnyObjectByType<PanicEventManagerVariantNoStop>();
        ResetBrainwaveCooldown();
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

        if (!brainwaveCasting && Time.timeScale > 0f) brainwaveCooldown -= Time.unscaledDeltaTime;
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
        if (isAttacking || isUltimateWarning || BrainwaveBlocksAttacks || PanicObstacleRules.EventRunning) return;

        if (brainwaveCooldown <= 0f && brainwaveManager != null && brainwaveManager.isActiveAndEnabled)
        {
            StartCoroutine(CastBrainwave());
            return;
        }
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
        if (playerTransform == null || isAttacking || isUltimateWarning || BrainwaveBlocksAttacks) yield break;

        isAttacking = true;

        float attackOffsetY = Random.Range(minAttackOffsetY, maxAttackOffsetY);
        float fixedX = fixedRightX;
        Vector3 startPos = new Vector3(fixedX, transform.position.y, transform.position.z);
        float trackedY = startPos.y;
        GameObject warningLine = null;

        if (laserWarningLinePrefab != null)
        {
            while (PanicObstacleRules.EventRunning) yield return null;
            warningLine = Instantiate(laserWarningLinePrefab, startPos, Quaternion.identity);
            standardShotWarning = warningLine;
            HazardWarningCue.Attach(warningLine, true);
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
        while (PanicObstacleRules.EventRunning) yield return null;

        if (warningLine != null)
        {
            Destroy(warningLine);
        }

        standardShotWarning = null;
        FreezeLaserBeam(fixedX, trackedY);

        // รีเซ็ตความเร็ว Y ไม่ให้ Alien กระตุกตอนกลับเข้าสภาวะขยับปกติ
        yVelocity = 0f;
        isAttacking = false;
    }

    private IEnumerator FireUltimateLaserSequence()
    {
        if (playerTransform == null || isAttacking || isUltimateWarning || BrainwaveBlocksAttacks) yield break;

        yield return StartCoroutine(PlayUltimateWarning());

        isAttacking = true;

        float fixedX = fixedRightX;
        // Keep the burst local to the Alien's initial position, without tracking the player.
        float burstCenterY = transform.position.y;
        for (int shot = 0; shot < ultimateLaserCount; shot++)
        {
            while (PanicObstacleRules.EventRunning) yield return null;
            float spread = Mathf.Max(0f, ultimateSpreadY);
            float targetY = burstCenterY + Random.Range(-spread, spread);
            float startY = transform.position.y;
            float timer = 0f;

            while (timer < ultimateTrackDuration)
            {
                timer += Time.deltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(timer / Mathf.Max(0.01f, ultimateTrackDuration)));
                float trackedY = Mathf.Lerp(startY, targetY, progress);
                transform.position = new Vector3(fixedX, trackedY, transform.position.z);
                yield return null;
            }

            transform.position = new Vector3(fixedX, targetY, transform.position.z);
            while (PanicObstacleRules.EventRunning) yield return null;
            FreezeLaserBeam(fixedX, targetY);

            if (shot < ultimateLaserCount - 1)
                yield return new WaitForSeconds(ultimateLaserInterval);
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
        if (BrainwaveBlocksAttacks) return;
        if (laserBeamPrefab == null)
        {
            Debug.LogWarning("[AlienBoss] ยังไม่ได้ใส่ Laser Beam Prefab");
            return;
        }

        GameObject beam = Instantiate(laserBeamPrefab, new Vector3(x, y, 0f), Quaternion.identity);
        activeLaserBeams.RemoveAll(active => active == null);
        activeLaserBeams.Add(beam);

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
