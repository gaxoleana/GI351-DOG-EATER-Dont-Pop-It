using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FallSpeedlineController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("ลาก Rigidbody2D ของ Player มาใส่ หรือปล่อยว่างเพื่อ auto-find")]
    [SerializeField] private Rigidbody2D playerRb;

    [Header("Speed Thresholds")]
    [Tooltip("ความเร็วร่วงที่เริ่มเห็น speedline แผ่ว ๆ")]
    [SerializeField] private float minVisibleFallSpeed = -6f;

    [Tooltip("ความเร็วร่วงที่ speedline ชัดเต็มที่")]
    [SerializeField] private float maxFallSpeedForFullVisible = -12f;

    [Header("Alpha Settings")]
    [Range(0f, 1f)]
    public float maxAlpha = 0.8f;

    [Tooltip("ความเร็วในการ fade เข้า/ออก")]
    public float fadeSmoothSpeed = 4f;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        SetAlpha(0f);
    }

    private void Start()
    {
        if (playerRb == null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                playerRb = player.GetComponent<Rigidbody2D>();
            }
        }
    }

    private void Update()
    {
        if (playerRb == null) return;

        float velocityY = playerRb.linearVelocity.y;
        float targetAlpha = 0f;

        if (velocityY < minVisibleFallSpeed)
        {
            float ratio = Mathf.InverseLerp(
                minVisibleFallSpeed,
                maxFallSpeedForFullVisible,
                velocityY);
            targetAlpha = Mathf.Lerp(0f, maxAlpha, ratio);
        }

        float currentAlpha = Mathf.Lerp(
            spriteRenderer.color.a,
            targetAlpha,
            Time.deltaTime * fadeSmoothSpeed);
        SetAlpha(currentAlpha);
    }

    private void SetAlpha(float alpha)
    {
        Color color = spriteRenderer.color;
        color.a = alpha;
        spriteRenderer.color = color;
    }
}
