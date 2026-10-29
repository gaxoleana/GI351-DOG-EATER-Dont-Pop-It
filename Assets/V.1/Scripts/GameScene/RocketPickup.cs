using UnityEngine;
using System.Collections;

public class RocketPickup : MonoBehaviour
{
    public float moveSpeed = 8f;
    public float rotationSpeedY = 180f;
    public float boostHeight = 100f;
    public float boostDuration = 2.5f;
    public float lifeTime = 8f;

    private Vector2 moveDirection;
    private Vector3 baseScale;
    private SpriteRenderer medal;
    private SpriteRenderer emblem;
    private LineRenderer halo;
    private TrailRenderer trail;
    private Material glowMaterial;
    private Texture2D medalTexture;
    private Sprite medalSprite;
    private float age;
    private bool collected;

    private void Start()
    {
        baseScale = transform.localScale;
        medal = GetComponent<SpriteRenderer>();
        int order = medal != null ? medal.sortingOrder : 0;
        int layer = medal != null ? medal.sortingLayerID : 0;
        // Keep the rocket as an inset symbol; the outer silhouette is a collectible medal.
        if (medal != null)
        {
            Sprite original = medal.sprite;
            var emblemObject = new GameObject("Rocket Emblem");
            emblemObject.transform.SetParent(transform, false);
            emblem = emblemObject.AddComponent<SpriteRenderer>();
            emblem.sprite = original;
            emblem.flipX = medal.flipX;
            emblem.sortingLayerID = layer;
            emblem.sortingOrder = order + 1;
            if (original != null)
            {
                float size = Mathf.Max(original.bounds.size.x, original.bounds.size.y);
                emblemObject.transform.localScale = Vector3.one * (1.05f / Mathf.Max(0.01f, size));
                emblemObject.transform.localPosition = -original.bounds.center * emblemObject.transform.localScale.x;
            }
            CreateMedal();
            medal.sprite = medalSprite;
            medal.flipX = false;
            medal.color = Color.white;
        }
        glowMaterial = new Material(Shader.Find("Sprites/Default"));
        var glowObject = new GameObject("Reward Glow");
        glowObject.transform.SetParent(transform, false);
        halo = glowObject.AddComponent<LineRenderer>();
        halo.sharedMaterial = glowMaterial;
        halo.useWorldSpace = true;
        halo.loop = true;
        halo.positionCount = 48;
        halo.widthMultiplier = 0.045f;
        halo.startColor = halo.endColor = new Color(1f, 0.95f, 0.65f, 0.6f);
        halo.sortingLayerID = layer;
        halo.sortingOrder = order - 1;
        trail = glowObject.AddComponent<TrailRenderer>();
        trail.sharedMaterial = glowMaterial;
        trail.time = 0.35f;
        trail.minVertexDistance = 0.08f;
        trail.startWidth = 0.18f;
        trail.endWidth = 0f;
        trail.startColor = new Color(1f, 0.85f, 0.3f, 0.5f);
        trail.endColor = new Color(1f, 0.85f, 0.3f, 0f);
        trail.sortingLayerID = layer;
        trail.sortingOrder = order - 2;
    }

    private void CreateMedal()
    {
        const int size = 96;
        medalTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        medalTexture.filterMode = FilterMode.Bilinear;
        medalTexture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector2 p = new Vector2((x + 0.5f) / size - 0.5f, (y + 0.5f) / size - 0.5f);
            float d = p.magnitude;
            Color c = Color.Lerp(new Color(1f, 0.66f, 0.1f), new Color(1f, 0.95f, 0.5f), y / (float)size);
            if (d > 0.405f) c = new Color(1f, 0.95f, 0.68f);
            if (d > 0.455f) c = new Color(0.72f, 0.39f, 0.07f);
            c.a = Mathf.Clamp01((0.48f - d) * size);
            pixels[y * size + x] = c;
        }
        medalTexture.SetPixels(pixels);
        medalTexture.Apply(false, true);
        medalSprite = Sprite.Create(medalTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 48f);
    }

    private void OnDestroy()
    {
        if (glowMaterial != null) Destroy(glowMaterial);
        if (medalSprite != null) Destroy(medalSprite);
        if (medalTexture != null) Destroy(medalTexture);
    }

    public void Initialize(Vector2 direction)
    {
        moveDirection = direction.normalized;
        Destroy(gameObject, lifeTime);
    }

    private void DrawGlow(float radius)
    {
        if (halo == null) return;
        float worldRadius = radius * Mathf.Max(Mathf.Abs(baseScale.x), Mathf.Abs(baseScale.y));
        for (int i = 0; i < halo.positionCount; i++)
        {
            float angle = i / (float)halo.positionCount * Mathf.PI * 2f;
            halo.SetPosition(i, transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * worldRadius);
        }
    }

    private void Update()
    {
        if (collected) return;
        transform.position += (Vector3)(moveDirection * moveSpeed * Time.deltaTime);
        age += Time.deltaTime;
        float breath = Mathf.Sin(age * 1.6f);
        transform.localScale = baseScale * (1f + breath * 0.015f);
        DrawGlow(1.05f + breath * 0.02f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;
        collected = true;
        foreach (var collider in GetComponents<Collider2D>()) collider.enabled = false;
        player.StartRocketBoost(boostHeight, boostDuration);
        StartCoroutine(CollectionBloom());
    }

    private IEnumerator CollectionBloom()
    {
        if (trail != null) trail.emitting = false;
        float t = 0f;
        while (t < 0.28f)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / 0.28f);
            float eased = 1f - (1f - progress) * (1f - progress);
            transform.localScale = baseScale * Mathf.Lerp(1f, 1.35f, eased);
            if (medal != null) medal.color = new Color(1f, 1f, 1f, 1f - progress);
            if (emblem != null) emblem.color = new Color(1f, 1f, 1f, 1f - progress);
            if (halo != null) halo.startColor = halo.endColor = new Color(1f, 0.94f, 0.5f, 1f - progress);
            DrawGlow(Mathf.Lerp(1.05f, 1.8f, eased));
            yield return null;
        }
        Destroy(gameObject);
    }
}
