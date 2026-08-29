using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
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

    [Header("Gum Panic Event")]
    [SerializeField] private Image redVignette;
    [SerializeField] private GameObject panicBarBackground;
    [SerializeField] private Image panicBarFill;
    [SerializeField] private float minTimeBetweenPanics = 8f;
    [SerializeField] private float maxTimeBetweenPanics = 18f;
    [SerializeField] private int requiredSpamClicks = 6;
    [SerializeField] private float panicTimeLimit = 2.5f;

    // Essential Variables
    private Rigidbody2D rb;
    private float currentStamina;
    private bool isHoldingButton = false;
    private bool isExhausted = false;
    private bool isPopped = false;

    // Panic State Variables
    private bool inPanicState = false;
    private int currentSpamClicks = 0;
    private float panicTimer = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentStamina = maxStamina;
    }

    private void Start()
    {
        if (panicBarBackground != null) panicBarBackground.SetActive(false);
        StartCoroutine(RandomPanicRoutine());
    }

    private void Update()
    {
        if (isPopped) return;

        if (inPanicState)
        {
            if (redVignette != null)
            {
                float alpha = Mathf.PingPong(Time.time * 6f, 0.6f) + 0.2f;
                redVignette.color = new Color(1f, 0f, 0f, alpha);
            }

            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            {
                currentSpamClicks++;

                if (panicBarFill != null)
                {
                    panicBarFill.fillAmount = (float)currentSpamClicks / requiredSpamClicks;
                }

                if (bubbleTransform != null) bubbleTransform.localScale *= 1.05f;

                if (currentSpamClicks >= requiredSpamClicks)
                {
                    ResolvePanicSuccess();
                }
            }

            panicTimer -= Time.deltaTime;
            if (panicTimer <= 0f)
            {
                PopBubble();
            }

            return;
        }

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
                currentStamina = Mathf.Min(currentStamina, maxStamina);
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

    private void FixedUpdate()
    {
        if ((isHoldingButton || inPanicState) && !isPopped)
        {
            rb.gravityScale = inflatedGravity;

            if (isHoldingButton && !inPanicState)
            {
                rb.AddForce(Vector2.up * inflateUpwardForce, ForceMode2D.Force);
            }
        }
        else
        {
            rb.gravityScale = normalGravity;
        }
    }

    private void PopBubble()
    {
        isPopped = true;
        inPanicState = false;
        isHoldingButton = false;

        currentStamina = 0f;
        if (staminaBarFill != null) staminaBarFill.fillAmount = 0f;

        if (redVignette != null) redVignette.color = new Color(1f, 0f, 0f, 0f);
        if (panicBarBackground != null) panicBarBackground.SetActive(false);

        if (bubbleTransform != null)
        {
            bubbleTransform.gameObject.SetActive(false);
        }
    }

    private IEnumerator RandomPanicRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(minTimeBetweenPanics, maxTimeBetweenPanics);
            yield return new WaitForSeconds(waitTime);

            if (!inPanicState && !isPopped)
            {
                TriggerPanicEvent();
            }
        }
    }

    private void TriggerPanicEvent()
    {
        inPanicState = true;
        currentSpamClicks = 0;
        panicTimer = panicTimeLimit;

        if (panicBarBackground != null) panicBarBackground.SetActive(true);
        if (panicBarFill != null) panicBarFill.fillAmount = 0f;
    }

    private void ResolvePanicSuccess()
    {
        inPanicState = false;

        if (redVignette != null) redVignette.color = new Color(1f, 0f, 0f, 0f);
        if (panicBarBackground != null) panicBarBackground.SetActive(false);
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
        rb.simulated = false;

        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.TriggerGameOver();
        }
    }
}