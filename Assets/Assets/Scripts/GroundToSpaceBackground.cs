using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class GroundToSpaceBackground : MonoBehaviour
{
    [Header("Gradient Settings")]
    public Gradient gradient;
    public int textureHeight = 512; // ยิ่งสูง ยิ่งไล่สีเนียน
    public int textureWidth = 4;    // ไม่ต้องเยอะ เพราะไล่สีแนวตั้งเท่านั้น

    [Header("World Size")]
    public float worldHeight = 100f; // ความสูงจริงที่ background จะครอบคลุม (unit)
    public float worldWidth = 20f;

    private SpriteRenderer sr;
    private Texture2D tex;

    void OnEnable()
    {
        sr = GetComponent<SpriteRenderer>();
        Generate();
    }

    void Generate()
    {
        if (gradient == null) return;

        tex = new Texture2D(textureWidth, textureHeight);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < textureHeight; y++)
        {
            float t = (float)y / (textureHeight - 1);
            Color c = gradient.Evaluate(t);
            for (int x = 0; x < textureWidth; x++)
                tex.SetPixel(x, y, c);
        }
        tex.Apply();

        Sprite sprite = Sprite.Create(
            tex,
            new Rect(0, 0, textureWidth, textureHeight),
            new Vector2(0.5f, 0.5f),
            1f // pixels per unit
        );
        sr.sprite = sprite;

        // ยืด sprite ให้ครอบคลุมขนาดโลกที่ต้องการ
        transform.localScale = new Vector3(worldWidth, worldHeight, 1f);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // อัปเดตทันทีเมื่อแก้ค่าใน Inspector
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null)
            {
                sr = GetComponent<SpriteRenderer>();
                Generate();
            }
        };
    }
#endif
}