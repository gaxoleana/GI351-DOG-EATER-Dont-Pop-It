using UnityEngine;

// An edge arrow preserves the true lane while keeping its direction visible off-screen.
public class HazardWarningCue : MonoBehaviour
{
    private LineRenderer arrow;
    private Material material;
    private bool fromRight;
    public static void Attach(GameObject warning, bool fromRight)
    {
        if (warning == null) return;
        var cue = warning.AddComponent<HazardWarningCue>(); cue.fromRight = fromRight;
    }
    void Start()
    {
        var child = new GameObject("Hazard Edge Hint"); child.transform.SetParent(transform, false);
        arrow = child.AddComponent<LineRenderer>(); arrow.useWorldSpace = true;
        material = new Material(Shader.Find("Sprites/Default")); arrow.sharedMaterial = material;
        arrow.positionCount = 3; arrow.sortingOrder = 100;
        arrow.startColor = arrow.endColor = new Color(1f, 0.72f, 0.3f, 0.9f);
    }
    void LateUpdate()
    {
        var cam = Camera.main; if (cam == null || arrow == null) return;
        var viewport = cam.WorldToViewportPoint(transform.position);
        viewport.x = fromRight ? 0.92f : 0.08f;
        bool above = viewport.y > 0.88f, below = viewport.y < 0.12f;
        viewport.y = Mathf.Clamp(viewport.y, 0.12f, 0.88f);
        var center = cam.ViewportToWorldPoint(viewport); center.z = 0;
        float length = cam.orthographicSize * 0.045f;
        Vector3 direction = above ? Vector3.up : below ? Vector3.down : (fromRight ? Vector3.left : Vector3.right);
        Vector3 side = new Vector3(-direction.y, direction.x, 0);
        arrow.widthMultiplier = cam.orthographicSize * 0.009f;
        arrow.SetPosition(0, center - direction * length + side * length * 0.65f);
        arrow.SetPosition(1, center + direction * length);
        arrow.SetPosition(2, center - direction * length - side * length * 0.65f);
    }
    void OnDestroy() { if (material != null) Destroy(material); }
}
