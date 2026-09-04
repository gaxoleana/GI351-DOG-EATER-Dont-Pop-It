using UnityEngine;

/// <summary>
/// วาดวงกลม placeholder ด้วย LineRenderer (ไม่ต้องมี sprite art) แสดงขอบเขตสูงสุดที่ gum
/// เป่าไปถึงได้ก่อนแตก — ไว้ใช้แทน art จริงระหว่างพัฒนา ค่อยเปลี่ยนเป็น sprite ทีหลังได้
/// วงนี้ "โชว์ตลอด" ต่างจาก ring warning ใน GumController ที่ fade in เฉพาะโซนอันตราย
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class DeadZonePlaceholder : MonoBehaviour
{
    [Header("References")]
    [Tooltip("ผูกกับ GumController เพื่ออ่านขนาด visual สูงสุดของ gum")]
    public GumController gum;

    [Header("Circle Settings")]
    [Tooltip("จำนวนจุดที่ใช้วาดวงกลม ยิ่งเยอะยิ่งกลมเนียน")]
    [Range(8, 64)] public int segments = 32;

    [Tooltip("ความหนาเส้น")]
    public float lineWidth = 0.05f;

    [Tooltip("สี placeholder (แนะนำให้โปร่งใสหน่อย จะได้ไม่บังเกม)")]
    public Color placeholderColor = new Color(1f, 1f, 1f, 0.35f);

    [Tooltip("รัศมีวงกลม = maxVisualScale ของ gum คูณค่านี้ ปรับได้ถ้าอยากให้วงใหญ่/เล็กกว่าขอบ gum จริง")]
    public float radiusMultiplier = 0.55f;

    private LineRenderer line;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = segments;
        line.widthMultiplier = lineWidth;
        line.startColor = placeholderColor;
        line.endColor = placeholderColor;

        // material พื้นฐานกันไม่ขึ้นสีชมพู (missing material) ถ้ายังไม่ได้ตั้งเอง
        if (line.sharedMaterial == null)
        {
            line.material = new Material(Shader.Find("Sprites/Default"));
        }
    }

    void Start()
    {
        DrawCircle();
    }

    void Update()
    {
        // เผื่อปรับค่า maxVisualScale ของ gum ระหว่าง playtest ใน Inspector
        // ถ้าไม่ต้องการ real-time update ลบบรรทัดนี้ทิ้งแล้วเรียก DrawCircle() ใน Start() อย่างเดียวพอ
        DrawCircle();
    }

    private void DrawCircle()
    {
        if (gum == null) return;

        float radius = gum.CurrentMaxVisualScale * radiusMultiplier;

        for (int i = 0; i < segments; i++)
        {
            float angle = (2f * Mathf.PI * i) / segments;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            line.SetPosition(i, new Vector3(x, y, 0f));
        }

        line.widthMultiplier = lineWidth;
        line.startColor = placeholderColor;
        line.endColor = placeholderColor;
    }
}