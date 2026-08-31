using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    private enum EventType { None, RedSpam, BlueFreeze }

    [Header("Camera Zoom Settings")]
    [SerializeField] private CinemachineCamera cinemachineCam;
    [SerializeField] private float minCamSize = 5f;  // Orthographic size when bubble is at min scale
    [SerializeField] private float maxCamSize = 8f;  // Orthographic size when bubble is fully blown
    [SerializeField] private float zoomSpeed = 4f;


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

    [Header("Pre-Event Warning Settings")]
    [SerializeField] private Image redWarningImage;   // รูป PNG เตือนก่อนเกิด Red Event (ต้องเป็น UI Image บน Canvas)
    [SerializeField] private Image blueWarningImage;  // รูป PNG เตือนก่อนเกิด Blue Event (ต้องเป็น UI Image บน Canvas)
    [SerializeField] private Vector2 warningStartAnchoredPos = new Vector2(-500f, 0f); // ตำแหน่งเริ่ม (นอกจอฝั่งซ้าย)
    [SerializeField] private Vector2 warningEndAnchoredPos = new Vector2(300f, 0f);    // ตำแหน่งปลายทาง (ฝั่งขวาของจอ)
    [SerializeField] private float warningFadeInDuration = 0.4f;
    [SerializeField] private float warningHoldDuration = 1.5f;
    [SerializeField] private float warningFadeOutDuration = 0.4f;

    // Essential Variables
    private Rigidbody2D rb;
    private float currentStamina;
    private bool isHoldingButton = false;
    private bool isExhausted = false;
    private bool isPopped = false;
    private bool isPreEventWarning = false; // true ระหว่างที่กำลังเล่น Warning ก่อน Event (หยุดทุกอย่างชั่วคราว)

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
        if (redWarningImage != null) redWarningImage.gameObject.SetActive(false);
        if (blueWarningImage != null) blueWarningImage.gameObject.SetActive(false);
        StartCoroutine(RandomPanicRoutine());
    }

    private void Update()
    {
        if (isPopped) return;

        UpdateCameraZoom();

        if (isPreEventWarning) return; // ระหว่าง Warning ห้ามรับ Input หรือขยับใด ๆ ทั้งสิ้น

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

                if (TryGetComponent<CinemachineImpulseSource>(out var impulse))
                {
                    impulse.GenerateImpulse(0.15f);
                }

                if (panicBarFill != null)
                {
                    panicBarFill.fillAmount = (float)currentSpamClicks / requiredSpamClicks;
                }

                if (bubbleTransform != null) bubbleTransform.localScale *= 1.01f;

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

        if (isPreEventWarning)
        {
            // แช่แข็งผู้เล่นไว้กับที่ระหว่างเล่น Warning กันตกหรือลอยเพี้ยนก่อน Event จะเริ่ม
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
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
        if (TryGetComponent<CinemachineImpulseSource>(out var impulse))
        {
            impulse.GenerateImpulse(1.0f);
        }

        isPopped = true;
        currentEvent = EventType.None;
        isHoldingButton = false;
        isPreEventWarning = false;

        currentStamina = 0f;
        if (staminaBarFill != null) staminaBarFill.fillAmount = 0f;

        if (redVignette != null) redVignette.color = new Color(1f, 0f, 0f, 0f);
        if (blueVignette != null) blueVignette.color = new Color(0f, 0.5f, 1f, 0f);
        if (panicBarBackground != null) panicBarBackground.SetActive(false);
        if (redWarningImage != null) redWarningImage.gameObject.SetActive(false);
        if (blueWarningImage != null) blueWarningImage.gameObject.SetActive(false);

        if (bubbleTransform != null)
        {
            bubbleTransform.gameObject.SetActive(false);
        }

        // Trigger game over immediately on pop
        Die();
    }

    private IEnumerator RandomPanicRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(minTimeBetweenPanics, maxTimeBetweenPanics);
            yield return new WaitForSeconds(waitTime);

            if (currentEvent == EventType.None && !isPopped && !isPreEventWarning)
            {
                // สุ่ม 50/50 ว่าจะเป็นสีแดง หรือ สีฟ้า
                int rand = Random.Range(0, 2);
                EventType nextEvent = (rand == 0) ? EventType.RedSpam : EventType.BlueFreeze;

                StartCoroutine(PlayWarningThenTriggerEvent(nextEvent));
            }
        }
    }

    // เล่นภาพเตือนก่อน แล้วค่อยเริ่ม Event จริงหลัง Warning จบ
    private IEnumerator PlayWarningThenTriggerEvent(EventType eventToTrigger)
    {
        isPreEventWarning = true;

        // หยุดความเร็วตกทันทีตอนเริ่ม Warning กันบัคตกค้างจากเฟรมก่อนหน้า
        if (rb != null) rb.linearVelocity = Vector2.zero;

        Image warningImage = (eventToTrigger == EventType.RedSpam) ? redWarningImage : blueWarningImage;

        if (warningImage != null)
        {
            yield return StartCoroutine(PlayWarningAnimation(warningImage));
        }
        else
        {
            // ไม่ได้ผูกรูป Warning ไว้ ก็รอเวลาเทียบเท่าแทน กัน Event โผล่มาแบบไม่ทันตั้งตัว
            yield return new WaitForSeconds(warningFadeInDuration + warningHoldDuration + warningFadeOutDuration);
        }

        isPreEventWarning = false;

        if (eventToTrigger == EventType.RedSpam)
        {
            TriggerRedEvent();
        }
        else
        {
            TriggerBlueEvent();
        }
    }

    // Fade In พร้อมเลื่อนจากซ้ายไปขวา -> ค้างไว้ -> Fade Out (ควบคุมด้วยโค้ดล้วน ไม่ใช้ Animation)
    private IEnumerator PlayWarningAnimation(Image warningImage)
    {
        RectTransform rt = warningImage.rectTransform;

        warningImage.gameObject.SetActive(true);

        Color c = warningImage.color;
        rt.anchoredPosition = warningStartAnchoredPos;
        c.a = 0f;
        warningImage.color = c;

        // 1) Fade In + เลื่อนจากซ้ายไปขวาพร้อมกัน
        float t = 0f;
        while (t < warningFadeInDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / warningFadeInDuration);

            rt.anchoredPosition = Vector2.Lerp(warningStartAnchoredPos, warningEndAnchoredPos, progress);
            c.a = progress;
            warningImage.color = c;

            yield return null;
        }

        rt.anchoredPosition = warningEndAnchoredPos;
        c.a = 1f;
        warningImage.color = c;

        // 2) ค้างภาพไว้ตามเวลาที่กำหนด
        yield return new WaitForSeconds(warningHoldDuration);

        // 3) Fade Out ออกไป (ค้างตำแหน่งเดิม)
        t = 0f;
        while (t < warningFadeOutDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / warningFadeOutDuration);

            c.a = 1f - progress;
            warningImage.color = c;

            yield return null;
        }

        c.a = 0f;
        warningImage.color = c;
        warningImage.gameObject.SetActive(false);
    }

    private void TriggerRedEvent()
    {
        currentEvent = EventType.RedSpam;
        currentSpamClicks = 0;
        eventTimer = redPanicTimeLimit;

        if (TryGetComponent<CinemachineImpulseSource>(out var impulse))
        {
            // Force explicit X and Y shake direction
            impulse.GenerateImpulse(new Vector3(0.5f, 0.5f, 0f));
        }

        if (panicBarFill != null) panicBarFill.color = Color.red;
        if (panicBarBackground != null) panicBarBackground.SetActive(true);
        if (panicBarFill != null) panicBarFill.fillAmount = 0f;
    }

    private void TriggerBlueEvent()
    {
        currentEvent = EventType.BlueFreeze;
        eventTimer = blueFreezeDuration;

        if (TryGetComponent<CinemachineImpulseSource>(out var impulse))
        {
            // Force explicit X and Y shake direction
            impulse.GenerateImpulse(new Vector3(0.5f, 0.5f, 0f));
        }

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

    private void UpdateCameraZoom()
    {
        if (cinemachineCam == null || bubbleTransform == null) return;

        // 1. Get real-time bubble radius (half of current X scale)
        float bubbleRadius = bubbleTransform.localScale.x * 0.5f;

        // 2. Set camera orthographic size to bubble radius + 10% padding border
        float targetCamSize = bubbleRadius * 0.15f;

        // 3. Ensure camera never zooms in tighter than your minimum setting
        targetCamSize = Mathf.Max(minCamSize, targetCamSize);

        // 4. Lerp camera size to match gum expansion smoothly
        cinemachineCam.Lens.OrthographicSize = Mathf.Lerp(
            cinemachineCam.Lens.OrthographicSize,
            targetCamSize,
            Time.deltaTime * zoomSpeed
        );
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