using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// Shared, non-interactive presentation for the two reward sources and the ending.
public class FlightRewardPresentation : MonoBehaviour
{
    private static FlightRewardPresentation instance;
    private Image goldEdge, endingFade;
    private Texture2D edgeTexture, cloudTexture;
    private Sprite edgeSprite, cloudSprite;
    private float flash;
    public static float FlashIntensity => instance != null ? instance.flash : 0f;

    private static FlightRewardPresentation Get()
    {
        if (instance != null) return instance;
        var root = new GameObject("Flight Reward Presentation");
        instance = root.AddComponent<FlightRewardPresentation>();
        instance.Build();
        return instance;
    }
    private Image MakeImage(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var image = go.GetComponent<Image>();
        image.raycastTarget = false;
        return image;
    }
    private void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -1; // effects above the world and below the existing HUD/ending
        endingFade = MakeImage(transform, "Ending Fade");
        endingFade.color = Color.clear;
        goldEdge = MakeImage(transform, "Gold Reward Edge");
        edgeTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        edgeTexture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color[64 * 64];
        for (int y = 0; y < 64; y++)
        for (int x = 0; x < 64; x++)
        {
            float edge = Mathf.Max(Mathf.Abs((x + .5f) / 32f - 1), Mathf.Abs((y + .5f) / 32f - 1));
            float alpha = Mathf.SmoothStep(0, .65f, Mathf.InverseLerp(.68f, 1f, edge));
            pixels[y * 64 + x] = new Color(1f, .78f, .2f, alpha);
        }
        edgeTexture.SetPixels(pixels); edgeTexture.Apply(false, true);
        edgeSprite = Sprite.Create(edgeTexture, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f));
        goldEdge.sprite = edgeSprite;
        goldEdge.color = Color.clear;
        cloudTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        cloudTexture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 64; y++)
        for (int x = 0; x < 64; x++)
        {
            float d = new Vector2((x + .5f) / 64f - .5f, (y + .5f) / 64f - .5f).magnitude;
            Color c = d > .43f ? new Color(.85f, .7f, .25f) : Color.white;
            c.a = Mathf.Clamp01((.48f - d) * 64);
            pixels[y * 64 + x] = c;
        }
        cloudTexture.SetPixels(pixels); cloudTexture.Apply(false, true);
        cloudSprite = Sprite.Create(cloudTexture, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f), 32f);
    }
    public static void Flash() { Get().flash = 1f; }
    public static void SetEndingFade(float alpha) { Get().endingFade.color = new Color(0, 0, 0, Mathf.Clamp01(alpha)); }
    public static void ShowRocketThought(Transform player, Sprite icon, float duration)
    {
        if (player != null && icon != null) Get().StartCoroutine(Get().Thought(player, icon, duration));
    }
    private IEnumerator Thought(Transform player, Sprite icon, float duration)
    {
        var root = new GameObject("Rocket Thought Cue");
        root.transform.SetParent(player, false);
        root.transform.localPosition = new Vector3(1.35f, 2.4f, 0);
        var bubble = root.AddComponent<SpriteRenderer>();
        bubble.sprite = cloudSprite; bubble.sortingOrder = 100;
        var symbol = new GameObject("Rocket Thought Icon");
        symbol.transform.SetParent(root.transform, false);
        var renderer = symbol.AddComponent<SpriteRenderer>();
        renderer.sprite = icon; renderer.sortingOrder = 102;
        float size = Mathf.Max(icon.bounds.size.x, icon.bounds.size.y);
        symbol.transform.localScale = Vector3.one * (1.1f / Mathf.Max(.01f, size));
        symbol.transform.localPosition = -icon.bounds.center * symbol.transform.localScale.x;
        for (int i = 0; i < 2; i++)
        {
            var dot = new GameObject("Thought Dot"); dot.transform.SetParent(root.transform, false);
            dot.transform.localPosition = new Vector3(-.65f - i * .3f, -.65f - i * .3f, 0);
            dot.transform.localScale = Vector3.one * (.16f - i * .05f);
            var dotRenderer = dot.AddComponent<SpriteRenderer>();
            dotRenderer.sprite = cloudSprite; dotRenderer.sortingOrder = 100;
        }
        var thoughtRenderers = root.GetComponentsInChildren<SpriteRenderer>();
        float t = 0;
        while (t < duration && player != null)
        {
            t += Time.deltaTime;
            float show = Mathf.Min(Mathf.Clamp01(t / .15f), Mathf.Clamp01((duration - t) / .15f));
            root.transform.localScale = Vector3.one * Mathf.Lerp(.92f, 1f, Mathf.SmoothStep(0, 1, show));
            foreach (var sr in thoughtRenderers) sr.color = new Color(1, 1, 1, show);
            yield return null;
        }
        if (root != null) Destroy(root);
    }
    void Update()
    {
        flash = Mathf.MoveTowards(flash, 0f, Time.unscaledDeltaTime / .65f);
        if (goldEdge != null) goldEdge.color = new Color(1, 1, 1, Mathf.Sin(flash * Mathf.PI * .5f));
    }
    void OnDestroy()
    {
        if (instance == this) instance = null;
        if (edgeSprite != null) Destroy(edgeSprite);
        if (edgeTexture != null) Destroy(edgeTexture);
        if (cloudSprite != null) Destroy(cloudSprite);
        if (cloudTexture != null) Destroy(cloudTexture);
    }
}
