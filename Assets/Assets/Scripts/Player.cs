using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class BubbleGumPlayer : MonoBehaviour
{
    [Header("Gravity Settings")]
    [SerializeField] private float inflateUpwardForce = 15f;
    [SerializeField] private float normalGravity = 3f;
    [SerializeField] private float inflatedGravity = 0.5f;

    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDrainRate = 10f;
    [SerializeField] private float staminaRegenRate = 15f;
    [SerializeField] private Image staminaBarFill;

    [Header("Gum Settings")]
    [SerializeField] private Transform bubbleTransform;
    [SerializeField] private Vector3 minBubbleScale = new Vector3(0.5f, 0.5f, 1f);
    [SerializeField] private Vector3 maxBubbleScale = new Vector3(2.5f, 2.5f, 1f);
    [SerializeField] private float scaleSpeed = 4f;

    private Rigidbody2D rb;
    private float currentStamina;
    private bool isHoldingButton = false;
    private bool isExhausted = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentStamina = maxStamina;
    }

    private void Update()
    {
        bool inputPressed = Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space);

        if (isExhausted && currentStamina > maxStamina * 0.1f)
        {
            isExhausted = false;
        }

        if (inputPressed && !isExhausted)
        {
            isHoldingButton = true;
            currentStamina -= staminaDrainRate * Time.deltaTime;

            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                isHoldingButton = false;
                isExhausted = true;
            }
        }
        else
        {
            isHoldingButton = false;

            if (currentStamina < maxStamina)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Min(currentStamina, maxMaxStamina(maxStamina));
            }
        }

        if (staminaBarFill != null)
        {
            staminaBarFill.fillAmount = currentStamina / maxStamina;
        }

        if (bubbleTransform != null)
        {
            Vector3 targetScale = isHoldingButton ? maxBubbleScale : minBubbleScale;
            bubbleTransform.localScale = Vector3.Lerp(bubbleTransform.localScale, targetScale, Time.deltaTime * scaleSpeed);
        }
    }

    private float maxMaxStamina(float max) => max;

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