using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BubbleGumPlayer : MonoBehaviour
{
    [Header("Upward Lift & Gravity")]
    [SerializeField] private float inflateUpwardForce = 15f;
    [SerializeField] private float normalGravity = 3f;
    [SerializeField] private float inflatedGravity = 0.5f;

    [Header("Bubble Scale Settings")]
    [SerializeField] private Transform bubbleTransform;
    [SerializeField] private Vector3 minBubbleScale = new Vector3(0.5f, 0.5f, 1f);
    [SerializeField] private Vector3 maxBubbleScale = new Vector3(2.5f, 2.5f, 1f);
    [SerializeField] private float scaleSpeed = 4f;

    private Rigidbody2D rb;
    private bool isHoldingButton = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        isHoldingButton = Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space);

        if (bubbleTransform != null)
        {
            Vector3 targetScale = isHoldingButton ? maxBubbleScale : minBubbleScale;
            bubbleTransform.localScale = Vector3.Lerp(bubbleTransform.localScale, targetScale, Time.deltaTime * scaleSpeed);
        }
    }

    private void FixedUpdate()
    {
        if (isHoldingButton)
        {
            rb.gravityScale = inflatedGravity;

            rb.AddForce(Vector2.up * inflateUpwardForce, ForceMode2D.Force);
        }
        else
        {
            rb.gravityScale = normalGravity;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("DeadZone"))
        {
            Die();
        }
    }

    private void Die()
    {
        GetComponent<Rigidbody2D>().simulated = false;

        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.TriggerGameOver();
        }
    }
}