using Unity.Cinemachine;
using UnityEngine;

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

    private CinemachineBasicMultiChannelPerlin noiseComponent;
    private Rigidbody2D rb;
    private bool isBlowInputHeld;
    private float defaultGravityScale;

    // สถานะพิเศษจากภายนอก เช่น Panic Event Blue (ห้ามกด)
    private bool inputLocked;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (vcam == null) vcam = FindAnyObjectByType<CinemachineCamera>();
        if (vcam != null)
        {
            noiseComponent = vcam.GetComponent<CinemachineBasicMultiChannelPerlin>();
        }
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
    }

    void Update()
    {
        if (useDefaultInput)
        {
            ReadDefaultInput();
        }

        UpdateGumFromInput();
        HandleMaxFallShake();

        if (rb != null)
        {
            debugVelocityY = rb.linearVelocity.y; // Unity 6 / 2023.3+ ใช้ linearVelocity (หากใช้ Unity เวอร์ชันเก่ากว่าให้เปลี่ยนเป็น rb.velocity.y)
            debugGravityScale = rb.gravityScale;
        }
    }

    void FixedUpdate()
    {
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

    private void UpdateGumFromInput()
    {
        if (gum == null || !gum.CanBlow()) return;

        bool effectiveBlow = isBlowInputHeld && !inputLocked;

        if (effectiveBlow)
        {
            gum.Grow(Time.deltaTime);
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

            float targetVelocityY = baseLiftForce * liftMultiplier;

            // ไล่ velocity เข้าหาเป้าหมายทีละนิด แทนการ snap ทันที
            // ถ้ากำลังตกอยู่ (velocity.y ติดลบ) จะค่อย ๆ ชะลอก่อน แล้วค่อยไต่ขึ้นเป็นลอยจริง
            velocity.y = Mathf.MoveTowards(velocity.y, targetVelocityY, liftAcceleration * Time.fixedDeltaTime);
        }
        else
        {
            // ร่วงอิสระ (ตอนปล่อย, ตอน Popped, ตอน Dazed)
            velocity.y -= gravity * Time.fixedDeltaTime;
            velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
        }

        rb.linearVelocity = velocity;
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
            PanicEventManager panicManager = FindAnyObjectByType<PanicEventManager>();
            if (panicManager != null && panicManager.currentState == PanicEventManager.EventState.Idle)
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
    public void OnHitByObstacleBody()
    {
        gum?.TriggerDazed();
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