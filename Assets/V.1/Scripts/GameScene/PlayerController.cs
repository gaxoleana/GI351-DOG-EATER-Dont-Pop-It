using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI; // 🔹 เพิ่มบรรทัดนี้เพื่อเรียกใช้ UI Image
using TMPro;
using System.Collections; // 🔹 เพิ่มบรรทัดนี้สำหรับ IEnumerator
using System.Collections.Generic;

/// <summary>
/// จัดการ input (one-button: กดค้าง/ปล่อย), movement, และเชื่อมกับ GumController
/// เพื่อให้ lift force ขึ้นอยู่กับขนาด gum ปัจจุบัน
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public GumController gum;

    [Header("Force Settings")]
    [Tooltip("แรงยกพื้นฐานตอน gum ยังเล็ก")]
    public float baseLiftForce = 5f;

    [Tooltip("แรงยกสูงสุด (คูณ) ตอน gum ใหญ่เต็มที่ เทียบกับ base")]
    public float liftMultiplierMax = 2.5f;

    [Header("Threshold Growth Speed Boost")]
    [Tooltip("เปิด/ปิด — ให้ threshold ที่ขยายขึ้น (ผ่าน milestone) ทำให้ลอยเร็วขึ้นด้วย")]
    public bool scaleLiftWithThresholdGrowth = true;

    [Tooltip("เพดานตัวคูณแรงยกที่มาจาก threshold growth (กันไม่ให้เร็วทะลุจนคุมไม่ได้ตอน threshold โตสุด)")]
    public float maxThresholdSpeedBoost = 1.8f;

    [Tooltip("แรงโน้มถ่วงตอนไม่ได้เป่า / ตอนมึนงง / ตอนแตก")]
    public float gravity = 9.8f;

    [Tooltip("ความเร็วตกสูงสุด กันไม่ให้ตกเร็วจนควบคุมไม่ทัน")]
    public float maxFallSpeed = 12f;

    [Tooltip("อัตราเร่งตอนเปลี่ยนจากตก → ลอย (หน่วย/วินาที²) ยิ่งค่าน้อย ยิ่งค่อย ๆ ชะลอ-ไต่ขึ้นนุ่มนวล ยิ่งค่ามาก ยิ่งเด้งขึ้นเร็ว")]
    public float liftAcceleration = 15f;

    [Header("Input")]
    [Tooltip("ใช้ mouse/touch (กดซ้าย = เป่า) — ปิดถ้าจะต่อ Input System เอง")]
    public bool useDefaultInput = true;

    [Header("Debug Status (Read Only)")]
    [Tooltip("ความเร็ว Y ปัจจุบัน (เปิดดูใน Inspector ตอนเล่น)")]
    [SerializeField] private float debugVelocityY;

    [Tooltip("ค่า Gravity Scale ปัจจุบัน (เปิดดูใน Inspector ตอนเล่น)")]
    [SerializeField] private float debugGravityScale;

    [Header("Max Fall Speed Shake Settings")]
    [Tooltip(" CinemachineCamera สำหรับทำจอสั่น")]
    public CinemachineCamera vcam;

    [Tooltip("ระดับความเร็วร่วงลง Y ที่จะเริ่มให้จอสั่น (ติดลบ เช่น -15)")]
    public float maxFallSpeedThreshold = -15f;

    [Tooltip("ความแรงจอสั่นตอนตกด้วยความเร็วสูงสุด")]
    public float fallShakeAmplitude = 2.0f;

    [Header("Damage Flash Effect")]
    [Tooltip("UI Image สีดำเต็มจอ สำหรับกระพริบตอนโดนชนตัว")]
    public Image blackFlashImage;

    [Tooltip("ความเร็วในการจางหายของจอดำ (ค่ายิ่งเยอะยิ่งจางเร็ว)")]
    public float flashFadeSpeed = 5f;

    [Header("Damage Feedback")]
    [Tooltip("UI Image สีดำเต็มจอ สำหรับกระพริบตอนโดนชนตัว")]
    public float damageDuration = 3f; // ระยะเวลาจอดำและสั่น (3 วินาที)
    public float damageShakeAmplitude = 4f; // ความแรงของการสั่นตอนโดนชน

    [Header("Event Immunity Settings")]
    [Tooltip("ระยะเวลา Immunity หลังผ่าน Event แดง/ฟ้า (วินาที)")]
    public float eventImmunityDuration = 1.0f;

    [Tooltip("คูลดาวน์ของ Immunity หลังใช้งาน (วินาที)")]
    public float eventImmunityCooldown = 15.0f;

    [Tooltip("สีทองสำหรับขอบตัวละครและลูกโป่งตอนได้ Immunity")]
    public Color immunityGoldColor = new Color(1f, 0.8431373f, 0f, 1f);

    [Tooltip("ความหนาของขอบสีทองรอบตัวละครตอนได้ Immunity")]
    public float playerOutlineThickness = 0.06f;

    [Header("Player Body Hit Penalty")]
    [Tooltip("ระยะเวลาเป่าช้าลงและบินช้าลงเมื่อโดนชนที่ตัว Player (วินาที)")]
    public float bodyHitSlowDuration = 2f;

    [Tooltip("ตัวคูณความเร็วในการเป่าหมากฝรั่งระหว่างติดสถานะ (0.5 = เป่าช้าลงครึ่งหนึ่ง)")]
    [Range(0.1f, 1f)]
    public float bodyHitBlowSpeedMultiplier = 0.5f;

    [Tooltip("ตัวคูณแรงยก/ความเร็วบินระหว่างติดสถานะ (0.5 = บินช้าลงครึ่งหนึ่ง)")]
    [Range(0.1f, 1f)]
    public float bodyHitLiftMultiplier = 0.5f;

    private float bodyHitSlowTimer;
    public bool IsBodyHitSlowed => bodyHitSlowTimer > 0f && !IsObstacleImmune;
    public float BodyHitSlowTimeRemaining => IsBodyHitSlowed ? bodyHitSlowTimer : 0f;

    private bool isDamageShaking = false; // เอาไว้กันไม่ให้สั่นตีกับตอนตกลงมา

    private CinemachineBasicMultiChannelPerlin noiseComponent;
    private Rigidbody2D rb;
    private bool isBlowInputHeld;
    private float defaultGravityScale;
    private Coroutine damageFeedbackCoroutine;
    private readonly List<float> smokeLiftMultipliers = new List<float>();
    private bool rocketBoostActive;
    private float rocketBoostSpeed;
    private float rocketBoostTargetY;
    private Coroutine rocketBoostCoroutine;
    public float RocketBoostTimeRemaining { get; private set; }

    // สถานะ Immunity หลังผ่าน Event แดง/ฟ้า
    private bool eventImmunityActive;
    private float nextEventImmunityAvailableTime = -999f;
    private Coroutine eventImmunityCoroutine;
    public float ImmunityTimeRemaining { get; private set; }
    public bool IsEventImmune => eventImmunityActive;

    private SpriteRenderer playerSpriteRenderer;
    private GameObject outlineRoot;
    private readonly List<SpriteRenderer> outlineRenderers = new List<SpriteRenderer>();
    private Material outlineMaterial;
    private TextMeshProUGUI immunityStatusText;

    // สถานะพิเศษจากภายนอก เช่น Panic Event Blue (ห้ามกด)
    private bool inputLocked;

    public bool IsInputLocked => inputLocked;
    public bool IsBlowing => isBlowInputHeld && !inputLocked && gum != null && gum.CanBlow();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        defaultGravityScale = rb.gravityScale;
        playerSpriteRenderer = GetComponent<SpriteRenderer>();

        if (vcam == null) vcam = FindAnyObjectByType<CinemachineCamera>();
        if (vcam != null)
        {
            noiseComponent = vcam.GetComponent<CinemachineBasicMultiChannelPerlin>();
        }

        BuildGoldOutline();
    }

    void Start()
    {
        if (gum != null)
        {
            gum.Init(transform);
        }
        else
        {
            Debug.LogWarning("[PlayerController] ยังไม่ได้ผูก GumController ใน Inspector");
        }

        EnsureImmunityStatusText();
    }

    void OnDestroy()
    {
        if (outlineMaterial != null)
        {
            Destroy(outlineMaterial);
        }
    }

    void Update()
    {
        if (useDefaultInput)
        {
            ReadDefaultInput();
        }

        UpdateGumFromInput();
        HandleMaxFallShake();
        UpdateImmunityUI();

        if (bodyHitSlowTimer > 0f)
        {
            bodyHitSlowTimer = Mathf.Max(0f, bodyHitSlowTimer - Time.deltaTime);
        }

        if (rb != null)
        {
            debugVelocityY = rb.linearVelocity.y; // Unity 6 / 2023.3+ ใช้ linearVelocity (หากใช้ Unity เวอร์ชันเก่ากว่าให้เปลี่ยนเป็น rb.velocity.y)
            debugGravityScale = rb.gravityScale;
        }
    }

    void LateUpdate()
    {
        UpdateGoldOutlineVisual();
    }

    void FixedUpdate()
    {
        if (rocketBoostActive)
        {
            Vector2 boostVelocity = rb.linearVelocity;
            if (transform.position.y < rocketBoostTargetY)
            {
                boostVelocity.y = rocketBoostSpeed;
            }
            else
            {
                transform.position = new Vector3(
                    transform.position.x,
                    rocketBoostTargetY,
                    transform.position.z);
                boostVelocity.y = 0f;
            }

            rb.linearVelocity = boostVelocity;
            return;
        }

        ApplyMovement();
    }

    private void ReadDefaultInput()
    {
        // รองรับทั้ง mouse (editor/PC), touch (มือถือ), และ Spacebar (keyboard)
        isBlowInputHeld = Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space);
    }

    /// <summary>
    /// เรียกจากภายนอก (Input System, UI button ฯลฯ) ถ้าไม่ได้ใช้ default input
    /// </summary>
    public void SetBlowInput(bool held)
    {
        isBlowInputHeld = held;
    }

    /// <summary>
    /// เรียกจาก PanicEventManager ตอน Blue Event (ห้ามกด) หรือ lock ชั่วคราวอื่น ๆ
    /// </summary>
    public void SetInputLocked(bool locked)
    {
        inputLocked = locked;
    }

    public bool IsObstacleImmune => rocketBoostActive || eventImmunityActive;

    /// <summary>
    /// เรียกเมื่อผ่าน Event แดง/ฟ้า สำเร็จ เพื่อเปิดใช้งาน Immunity 1 วินาที (คูลดาวน์ 15 วินาที)
    /// </summary>
    public bool TryActivateEventImmunity()
    {
        if (Time.time < nextEventImmunityAvailableTime)
        {
            return false;
        }

        nextEventImmunityAvailableTime = Time.time + Mathf.Max(0f, eventImmunityCooldown);

        if (eventImmunityCoroutine != null)
        {
            StopCoroutine(eventImmunityCoroutine);
        }

        eventImmunityCoroutine = StartCoroutine(EventImmunityRoutine(Mathf.Max(0.1f, eventImmunityDuration)));
        return true;
    }

    private IEnumerator EventImmunityRoutine(float duration)
    {
        eventImmunityActive = true;
        ImmunityTimeRemaining = duration;

        if (gum != null)
        {
            gum.SetGoldImmunityVisual(true, immunityGoldColor);
        }
        UpdateGoldOutlineVisual();
        UpdateImmunityUI();

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            ImmunityTimeRemaining = Mathf.Max(0f, duration - timer);
            yield return null;
        }

        eventImmunityActive = false;
        ImmunityTimeRemaining = 0f;
        eventImmunityCoroutine = null;

        if (gum != null)
        {
            gum.SetGoldImmunityVisual(false, immunityGoldColor);
        }
        UpdateGoldOutlineVisual();
        UpdateImmunityUI();
    }

    private void BuildGoldOutline()
    {
        if (playerSpriteRenderer == null) return;

        Shader solidShader = Shader.Find("GUI/Text Shader");
        if (solidShader == null) solidShader = Shader.Find("Sprites/Default");
        if (solidShader != null)
        {
            outlineMaterial = new Material(solidShader);
        }

        outlineRoot = new GameObject("PlayerGoldOutline");
        outlineRoot.transform.SetParent(transform, false);
        outlineRoot.transform.localPosition = Vector3.zero;
        outlineRoot.transform.localRotation = Quaternion.identity;
        outlineRoot.transform.localScale = Vector3.one;

        Vector2[] dirs = new Vector2[]
        {
            new Vector2(1f, 0f),
            new Vector2(-1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(0f, -1f),
            new Vector2(0.7071f, 0.7071f),
            new Vector2(-0.7071f, 0.7071f),
            new Vector2(0.7071f, -0.7071f),
            new Vector2(-0.7071f, -0.7071f)
        };

        for (int i = 0; i < dirs.Length; i++)
        {
            GameObject child = new GameObject($"Outline_{i}");
            child.transform.SetParent(outlineRoot.transform, false);
            child.transform.localPosition = new Vector3(
                dirs[i].x * playerOutlineThickness,
                dirs[i].y * playerOutlineThickness,
                0f);

            SpriteRenderer sr = child.AddComponent<SpriteRenderer>();
            if (outlineMaterial != null) sr.sharedMaterial = outlineMaterial;
            sr.color = immunityGoldColor;
            sr.sortingLayerID = playerSpriteRenderer.sortingLayerID;
            sr.sortingOrder = playerSpriteRenderer.sortingOrder - 1;
            outlineRenderers.Add(sr);
        }

        outlineRoot.SetActive(false);
    }

    private void UpdateGoldOutlineVisual()
    {
        if (outlineRoot == null || playerSpriteRenderer == null) return;

        if (outlineRoot.activeSelf != eventImmunityActive)
        {
            outlineRoot.SetActive(eventImmunityActive);
        }

        if (!eventImmunityActive) return;

        Vector2[] dirs = new Vector2[]
        {
            new Vector2(1f, 0f),
            new Vector2(-1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(0f, -1f),
            new Vector2(0.7071f, 0.7071f),
            new Vector2(-0.7071f, 0.7071f),
            new Vector2(0.7071f, -0.7071f),
            new Vector2(-0.7071f, -0.7071f)
        };

        for (int i = 0; i < outlineRenderers.Count; i++)
        {
            SpriteRenderer sr = outlineRenderers[i];
            if (sr == null) continue;
            sr.sprite = playerSpriteRenderer.sprite;
            sr.flipX = playerSpriteRenderer.flipX;
            sr.flipY = playerSpriteRenderer.flipY;
            sr.color = immunityGoldColor;
            sr.sortingLayerID = playerSpriteRenderer.sortingLayerID;
            sr.sortingOrder = playerSpriteRenderer.sortingOrder - 1;
            sr.transform.localPosition = new Vector3(
                dirs[i].x * playerOutlineThickness,
                dirs[i].y * playerOutlineThickness,
                0f);
        }
    }

    private void EnsureImmunityStatusText()
    {
        if (immunityStatusText != null) return;

        TextMeshProUGUI template = null;
        RocketBoostUI boostUI = FindAnyObjectByType<RocketBoostUI>();
        if (boostUI != null && boostUI.boostText != null)
        {
            template = boostUI.boostText;
        }
        else
        {
            GumRecoveryUI recoveryUI = FindAnyObjectByType<GumRecoveryUI>();
            if (recoveryUI != null && recoveryUI.recoveryText != null)
            {
                template = recoveryUI.recoveryText;
            }
        }

        if (template == null) return;

        Transform parent = template.transform.parent;
        GameObject textObj = Instantiate(template.gameObject, parent);
        textObj.name = "Immunity Status Text";
        immunityStatusText = textObj.GetComponent<TextMeshProUGUI>();

        RectTransform rt = immunityStatusText.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -90f);
        rt.sizeDelta = new Vector2(600f, 60f);
        rt.localScale = Vector3.one;

        immunityStatusText.fontSize = 60f;
        immunityStatusText.alignment = TextAlignmentOptions.Center;
        immunityStatusText.textWrappingMode = TextWrappingModes.NoWrap;
        immunityStatusText.overflowMode = TextOverflowModes.Overflow;
        immunityStatusText.color = immunityGoldColor;
        immunityStatusText.raycastTarget = false;
        textObj.SetActive(false);
    }

    private void UpdateImmunityUI()
    {
        if (immunityStatusText == null)
        {
            if (eventImmunityActive) EnsureImmunityStatusText();
            if (immunityStatusText == null) return;
        }

        bool show = eventImmunityActive && ImmunityTimeRemaining > 0f;
        if (immunityStatusText.gameObject.activeSelf != show)
        {
            immunityStatusText.gameObject.SetActive(show);
        }

        if (show)
        {
            immunityStatusText.color = immunityGoldColor;
            immunityStatusText.text = $"Immunity {ImmunityTimeRemaining:0.0}s";
        }
    }

    public void StartRocketBoost(float height = 50f, float duration = 2.5f)
    {
        FlightRewardPresentation.Flash();
        if (rocketBoostCoroutine != null)
        {
            StopCoroutine(rocketBoostCoroutine);
        }

        rocketBoostCoroutine = StartCoroutine(RocketBoostRoutine(height, duration));
    }

    private IEnumerator RocketBoostRoutine(float height, float duration)
    {
        rocketBoostActive = true;
        rocketBoostTargetY = transform.position.y + Mathf.Abs(height);
        rocketBoostSpeed = Mathf.Abs(height) / Mathf.Max(0.1f, duration);
        RocketBoostTimeRemaining = Mathf.Max(0f, duration);
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            RocketBoostTimeRemaining = Mathf.Max(0f, duration - timer);
            yield return null;
        }

        rocketBoostActive = false;
        RocketBoostTimeRemaining = 0f;
        rocketBoostSpeed = 0f;
        Vector2 velocity = rb.linearVelocity;
        velocity.y = 0f;
        rb.linearVelocity = velocity;
    }

    private void UpdateGumFromInput()
    {
        if (gum == null || !gum.CanBlow()) return;

        bool effectiveBlow = isBlowInputHeld && !inputLocked;

        if (effectiveBlow)
        {
            float blowMultiplier = IsBodyHitSlowed ? bodyHitBlowSpeedMultiplier : 1f;
            gum.Grow(Time.deltaTime * blowMultiplier);
        }
        else
        {
            gum.Shrink(Time.deltaTime);
        }
    }

    private void ApplyMovement()
    {
        Vector2 velocity = rb.linearVelocity;

        bool canLift = gum != null && gum.CanBlow() && isBlowInputHeld && !inputLocked;

        if (canLift)
        {
            float sizeRatio = gum.GetSizeRatio(); // 0-1 (สัดส่วนที่เป่าไปแล้วเทียบกับ threshold ตอนนี้)
            float liftMultiplier = Mathf.Lerp(1f, liftMultiplierMax, sizeRatio);

            if (scaleLiftWithThresholdGrowth)
            {
                // threshold ยิ่งโต (ผ่าน milestone) ยิ่งลอยเร็วขึ้นด้วย ไม่ใช่แค่ gum ดูใหญ่ขึ้นเฉย ๆ
                // ใช้ CurrentMaxVisualScale / maxVisualScale เพราะเท่ากับ currentDeadZoneThreshold / baseDeadZoneThreshold พอดี
                float thresholdGrowthFactor = gum.maxVisualScale > 0f
                    ? gum.CurrentMaxVisualScale / gum.maxVisualScale
                    : 1f;

                thresholdGrowthFactor = Mathf.Min(thresholdGrowthFactor, maxThresholdSpeedBoost);
                liftMultiplier *= thresholdGrowthFactor;
            }

            liftMultiplier *= SmokeLiftMultiplier;

            if (IsBodyHitSlowed)
            {
                liftMultiplier *= bodyHitLiftMultiplier;
            }

            float targetVelocityY = baseLiftForce * liftMultiplier;

            // ไล่ velocity เข้าหาเป้าหมายทีละนิด แทนการ snap ทันที
            // ถ้ากำลังตกอยู่ (velocity.y ติดลบ) จะค่อย ๆ ชะลอก่อน แล้วค่อยไต่ขึ้นเป็นลอยจริง
            velocity.y = Mathf.MoveTowards(velocity.y, targetVelocityY, liftAcceleration * Time.fixedDeltaTime);
        }
        else
        {
            // ร่วงอิสระ (ตอนปล่อย, ตอน Popped, ตอน Dazed)
            velocity.y -= gravity * rb.gravityScale * Time.fixedDeltaTime;
            velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
        }

        rb.linearVelocity = velocity;
    }

    private float SmokeLiftMultiplier
    {
        get
        {
            if (IsObstacleImmune) return 1f;

            float multiplier = 1f;
            for (int i = 0; i < smokeLiftMultipliers.Count; i++)
            {
                multiplier = Mathf.Min(multiplier, smokeLiftMultipliers[i]);
            }

            return multiplier;
        }
    }

    public void EnterSmoke(float liftMultiplier)
    {
        smokeLiftMultipliers.Add(Mathf.Clamp01(liftMultiplier));
    }

    public void ExitSmoke(float liftMultiplier)
    {
        smokeLiftMultipliers.Remove(Mathf.Clamp01(liftMultiplier));
    }

    /// <summary>
    /// ปรับตัวคูณแรงโน้มถ่วงชั่วคราว (เช่น ใส่ 0.5f เพื่อให้ตกช้าลงครึ่งหนึ่ง)
    /// </summary>
    public void SetGravityMultiplier(float multiplier)
    {
        if (rb != null)
        {
            rb.gravityScale = defaultGravityScale * multiplier;
        }
    }

    /// <summary>
    /// คืนค่าแรงโน้มถ่วงกลับสู่ระดับปกติ
    /// </summary>
    public void ResetGravity()
    {
        if (rb != null)
        {
            rb.gravityScale = defaultGravityScale;
        }
    }

    /// <summary>
    /// 🔹 สั่งเบรกความเร็วร่วงสะสม Y ไม่ให้ดิ่งเร็วเกินไปทันทีตอนเข้า Blue Event
    /// </summary>
    /// <param name="maxDownwardsVelocity">ความเร็วร่วงลงสูงสุดที่ยอมให้มีได้ (ค่าติดลบ เช่น -1.5f)</param>
    public void DampDownwardVelocity(float maxDownwardsVelocity = -1.5f)
    {
        if (rb == null) return;

        Vector2 vel = rb.linearVelocity;
        // ถ้ากำลังดิ่งลงเร็วกว่าค่า maxDownwardsVelocity ให้ดึงเบรกไว้ที่ค่านั้นทันที
        if (vel.y < maxDownwardsVelocity)
        {
            vel.y = maxDownwardsVelocity;
            rb.linearVelocity = vel;
        }
    }

    private void HandleMaxFallShake()
    {
        if (rb == null || noiseComponent == null) return;
        if (isDamageShaking) return; // 🔹 บรรทัดนี้สำคัญมาก! ถ้าสั่นจากดาเมจอยู่ ให้ข้ามการคำนวณตกเร็วไปเลย

        // เช็กความเร็วร่วง Y (ค่าติดลบยิ่งมาก = ตกยิ่งเร็ว)
        float currentVelY = rb.linearVelocity.y; // Unity 6 / 2023.3+ (เวอร์ชันเก่าใช้ rb.velocity.y)

        if (currentVelY <= maxFallSpeedThreshold)
        {
            // คำนวณความแรงสั่นตามระดับความเร็วที่เกิน Threshold
            float overSpeedRatio = Mathf.Clamp01((maxFallSpeedThreshold - currentVelY) / 10f);
            float currentShake = Mathf.Lerp(1.0f, fallShakeAmplitude, overSpeedRatio);

            noiseComponent.AmplitudeGain = currentShake;
        }
        else
        {
            // ถ้าไม่ใช่ช่วง Event สั่งหยุดสั่นเมื่อความเร็วตกปกติ
            PanicEventManagerVariantNoStop panicManager = FindAnyObjectByType<PanicEventManagerVariantNoStop>();
            if (panicManager != null && panicManager.currentState == PanicEventManagerVariantNoStop.EventState.Idle)
            {
                noiseComponent.AmplitudeGain = Mathf.Lerp(noiseComponent.AmplitudeGain, 0f, Time.deltaTime * 8f);
            }
        }
    }

    /// <summary>
    /// สั่งแรงยกตัวละครสั้น ๆ ตอนกดรัวใน Red Event เพื่อให้ตัวลอยขึ้นสู้แรงโน้มถ่วงได้ปกติ
    /// </summary>
    public void ApplyMashImpulse(float impulseForce = 3f)
    {
        Vector2 vel = rb.linearVelocity;
        // ปรับความเร็ว Y ขึ้นทันที ยิ่งกดรัวยิ่งไต่ระดับความสูงได้สม่ำเสมอ
        vel.y = Mathf.Max(vel.y + impulseForce, baseLiftForce);
        rb.linearVelocity = vel;
    }

    // --- เรียกจากระบบ Obstacle ---

    /// <summary>เรียกตอนโดน hitbox ตัว player (ไม่ใช่ hitbox gum)</summary>
    /// <summary>เรียกตอนโดน hitbox ตัว player (ไม่ใช่ hitbox gum)</summary>
    /// <summary>เรียกตอนโดน hitbox ตัว player (ไม่ใช่ hitbox gum)</summary>
    public void OnHitByObstacleBody()
    {
        if (IsObstacleImmune) return;

        bodyHitSlowTimer = bodyHitSlowDuration;

        if (gum != null && gum.currentState == GumController.GumState.Normal)
        {
            gum.currentSize = 0f;
        }

        // เริ่มแสดงเอฟเฟกต์จอดำและสั่น — หยุด Coroutine เดิมก่อน กันชนซ้ำเร็ว ๆ แล้ว Effect ซ้อนกัน
        if (damageFeedbackCoroutine != null)
        {
            StopCoroutine(damageFeedbackCoroutine);
        }
        damageFeedbackCoroutine = StartCoroutine(DamageFeedbackRoutine());
    }

    private IEnumerator DamageFeedbackRoutine()
    {
        isDamageShaking = true;

        FlightRewardPresentation.HitFlash();
        if (blackFlashImage != null)
        {
            blackFlashImage.color = Color.clear;
            blackFlashImage.gameObject.SetActive(false);
        }
        float timer = 0f;
        float duration = Mathf.Max(0.01f, Mathf.Min(damageDuration, gum != null ? gum.dazedDuration : 0.45f, 0.45f));
        while (timer < duration)
        {
            timer += Time.deltaTime;

            // คำนวณความรุนแรง (intensity) จาก 1 ไป 0 ตามเวลา 3 วินาที
            float intensity = 1f - (timer / duration);

            // 1. ค่อยๆ ลดความดำของจอ
            if (blackFlashImage != null)
            {
                Color flashColor = Color.black;
                flashColor.a = 0f;
                blackFlashImage.color = flashColor;
            }

            // 2. ค่อยๆ ลดความแรงของการสั่นกล้อง
            if (noiseComponent != null)
            {
                noiseComponent.AmplitudeGain = Mathf.Min(1.2f, damageShakeAmplitude) * intensity;
            }

            yield return null; // รอทำงานต่อในเฟรมถัดไป
        }

        // รีเซ็ตค่ากลับเป็นปกติเมื่อครบ 3 วินาที
        if (blackFlashImage != null)
        {
            blackFlashImage.color = new Color(0f, 0f, 0f, 0f);
            blackFlashImage.gameObject.SetActive(false);
        }

        if (noiseComponent != null)
        {
            noiseComponent.AmplitudeGain = 0f;
        }

        isDamageShaking = false;
    }

    private IEnumerator FlashBlackRoutine()
    {
        blackFlashImage.gameObject.SetActive(true);

        // เซ็ตสีเป็นสีดำทึบ (Alpha = 1) ทันทีที่โดนชน
        Color flashColor = Color.black;
        flashColor.a = 1f;
        blackFlashImage.color = flashColor;

        // ค่อยๆ Fade กลับไปโปร่งใสตามความเร็ว flashFadeSpeed
        while (blackFlashImage.color.a > 0.05f)
        {
            flashColor.a = Mathf.Lerp(flashColor.a, 0f, Time.deltaTime * flashFadeSpeed);
            blackFlashImage.color = flashColor;
            yield return null; // รอเฟรมถัดไป
        }

        // ปิด UI ทิ้งเมื่อจางหมดแล้ว
        flashColor.a = 0f;
        blackFlashImage.color = flashColor;
        blackFlashImage.gameObject.SetActive(false);
    }

    /// <summary>เรียกตอนโดน hitbox gum โดยตรง</summary>
    public void OnHitByObstacleGum()
    {
        // gum จะแตกเองผ่าน logic ปกติของมัน ถ้าอยากบังคับแตกทันที
        // สามารถเพิ่ม public ForcePop() ใน GumController แล้วเรียกตรงนี้แทน
        // ใส่ SFX โดนชน, ตัวละครชะงัก หรือเล่น Animation โดนชนตรงนี้ได้
        Debug.Log("Player taken damage from obstacle!");
    }
}
