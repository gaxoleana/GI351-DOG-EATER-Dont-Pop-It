using UnityEngine;
using System.Collections;

public class Obstacle : MonoBehaviour
{
    public float speed = 12f;
    public Vector2 moveDirection = Vector2.left;
    public float lifeTime = 5f;
    [Header("Smoke Trail")]
    [Tooltip("ปล่อยควันที่ตำแหน่งเดียวกับเครื่องบินเมื่อเริ่มทำงาน")]
    public bool emitsSmoke = false;

    [Tooltip("อายุควันแบบสุ่ม (วินาที)")]
    public Vector2 smokeDurationRange = new Vector2(2f, 3f);

    [Tooltip("ตัวคูณแรงยกของผู้เล่นเมื่ออยู่ในควัน")]
    [Range(0.05f, 1f)]
    public float smokeLiftMultiplier = 0.08f;

    [Tooltip("Prefab ควันที่จะปล่อยตามหลังเครื่องบิน")]
    public GameObject smokePrefab;

    [Tooltip("ระยะห่างระหว่างควันแต่ละก้อน (วินาที)")]
    public float smokeSpawnInterval = 0.5f;

    private float smokeSpawnTimer;

    void Start()
    {
        Destroy(gameObject, lifeTime);

        if (emitsSmoke)
        {
            EmitSmoke();
            smokeSpawnTimer = smokeSpawnInterval;
        }
    }

    void Update()
    {
        transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);

        if (emitsSmoke)
        {
            smokeSpawnTimer -= Time.deltaTime;
            if (smokeSpawnTimer <= 0f)
            {
                EmitSmoke();
                smokeSpawnTimer = Mathf.Max(0.05f, smokeSpawnInterval);
            }
        }
    }

    private void EmitSmoke()
    {
        SmokeCloud.Create(smokePrefab, transform.position, smokeDurationRange, smokeLiftMultiplier);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. ถ้าชนโดนหมากฝรั่ง (ต้องตั้ง Tag ชิ้นส่วนหมากฝรั่งเป็น "Gum")
        if (other.CompareTag("Gum"))
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null && player.IsObstacleImmune)
            {
                Destroy(gameObject);
                return;
            }

            GumController gum = other.GetComponentInParent<GumController>();
            if (gum != null && gum.currentState == GumController.GumState.Normal)
            {
                Debug.Log("💥 โดนหมากฝรั่ง -> แตกแบบฟื้นตัวเร็ว!");
                gum.ForcePop(gum.fastStunDuration);
            }
            Destroy(gameObject);
        }
        // 2. ถ้าชนโดนผู้เล่น (ต้องตั้ง Tag ชิ้นส่วนคนเป็น "Player")
        else if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                if (player.IsObstacleImmune)
                {
                    Destroy(gameObject);
                    return;
                }

                Debug.Log("😵 โดนผู้เล่น -> มึน!");
                player.OnHitByObstacleBody(); // เรียกฟังก์ชันมึนที่คุณเตรียมไว้
            }
            Destroy(gameObject);
        }
    }
}

public class SmokeCloud : MonoBehaviour
{
    [Tooltip("ระยะเวลา Fade เข้าและ Fade ออกของควัน (วินาที)")]
    public float fadeDuration = 0.3f;

    private PlayerController playerInside;
    private float liftMultiplier;
    private CircleCollider2D trigger;

    public static void Create(GameObject smokePrefab, Vector3 position, Vector2 durationRange, float liftMultiplier)
    {
        Quaternion randomRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        GameObject smokeObject = smokePrefab != null
            ? Instantiate(smokePrefab, position, randomRotation)
            : new GameObject("Obstacle Smoke");

        smokeObject.transform.SetPositionAndRotation(position, randomRotation);

        smokeObject.transform.SetParent(null, true);

        Obstacle obstacleComponent = smokeObject.GetComponent<Obstacle>();
        if (obstacleComponent != null)
        {
            Destroy(obstacleComponent);
        }

        SmokeCloud smoke = smokeObject.GetComponent<SmokeCloud>();
        if (smoke == null)
        {
            smoke = smokeObject.AddComponent<SmokeCloud>();
        }
        smoke.Initialize(durationRange, liftMultiplier);
    }

    private void Initialize(Vector2 durationRange, float liftMultiplier)
    {
        this.liftMultiplier = Mathf.Clamp(liftMultiplier, 0.05f, 1f);
        float minimumDuration = Mathf.Min(durationRange.x, durationRange.y);
        float maximumDuration = Mathf.Max(durationRange.x, durationRange.y);
        float smokeLifetime = Random.Range(minimumDuration, maximumDuration);
        Destroy(gameObject, smokeLifetime);

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            StartCoroutine(FadeSpriteRoutine(spriteRenderer, smokeLifetime));
        }

        ParticleSystem particles = GetComponent<ParticleSystem>();
        if (particles == null && GetComponent<SpriteRenderer>() == null)
        {
            particles = gameObject.AddComponent<ParticleSystem>();
        }
        if (particles != null)
        {
            ParticleSystem.MainModule main = particles.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
            main.startColor = new Color(0.22f, 0.24f, 0.27f, 0.65f);
            main.maxParticles = 120;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 20f;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 2.5f;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.35f),
                new Keyframe(0.5f, 1f),
                new Keyframe(1f, 0.2f));
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 1;
            Shader smokeShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (smokeShader == null) smokeShader = Shader.Find("Particles/Standard Unlit");
            if (smokeShader == null) smokeShader = Shader.Find("Sprites/Default");
            if (smokeShader != null) renderer.material = new Material(smokeShader);
        }

        Collider2D[] prefabColliders = GetComponents<Collider2D>();
        for (int i = 0; i < prefabColliders.Length; i++)
        {
            if (!(prefabColliders[i] is CircleCollider2D))
            {
                prefabColliders[i].enabled = false;
            }
        }

        trigger = GetComponent<CircleCollider2D>();
        if (trigger == null)
        {
            trigger = gameObject.AddComponent<CircleCollider2D>();
        }
        trigger.isTrigger = true;
        trigger.radius = 2f;
    }

    private IEnumerator FadeSpriteRoutine(SpriteRenderer spriteRenderer, float smokeLifetime)
    {
        Color baseColor = spriteRenderer.color;
        float fadeTime = Mathf.Min(fadeDuration, smokeLifetime * 0.5f);
        float visibleTime = Mathf.Max(0f, smokeLifetime - (fadeTime * 2f));

        float timer = 0f;
        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            SetSpriteAlpha(spriteRenderer, baseColor, Mathf.Clamp01(timer / fadeTime));
            yield return null;
        }

        SetSpriteAlpha(spriteRenderer, baseColor, 1f);
        yield return new WaitForSeconds(visibleTime);

        timer = 0f;
        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            SetSpriteAlpha(spriteRenderer, baseColor, 1f - Mathf.Clamp01(timer / fadeTime));
            yield return null;
        }

        SetSpriteAlpha(spriteRenderer, baseColor, 0f);
    }

    private static void SetSpriteAlpha(SpriteRenderer spriteRenderer, Color baseColor, float alpha)
    {
        Color color = baseColor;
        color.a = baseColor.a * alpha;
        spriteRenderer.color = color;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null && playerInside == null)
        {
            playerInside = player;
            player.EnterSmoke(liftMultiplier);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        OnTriggerEnter2D(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null && player == playerInside)
        {
            player.ExitSmoke(liftMultiplier);
            playerInside = null;
        }
    }

    private void OnDestroy()
    {
        if (playerInside != null)
        {
            playerInside.ExitSmoke(liftMultiplier);
            playerInside = null;
        }
    }
}