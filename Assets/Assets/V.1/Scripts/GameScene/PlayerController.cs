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

    [Tooltip("แรงโน้มถ่วงตอนไม่ได้เป่า / ตอนมึนงง / ตอนแตก")]
    public float gravity = 9.8f;

    [Tooltip("ความเร็วตกสูงสุด กันไม่ให้ตกเร็วจนควบคุมไม่ทัน")]
    public float maxFallSpeed = 12f;

    [Tooltip("อัตราเร่งตอนเปลี่ยนจากตก → ลอย (หน่วย/วินาที²) ยิ่งค่าน้อย ยิ่งค่อย ๆ ชะลอ-ไต่ขึ้นนุ่มนวล ยิ่งค่ามาก ยิ่งเด้งขึ้นเร็ว")]
    public float liftAcceleration = 15f;

    [Header("Input")]
    [Tooltip("ใช้ mouse/touch (กดซ้าย = เป่า) — ปิดถ้าจะต่อ Input System เอง")]
    public bool useDefaultInput = true;

    private Rigidbody2D rb;
    private bool isBlowInputHeld;

    // สถานะพิเศษจากภายนอก เช่น Panic Event Blue (ห้ามกด)
    private bool inputLocked;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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
            float sizeRatio = gum.GetSizeRatio(); // 0-1
            float liftMultiplier = Mathf.Lerp(1f, liftMultiplierMax, sizeRatio);
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
    }
}