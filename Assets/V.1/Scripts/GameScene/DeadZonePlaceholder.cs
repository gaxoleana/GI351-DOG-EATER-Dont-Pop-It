using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DeadZonePlaceholder : MonoBehaviour
{
    [Header("References")]
    public GumController gum;

    [Header("Circle Settings")]
    [Range(8, 64)] public int segments = 32;
    public float lineWidth = 0.05f;
    public Color placeholderColor = new Color(1f, 1f, 1f, 0.35f);
    public float radiusMultiplier = 0.55f;

    private LineRenderer line;
    private float noInputTimer = 0f;
    public float hideDelay = 0.2f;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = segments;
        line.widthMultiplier = lineWidth;
        line.startColor = placeholderColor;
        line.endColor = placeholderColor;

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
        // 1) ดักจับ Input (Spacebar หรือ Mouse Button 0)
        bool hasInput = Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0);

        if (hasInput)
        {
            noInputTimer = 0f;
            if (!line.enabled) line.enabled = true;
        }
        else
        {
            noInputTimer += Time.deltaTime;
            if (noInputTimer >= hideDelay && line.enabled)
            {
                line.enabled = false;
            }
        }

        // 2) อัปเดตและวาดวงกลมเฉพาะตอนที่มันถูกเปิดให้แสดงเท่านั้น
        if (line.enabled)
        {
            DrawCircle();
        }
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