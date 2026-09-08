using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns stars after the player reaches a configured altitude.
/// Each star pulses its scale briefly, then fades away.
/// </summary>
public class StarSpawner : MonoBehaviour
{
    [Header("References")]
    public Transform playerTransform;
    public GameObject starPrefab;

    [Header("Altitude")]
    [Tooltip("100 world units = 1 km. 2000 units = 20 km.")]
    public float startAltitude = 2000f;

    [Header("Spawn Timing")]
    public float spawnIntervalMin = 0.8f;
    public float spawnIntervalMax = 1.8f;

    [Header("Spawn Position")]
    public float spawnXRangeMin = -7f;
    public float spawnXRangeMax = 7f;
    public float spawnAheadDistance = 10f;

    [Header("Star Animation")]
    public float pulseDuration = 1.2f;
    public float pulseScaleMultiplier = 1.35f;
    public float fadeDuration = 1.2f;

    private float startY;
    private float nextSpawnTimer;

    private void Start()
    {
        if (playerTransform == null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null) playerTransform = player.transform;
        }

        if (playerTransform == null)
        {
            Debug.LogWarning("[StarSpawner] Player Transform is not assigned.", this);
            return;
        }

        startY = playerTransform.position.y;
        ScheduleNextSpawn();
    }

    private void Update()
    {
        if (playerTransform == null || starPrefab == null) return;

        float altitude = Mathf.Max(0f, playerTransform.position.y - startY);
        if (altitude < startAltitude) return;

        nextSpawnTimer -= Time.deltaTime;
        if (nextSpawnTimer <= 0f)
        {
            SpawnStar();
            ScheduleNextSpawn();
        }
    }

    private void ScheduleNextSpawn()
    {
        nextSpawnTimer = Random.Range(spawnIntervalMin, spawnIntervalMax);
    }

    private void SpawnStar()
    {
        float spawnX = playerTransform.position.x + Random.Range(spawnXRangeMin, spawnXRangeMax);
        float spawnY = playerTransform.position.y + spawnAheadDistance;
        GameObject star = Instantiate(starPrefab, new Vector3(spawnX, spawnY, 0f), Quaternion.identity, transform);

        StarFadeEffect effect = star.AddComponent<StarFadeEffect>();
        effect.pulseDuration = pulseDuration;
        effect.pulseScaleMultiplier = pulseScaleMultiplier;
        effect.fadeDuration = fadeDuration;
    }
}

public class StarFadeEffect : MonoBehaviour
{
    public float pulseDuration = 1.2f;
    public float pulseScaleMultiplier = 1.35f;
    public float fadeDuration = 1.2f;

    private Vector3 originalScale;
    private SpriteRenderer[] renderers;

    private void Start()
    {
        originalScale = transform.localScale;
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        StartCoroutine(AnimateStar());
    }

    private IEnumerator AnimateStar()
    {
        float timer = 0f;
        while (timer < pulseDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / pulseDuration);
            float pulse = 1f + Mathf.Sin(progress * Mathf.PI * 2f) * (pulseScaleMultiplier - 1f);
            transform.localScale = originalScale * pulse;
            yield return null;
        }

        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(timer / fadeDuration);
            SetAlpha(alpha);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void SetAlpha(float alpha)
    {
        if (renderers == null) return;

        foreach (SpriteRenderer spriteRenderer in renderers)
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
    }
}