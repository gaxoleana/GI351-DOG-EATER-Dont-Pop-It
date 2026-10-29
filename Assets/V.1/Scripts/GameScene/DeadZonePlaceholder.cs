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
    private float noBlowTimer;
    [Min(0f)] public float hideDelay = 0.2f;
    [Min(0f)] public float fadeDuration = 0.35f;

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
        currentAlpha = 0f;
        line.enabled = false;
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
        ApplyAlpha();
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
        bool blowing = player != null && player.IsBlowing && gum != null && gum.CanBlow() && gum.currentSize > 0.0001f;
        UpdateVisibility(blowing, Time.deltaTime);
        if (line.enabled) DrawCircle();
    }

    private void UpdateVisibility(bool blowing, float deltaTime)
    {
        float targetAlpha = currentAlpha;
        if (blowing)
        {
            noBlowTimer = 0f;
            targetAlpha = placeholderColor.a;
        }
        else
        {
            // Keep the ring steady through brief gaps between presses.
            float previousTimer = noBlowTimer;
            noBlowTimer += deltaTime;
            deltaTime = Mathf.Max(0f, noBlowTimer - hideDelay) - Mathf.Max(0f, previousTimer - hideDelay);
            if (noBlowTimer >= hideDelay) targetAlpha = 0f;
        }

        currentAlpha = fadeDuration <= 0f ? targetAlpha :
            Mathf.MoveTowards(currentAlpha, targetAlpha, placeholderColor.a / fadeDuration * deltaTime);
        bool visible = currentAlpha > 0f;
        line.enabled = visible;
        contrastLine.enabled = visible;
        ApplyAlpha();
    }

    void OnDisable()
    {
        currentAlpha = 0f;
        noBlowTimer = 0f;
        if (line != null) line.enabled = false;
        if (contrastLine != null) contrastLine.enabled = false;
        if (line != null) ApplyAlpha();
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
    }

    private void ApplyAlpha()
    {
        Color color = placeholderColor;
        color.a = currentAlpha;
        line.startColor = color;
        line.endColor = color;
        if (contrastLine != null)
        {
            float opacity = placeholderColor.a > 0f ? currentAlpha / placeholderColor.a : 0f;
            Color rimColor = new Color(0.04f, 0.12f, 0.2f, 0.85f * opacity);
            contrastLine.startColor = contrastLine.endColor = rimColor;
        }
    }

}
