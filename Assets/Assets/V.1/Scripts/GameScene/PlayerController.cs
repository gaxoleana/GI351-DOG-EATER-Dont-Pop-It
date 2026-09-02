using UnityEngine;

// ติดสคริปต์นี้ไว้ที่ Player (parent) — อ้างอิง BalloonController จาก child อัตโนมัติ
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public BalloonController balloon;   // ลากใส่เอง หรือปล่อยว่างให้หาอัตโนมัติจาก child

    [Header("Float Settings")]
    public float floatForcePerSize = 8f;   // แรงยกต่อหน่วยขนาดลูกโป่ง ยิ่งลูกโป่งใหญ่ยิ่งลอยแรง
    public float neutralSize = 1f;         // ขนาดที่ไม่ลอยไม่ตก (สมดุลกับแรงโน้มถ่วง)
    public float maxRiseSpeed = 6f;        // จำกัดความเร็วลอยขึ้นสูงสุด
    public float maxFallSpeed = 10f;       // จำกัดความเร็วตกสูงสุด

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (balloon == null)
        {
            balloon = GetComponentInChildren<BalloonController>();
        }
    }

    void OnEnable()
    {
        if (balloon != null)
        {
            balloon.OnPop += HandleBalloonPop;
        }
    }

    void OnDisable()
    {
        if (balloon != null)
        {
            balloon.OnPop -= HandleBalloonPop;
        }
    }

    void FixedUpdate()
    {
        if (balloon == null || balloon.IsPopped) return; // ลูกโป่งแตกแล้ว ปล่อยให้ตกตามแรงโน้มถ่วงปกติ

        // ขนาดลูกโป่งมากกว่า neutralSize เท่าไหร่ ยิ่งลอยขึ้นเร็วเท่านั้น
        // น้อยกว่า neutralSize จะกลายเป็นแรงลบ (ตกลง) โดยอัตโนมัติ ไม่ต้องคำนวณแยก
        float sizeDiff = balloon.currentSize - neutralSize;
        float floatForce = sizeDiff * floatForcePerSize;

        rb.AddForce(Vector2.up * floatForce);

        // จำกัดความเร็วแนวตั้งไม่ให้ลอย/ตกเร็วเกินไป
        float clampedY = Mathf.Clamp(rb.linearVelocity.y, -maxFallSpeed, maxRiseSpeed);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, clampedY);
    }

    void HandleBalloonPop()
    {
        // ลูกโป่งแตก ไม่ต้องทำอะไรเป็นพิเศษที่นี่ — FixedUpdate จะข้ามการลอยเอง
        // ถ้าต้องการ effect เพิ่มเติมตอนตก (เช่นเล่นเสียง/anim) ใส่ตรงนี้ได้
    }
}