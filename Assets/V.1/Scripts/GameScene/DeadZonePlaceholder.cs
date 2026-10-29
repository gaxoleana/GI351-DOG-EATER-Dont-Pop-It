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
    private LineRenderer contrastLine;
    private PlayerController player;
    private float currentAlpha;
    public float hideDelay = 0.2f;
    public float fadeDuration = 0.35f;

    void Awake()
    {
        // Bright cyan with a dark rim stays readable on every altitude background.
        placeholderColor = new Color(0.35f, 0.95f, 1f, 0.95f);
        lineWidth = 0.045f;
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = segments;
        line.widthMultiplier = lineWidth;
        currentAlpha = placeholderColor.a;
        ApplyAlpha();

        if (line.sharedMaterial == null)
        {
            line.material = new Material(Shader.Find("Sprites/Default"));
        }

        var rim = new GameObject("Placeholder Contrast Rim");
        rim.transform.SetParent(transform, false);
        contrastLine = rim.AddComponent<LineRenderer>();
        contrastLine.sharedMaterial = line.sharedMaterial;
        contrastLine.useWorldSpace = false;
        contrastLine.loop = true;
        contrastLine.positionCount = segments;
        contrastLine.widthMultiplier = lineWidth + 0.04f;
        contrastLine.sortingLayerID = line.sortingLayerID;
        contrastLine.sortingOrder = line.sortingOrder - 1;
        contrastLine.startColor = contrastLine.endColor = new Color(0.04f, 0.12f, 0.2f, 0.85f);
        contrastLine.enabled = false;
    }

    void Start()
    {
        player = GetComponentInParent<PlayerController>();
        if (gum == null && player != null) gum = player.gum;
        line.enabled = false;
        DrawCircle();
    }

    void LateUpdate()
    {
        bool visible = player != null && player.IsBlowing && gum != null && gum.CanBlow() && gum.currentSize > 0.0001f;
        line.enabled = visible;
        contrastLine.enabled = visible;
        currentAlpha = visible ? placeholderColor.a : 0f;
        ApplyAlpha();
        if (visible) DrawCircle();
    }

    void OnDisable()
    {
        if (line != null) line.enabled = false;
        if (contrastLine != null) contrastLine.enabled = false;
    }

    private void DrawCircle()
    {
        if (gum == null) return;

        transform.position = gum.DeadZoneCenter;

        float radius = gum.CurrentMaxVisualScale * radiusMultiplier;

        for (int i = 0; i < segments; i++)
        {
            float angle = (2f * Mathf.PI * i) / segments;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            line.SetPosition(i, new Vector3(x, y, 0f));
            contrastLine.SetPosition(i, new Vector3(x, y, 0f));
        }

        line.widthMultiplier = lineWidth;
        ApplyAlpha();
    }

    private void ApplyAlpha()
    {
        Color color = placeholderColor;
        color.a = currentAlpha;
        line.startColor = color;
        line.endColor = color;
    }
}
