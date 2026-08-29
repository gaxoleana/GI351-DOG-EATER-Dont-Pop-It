using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    private enum EventType { None, RedSpam, BlueFreeze }

    [Header("Gravity Settings")]
    [SerializeField] private float inflateUpwardForce = 15f;
    [SerializeField] private float normalGravity = 3f;
    [SerializeField] private float inflatedGravity = 0.5f;
    [SerializeField] private float blueEventGravity = 0.15f; // ค่าแรงโน้มถ่วงตอนติดฟ้า (ค่อยๆ ร่วงช้ามาก)
    [SerializeField] private float blueMaxFallSpeed = 1f;    // จำกัดความเร็วตกไม่ให้หล่นเร็วเกินไป

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

    [Header("Gum Event Overlays")]
    [SerializeField] private Image redVignette;
    [SerializeField] private Image blueVignette; // เพิ่มช่องลาก UI ขอบจอฟ้า

    [Header("Gum Event Progress Bar")]
    [SerializeField] private GameObject panicBarBackground;
    [SerializeField] private Image panicBarFill;

    [Header("Event Timing & Difficulty")]
    [SerializeField] private float minTimeBetweenPanics = 8f;
    [SerializeField] private float maxTimeBetweenPanics = 18f;
    [SerializeField] private int requiredSpamClicks = 6;
    [SerializeField] private float redPanicTimeLimit = 2.5f;
    [SerializeField] private float blueFreezeDuration = 2.5f; // เวลาที่ต้องปล่อยมือห้ามกด

    // Essential Variables
    private Rigidbody2D rb;
    private float currentStamina;
    private bool isHoldingButton = false;
    private bool isExhausted = false;
    private bool isPopped = false;

    // Event State Variables
    private EventType currentEvent = EventType.None;
    private int currentSpamClicks = 0;
    private float eventTimer = 0f;

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

        bool inputDown = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
        bool inputPressed = Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space);

        // ---------------- 1. EVENT สีแดง (รัวปุ่มเพื่อแก้) ----------------
        if (currentEvent == EventType.RedSpam)
        {
            if (redVignette != null)
            {
                float alpha = Mathf.PingPong(Time.time * 6f, 0.6f) + 0.2f;
                redVignette.color = new Color(1f, 0f, 0f, alpha);
            }

            if (inputDown)
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

            eventTimer -= Time.deltaTime;
            if (eventTimer <= 0f)
            {
                PopBubble(); // กดไม่ทันเวลา = แตก
            }

            return;
        }
        // ---------------- 2. EVENT สีฟ้า (ห้ามกดเด็ดขาด) ----------------
        if (currentEvent == EventType.BlueFreeze)
        {
            if (blueVignette != null)
            {
                float alpha = Mathf.PingPong(Time.time * 4f, 0.5f) + 0.3f;
                blueVignette.color = new Color(0f, 0.5f, 1f, alpha);
            }

            // คำนวณเวลาที่ผ่านไปตั้งแต่เริ่ม Event ฟ้า
            float timePassedInEvent = blueFreezeDuration - eventTimer;

            // 1) ถ้าผู้เล่น "กดปุ่มใหม่" จังหวะนี้ = แตกทันที
            // 2) หรือถ้าผ่านช่วงเวลาเผื่อใจ (0.35 วินาทีแรก) ไปแล้วแต่นิ้วยัง "กดค้างไม่ยอมปล่อย" = แตกทันที
            if (inputDown || (timePassedInEvent > 1f && inputPressed))
            {
                PopBubble();
                return;
            }

            // --- เพิ่มการฟื้นฟู Stamina ขณะติดฟ้า ---
            if (currentStamina < maxStamina)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Min(currentStamina, maxStamina);
            }

            // อัปเดตหลอด Stamina UI ตามปกติ
            if (staminaBarFill != null)
            {
                staminaBarFill.fillAmount = currentStamina / maxStamina;
            }

            // หลอดเวลาสีฟ้าจะค่อยๆ ลดลงถอยหลัง
            eventTimer -= Time.deltaTime;
            if (panicBarFill != null)
            {
                panicBarFill.fillAmount = eventTimer / blueFreezeDuration;
            }

            if (eventTimer <= 0f)
            {
                ResolvePanicSuccess(); // อดทนไม่กดจนครบเวลา = ผ่าน
            }

            return;
        }
        // ---------------- 3. NORMAL GAMEPLAY ----------------
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
        if (isPopped)
        {
            rb.gravityScale = normalGravity;
            return;
        }

        // 1. ถ้าติด Event สีฟ้า -> ค่อยๆ ร่วงช้าๆ แบบนุ่มนวล
        if (currentEvent == EventType.BlueFreeze)
        {
            rb.gravityScale = blueEventGravity;

            // ล็อกความเร็วร่วง ไม่ให้ตกลงมาเร็วเกินเพดานที่ตั้งไว้

            if (rb.linearVelocity.y < -blueMaxFallSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -blueMaxFallSpeed);
            }

            if (rb.linearVelocity.y < -blueMaxFallSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -blueMaxFallSpeed);
            }

        }
        // 2. ถ้าติด Event สีแดง หรือ กำลังกดค้างลอยตัวปกติ
        else if (isHoldingButton || currentEvent == EventType.RedSpam)
        {
            rb.gravityScale = inflatedGravity;

            if (isHoldingButton && currentEvent == EventType.None)
            {
                rb.AddForce(Vector2.up * inflateUpwardForce, ForceMode2D.Force);
            }
        }
        // 3. ปล่อยปุ่มช่วงปกติ
        else
        {
            rb.gravityScale = normalGravity;
        }
    }

    private void PopBubble()
    {
        isPopped = true;
        currentEvent = EventType.None;
        isHoldingButton = false;

        currentStamina = 0f;
        if (staminaBarFill != null) staminaBarFill.fillAmount = 0f;

        if (redVignette != null) redVignette.color = new Color(1f, 0f, 0f, 0f);
        if (blueVignette != null) blueVignette.color = new Color(0f, 0.5f, 1f, 0f);
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

            if (currentEvent == EventType.None && !isPopped)
            {
                // สุ่ม 50/50 ว่าจะเป็นสีแดง หรือ สีฟ้า
                int rand = Random.Range(0, 2);
                if (rand == 0)
                {
                    TriggerRedEvent();
                }
                else
                {
                    TriggerBlueEvent();
                }
            }
        }
    }

    private void TriggerRedEvent()
    {
        currentEvent = EventType.RedSpam;
        currentSpamClicks = 0;
        eventTimer = redPanicTimeLimit;

        if (panicBarFill != null) panicBarFill.color = Color.red;
        if (panicBarBackground != null) panicBarBackground.SetActive(true);
        if (panicBarFill != null) panicBarFill.fillAmount = 0f;
    }

    private void TriggerBlueEvent()
    {
        currentEvent = EventType.BlueFreeze;
        eventTimer = blueFreezeDuration;

        // ล้างความเร็วตกเดิม เพื่อให้เริ่มร่อนลงช้าๆ นุ่มนวล
        rb.linearVelocity = Vector2.zero;
        rb.linearVelocity = Vector2.zero;

        if (panicBarFill != null) panicBarFill.color = Color.cyan;
        if (panicBarBackground != null) panicBarBackground.SetActive(true);
        if (panicBarFill != null) panicBarFill.fillAmount = 1f;
    }

    private void ResolvePanicSuccess()
    {
        currentEvent = EventType.None;

        if (redVignette != null) redVignette.color = new Color(1f, 0f, 0f, 0f);
        if (blueVignette != null) blueVignette.color = new Color(0f, 0.5f, 1f, 0f);
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