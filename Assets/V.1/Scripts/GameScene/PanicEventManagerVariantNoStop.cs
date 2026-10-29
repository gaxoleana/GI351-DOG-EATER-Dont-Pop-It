using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine; // Cinemachine 3.x Namespace

/// <summary>
/// สุ่มและจัดการ Panic Events สองรูปแบบ (Red = กดรัว, Blue = ห้ามกด)
/// หยุดเวลาเกม (Time.timeScale = 0) ชั่วคราวระหว่างช่วง Warning Phase
/// </summary>
public class PanicEventManagerVariantNoStop : MonoBehaviour
{
    public enum EventType
    {
        None,
        Red_Mash,      // ต้องกดปุ่มรัว ๆ ให้ครบเป้าหมาย
        Blue_HoldOff   // ห้ามกดปุ่มเด็ดขาดจนกว่าเวลาจะหมด
    }

    public enum EventState
    {
        Idle,       // รอสุ่มเวลาเกิด Event
        Warning,    // โชว์ UI เตือนล่วงหน้า (สั่งหยุดเวลาเกม)
        Active      // Event เริ่มทำงานจริง (ปลดล็อกเวลาเกม, ล็อก Normal Input)
    }

    [Header("References")]
    public PlayerController player;
    public GumController gum;

    [Header("World Overlay Settings (2D Sprite Dimmer)")]
    [Tooltip("GameObject สี่เหลี่ยมสีดำโปร่งแสงใน World Space")]
    public GameObject warningDimmerObject;

    [Header("Event Interval Settings")]
    [Tooltip("ระยะเวลาต่ำสุด-สูงสุด ระหว่างการสุ่มเกิด Panic Event แต่ละครั้ง (วินาที)")]
    public float minInterval = 8f;
    public float maxInterval = 15f;

    [Tooltip("เริ่มสุ่ม Panic Event หลังความสูงกี่เมตรขึ้นไป")]
    public float startAltitude = 100f;

    [Header("Warning Phase Settings")]
    [Tooltip("ระยะเวลาแสดง UI เตือนล่วงหน้ารวมทั้งหมด (วินาทีจริง)")]
    public float warningDuration = 1.5f;

    [Header("Warning Animation Settings")]
    [Tooltip("RectTransform ของป้าย Warning ที่จะทำอนิเมชันสไลด์")]
    public RectTransform warningRectTransform;

    [Tooltip("Image Component ของป้าย Warning")]
    public Image warningImage;

    [Tooltip("รูปภาพ Warning สำหรับ Red Event")]
    public Sprite redWarningSprite;

    [Tooltip("รูปภาพ Warning สำหรับ Blue Event")]
    public Sprite blueWarningSprite;

    [Tooltip("ระยะสไลด์ออกนอกจอฝั่งซ้าย/ขวา (Pixel หรือ UI Unit) เช่น 1200")]
    public float slideDistanceX = 1200f;

    [Tooltip("เวลาที่ใช้ในการสไลด์เข้าสู่กลางจอ (วินาทีจริง)")]
    public float slideInDuration = 0.3f;

    [Tooltip("เวลาที่ใช้ในการสไลด์ออกจากจอไปทางขวา (วินาทีจริง)")]
    public float slideOutDuration = 0.3f;

    [Header("Red Event (Mash) Settings")]
    [Tooltip("ระยะเวลาที่มีให้กดรัว (วินาที)")]
    public float redEventDuration = 2.5f;
    public float sizePerMash = 0.05f;

    [Tooltip("แรงยกตัวละครขึ้นสั้น ๆ ต่อการกด 1 ครั้งตอนกดรัว")]
    public float mashLiftImpulse = 2.5f;

    [Tooltip("จำนวนครั้งต่ำสุดในการกดรัว")]
    public int minTargetMashes = 3;

    [Tooltip("จำนวนครั้งสูงสุดในการกดรัว")]
    public int maxTargetMashes = 12;

    // Runtime Calculated ( Read Only )
    [HideInInspector]
    public int targetMashes; // เก็บจำนวนเป้าหมายที่สุ่มได้ในรอบนั้นๆ

    [Header("Blue Event (Hold Off) Settings")]
    [Tooltip("ระยะเวลาที่ต้องห้ามกดปุ่ม (วินาที)")]
    public float blueEventDuration = 1.5f;

    [Tooltip("ระยะเวลาต่ำสุดที่ต้องห้ามกดปุ่ม (วินาที)")]
    public float minBlueEventDuration = 1f;

    [Tooltip("ระยะเวลาสูงสุดที่ต้องห้ามกดปุ่ม (วินาที)")]
    public float maxBlueEventDuration = 2f;

    [Tooltip("ตัวคูณแรงโน้มถ่วงระหว่าง Blue Event (0 = ลอยค้าง, ค่าน้อยมาก = ตกช้ามาก)")]
    public float blueEventGravityMultiplier = 0.05f;

    [HideInInspector]
    public float currentBlueDuration; // เก็บเวลาที่สุ่มได้ในรอบนั้นๆ

    [Header("Active Event UI Feedback")]
    public GameObject redUIContainer;
    public Image redProgressBar;
    public TextMeshProUGUI redCountText;

    public GameObject blueUIContainer;
    public Image blueProgressBar;
    public TextMeshProUGUI blueTimerText;

    [Header("Camera Shake Settings (Cinemachine 3.1.7)")]
    [Tooltip("CinemachineCamera ที่มี CinemachineBasicMultiChannelPerlin ติดอยู่")]
    public CinemachineCamera vcam;

    [Tooltip("ความแรงของจอสั่นสม่ำเสมอตลอดช่วง Active Event")]
    public float activeShakeAmplitude = 1.0f;

    [Tooltip("ความแรงจอสั่นกระแทกเพิ่มเติมตอนกดปุ่มรัวใน Red Event")]
    public float mashShakePulse = 2.5f;

    [Tooltip("ความเร็วในการคืนค่าแรงสั่นกลับสู่ระดับปกติ")]
    public float shakeDamping = 5f;

    private CinemachineBasicMultiChannelPerlin noiseComponent;
    private float currentShakeAmplitude;

    [Header("Vignette Effect")]
    [Tooltip("ควบคุม Vignette ตอนเกิด Red/Blue Event")]
    public PanicVignetteController panicVignetteController;

    [Header("Runtime Status (Read Only)")]
    public EventState currentState = EventState.Idle;
    public EventType currentEvent = EventType.None;
    public float stateTimer;
    public int currentMashes;

    private float nextEventCooldown;
    private Vector2 defaultWarningPos;
    private Coroutine warningAnimCoroutine;
    private Rigidbody2D blueBody;
    private float blueGravityBefore;
    private bool blueInputLockedBefore;
    private bool blueOwnsPlayer;
    private bool blueOwnsClock;
    private float blueTimeScaleBefore;
    private float blueFixedDeltaBefore;
    private float blueSlowScale;
    [Range(0.1f, 1f)] public float blueWorldSpeed = 0.3f;

    public bool AlienBrainwaveActive { get; private set; }
    private float brainRemaining, brainPhaseRemaining, brainPhaseDuration;
    private bool brainInputBefore;
    private Image brainPortrait;
    private Image brainScreenTint;

    public bool TryStartAlienBrainwave(float duration, Sprite portrait, bool flipX)
    {
        if (!isActiveAndEnabled || currentState != EventState.Idle || player == null ||
            gum == null || gum.currentState != GumController.GumState.Normal || Time.timeScale <= 0f) return false;
        brainInputBefore = player.IsInputLocked;
        AlienBrainwaveActive = true;
        brainRemaining = Mathf.Clamp(duration, 1f, 2.5f);
        HideAllUI();
        if (portrait != null && warningRectTransform != null)
        {
            var tintObject = new GameObject("Alien Brainwave Screen Tint", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            brainScreenTint = tintObject.GetComponent<Image>();
            brainScreenTint.rectTransform.SetParent(warningRectTransform.parent, false);
            brainScreenTint.rectTransform.anchorMin = Vector2.zero;
            brainScreenTint.rectTransform.anchorMax = Vector2.one;
            brainScreenTint.rectTransform.offsetMin = brainScreenTint.rectTransform.offsetMax = Vector2.zero;
            brainScreenTint.raycastTarget = false;
            var go = new GameObject("Alien Brainwave Portrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            brainPortrait = go.GetComponent<Image>();
            brainPortrait.rectTransform.SetParent(warningRectTransform.parent, false);
            brainPortrait.rectTransform.anchorMin = brainPortrait.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            brainPortrait.rectTransform.sizeDelta = new Vector2(320f, 320f);
            brainPortrait.rectTransform.localScale = new Vector3(flipX ? -1f : 1f, 1f, 1f);
            brainPortrait.sprite = portrait;
            brainPortrait.preserveAspect = true;
            brainPortrait.raycastTarget = false;
        }
        SetBrainPhase(Random.value < 0.5f ? EventType.Red_Mash : EventType.Blue_HoldOff);
        return true;
    }

    private void ReleaseBrainBlue()
    {
        RestoreBlueClock();
        if (blueOwnsPlayer && blueBody != null) blueBody.gravityScale = blueGravityBefore;
        blueOwnsPlayer = false;
        blueBody = null;
    }

    private void SetBrainPhase(EventType type)
    {
        ReleaseBrainBlue();
        currentState = EventState.Active;
        currentEvent = type;
        currentMashes = 0;
        targetMashes = 1;
        // Every phase has enough time for a response; the last phase is never truncated.
        brainPhaseDuration = brainRemaining <= 0.75f ? brainRemaining : Random.Range(0.4f, Mathf.Min(0.65f, brainRemaining - 0.4f));
        brainPhaseRemaining = brainPhaseDuration;
        stateTimer = brainPhaseDuration;
        currentBlueDuration = brainPhaseDuration;
        if (type == EventType.Blue_HoldOff) PrepareBluePlayer();
        else player.SetInputLocked(true);
        if (redUIContainer != null) redUIContainer.SetActive(type == EventType.Red_Mash);
        if (blueUIContainer != null) blueUIContainer.SetActive(type == EventType.Blue_HoldOff);
        if (type == EventType.Blue_HoldOff)
        {
            if (blueTimerText != null) { blueTimerText.gameObject.SetActive(true); blueTimerText.enabled = true; }
            if (blueProgressBar != null) { blueProgressBar.gameObject.SetActive(true); blueProgressBar.enabled = true; blueProgressBar.fillAmount = 0f; }
        }
        if (panicVignetteController != null)
        {
            panicVignetteController.SetVignetteColor(type == EventType.Red_Mash ? Color.red : new Color(0.2f, 0.55f, 1f));
            panicVignetteController.SetVignetteActive(true);
        }
        if (brainScreenTint != null) brainScreenTint.color = type == EventType.Red_Mash
            ? new Color(1f, 0.08f, 0.14f, 0.14f) : new Color(0.08f, 0.45f, 1f, 0.14f);
        TriggerContinuousShake(type == EventType.Red_Mash ? activeShakeAmplitude : activeShakeAmplitude * 0.12f);
        UpdateRedUI();
    }

    private void UpdateBrainwave()
    {
        if (Time.timeScale <= 0f) return;
        float dt = Time.unscaledDeltaTime;
        brainRemaining = Mathf.Max(0f, brainRemaining - dt);
        brainPhaseRemaining = Mathf.Max(0f, brainPhaseRemaining - dt);
        stateTimer = brainPhaseRemaining;
        if (brainPortrait != null)
        {
            brainPortrait.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(Time.unscaledTime * 45f) * 5f, 180f);
            brainPortrait.color = Color.Lerp(Color.white, new Color(0.65f, 1f, 0.65f), 0.35f);
        }
        if (currentEvent == EventType.Red_Mash)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            {
                currentMashes++;
                gum.AddSize(sizePerMash);
                player.ApplyMashImpulse(mashLiftImpulse);
                AddShakePulse(mashShakePulse);
            }
            UpdateRedUI();
        }
        else
        {
            if (blueTimerText != null) blueTimerText.text = $"{stateTimer:0.0}s";
            if (blueProgressBar != null) blueProgressBar.fillAmount = 1f - stateTimer / brainPhaseDuration;
            SubmitBlueInput(Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space));
            if (!AlienBrainwaveActive) return;
        }
        if (brainPhaseRemaining > 0f) return;
        if (currentEvent == EventType.Red_Mash && currentMashes < targetMashes) { FailEvent(); return; }
        if (brainRemaining <= 0.001f) { EndEvent(true); return; }
        SetBrainPhase(currentEvent == EventType.Red_Mash ? EventType.Blue_HoldOff : EventType.Red_Mash);
    }

    private void BeginBlueSlowMotion()
    {
        if (blueOwnsClock || Time.timeScale <= 0f) return;
        blueTimeScaleBefore = Time.timeScale;
        blueFixedDeltaBefore = Time.fixedDeltaTime;
        float factor = Mathf.Clamp(blueWorldSpeed, 0.1f, 1f);
        blueSlowScale = blueTimeScaleBefore * factor;
        Time.timeScale = blueSlowScale;
        Time.fixedDeltaTime = blueFixedDeltaBefore * factor;
        blueOwnsClock = true;
    }

    private void RestoreBlueClock()
    {
        if (!blueOwnsClock) return;
        // Preserve an external pause while restoring the physics step.
        if (Time.timeScale > 0f) Time.timeScale = blueTimeScaleBefore;
        Time.fixedDeltaTime = blueFixedDeltaBefore;
        blueOwnsClock = false;
    }
    [Header("Blue Stability")]
    [Min(0f)] public float blueReleaseGrace = 0.25f;
    [Range(-1f, 0f)] public float blueDriftSpeed = -0.3f;

    private void PrepareBluePlayer()
    {
        if (blueOwnsPlayer || player == null) return;
        BeginBlueSlowMotion();
        blueInputLockedBefore = player.IsInputLocked;
        blueBody = player.GetComponent<Rigidbody2D>();
        if (blueBody != null)
        {
            blueGravityBefore = blueBody.gravityScale;
            blueBody.gravityScale = 0f;
            Vector2 v = blueBody.linearVelocity;
            v.y = Mathf.Clamp(v.y, -0.6f, 0f);
            blueBody.linearVelocity = v;
        }
        player.SetInputLocked(true);
        blueOwnsPlayer = true;
    }

    void FixedUpdate()
    {
        if (!blueOwnsPlayer || blueBody == null || currentEvent != EventType.Blue_HoldOff) return;
        Vector2 v = blueBody.linearVelocity;
        v.y = Mathf.MoveTowards(Mathf.Min(0f, v.y), blueDriftSpeed, 6f * Time.fixedDeltaTime);
        blueBody.linearVelocity = v;
    }

    private IEnumerator AnimateBlueWarning()
    {
        PrepareBluePlayer();
        if (panicVignetteController != null)
        {
            panicVignetteController.SetVignetteColor(new Color(0.2f, 0.55f, 1f));
            panicVignetteController.SetVignetteActive(true);
        }
        // Restore the authored blue warning before showing the active-event HUD.
        if (blueUIContainer != null) blueUIContainer.SetActive(false);
        if (warningDimmerObject != null) warningDimmerObject.SetActive(true);
        if (warningImage != null)
        {
            warningImage.sprite = blueWarningSprite;
            warningImage.enabled = true;
            warningImage.SetNativeSize();
        }
        Vector2 center = defaultWarningPos;
        Vector2 left = center - Vector2.right * slideDistanceX;
        Vector2 right = center + Vector2.right * slideDistanceX;
        if (warningRectTransform != null)
        {
            warningRectTransform.anchoredPosition = left;
            warningRectTransform.gameObject.SetActive(true);
        }
        float duration = Mathf.Max(0.25f, warningDuration);
        float enter = Mathf.Clamp(slideInDuration, 0f, duration * 0.5f);
        float exit = Mathf.Clamp(slideOutDuration, 0f, duration * 0.5f);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.timeScale > 0f ? Time.unscaledDeltaTime : 0f;
            if (warningRectTransform != null)
            {
                if (enter > 0f && elapsed < enter)
                    warningRectTransform.anchoredPosition = Vector2.Lerp(left, center, Mathf.SmoothStep(0f, 1f, elapsed / enter));
                else if (exit > 0f && elapsed > duration - exit)
                    warningRectTransform.anchoredPosition = Vector2.Lerp(center, right, Mathf.SmoothStep(0f, 1f, (elapsed - duration + exit) / exit));
                else
                    warningRectTransform.anchoredPosition = center;
            }
            yield return null;
        }
        if (warningRectTransform != null)
        {
            warningRectTransform.gameObject.SetActive(false);
            warningRectTransform.anchoredPosition = center;
        }
        if (warningDimmerObject != null) warningDimmerObject.SetActive(false);
        warningAnimCoroutine = null;
        StartActiveEvent();
    }

    private readonly List<Sprite> eventHudSprites = new List<Sprite>();

    private void StyleEventBar(Image bar)
    {
        if (bar == null || bar.sprite == null) return;
        Image frame = bar.transform.parent.GetComponent<Image>();
        if (frame == null || frame.sprite == null) return;

        // Slice the existing frame so its stroke stays even on every side.
        // Runtime sprites share the original textures; imported artwork stays untouched.
        Sprite frameSource = frame.sprite;
        Rect frameRect = frameSource.rect;
        Sprite frameSprite = Sprite.Create(frameSource.texture, frameRect, Vector2.one * 0.5f,
            frameSource.pixelsPerUnit, 0, SpriteMeshType.FullRect, new Vector4(32f, 32f, 32f, 32f));
        eventHudSprites.Add(frameSprite);
        frame.sprite = frameSprite;
        frame.type = Image.Type.Sliced;
        frame.pixelsPerUnitMultiplier = frameRect.height / frame.rectTransform.rect.height * 2.5f;

        // The full button artwork has its own thick frame. Use only its glossy
        // interior for the fill, leaving the single outer frame visible.
        Sprite fillSource = bar.sprite;
        Rect fillRect = fillSource.rect;
        fillRect = new Rect(fillRect.x + 32f, fillRect.y + 8f, fillRect.width - 64f, fillRect.height - 16f);
        Sprite fillSprite = Sprite.Create(fillSource.texture, fillRect, Vector2.one * 0.5f,
            fillSource.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        eventHudSprites.Add(fillSprite);
        bar.sprite = fillSprite;

        float inset = frame.rectTransform.rect.height * 0.055f;
        bar.rectTransform.localScale = Vector3.one;
        bar.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, frame.rectTransform.rect.width - inset * 2f);
        bar.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, frame.rectTransform.rect.height - inset * 2f);
    }

    private static void CenterEventLabel(TextMeshProUGUI label, Image bar)
    {
        if (label == null || bar == null) return;
        RectTransform labelRect = label.rectTransform;
        RectTransform barRect = bar.rectTransform;
        // Keep the authored uniform text scale and font material from the old HUD.
        // A sibling label avoids inheriting the fill's non-uniform scale or clipping.
        labelRect.SetParent(barRect.parent, false);
        labelRect.anchorMin = barRect.anchorMin;
        labelRect.anchorMax = barRect.anchorMax;
        labelRect.pivot = barRect.pivot;
        labelRect.anchoredPosition = barRect.anchoredPosition;
        labelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
            barRect.rect.width * Mathf.Abs(barRect.localScale.x / labelRect.localScale.x));
        labelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
            barRect.rect.height * Mathf.Abs(barRect.localScale.y / labelRect.localScale.y));
        labelRect.SetAsLastSibling();
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        label.raycastTarget = false;
    }

    void OnDestroy()
    {
        foreach (Sprite sprite in eventHudSprites)
            if (sprite != null) Destroy(sprite);
    }

    void Start()
    {
        StyleEventBar(redProgressBar);
        StyleEventBar(blueProgressBar);
        CenterEventLabel(redCountText, redProgressBar);
        CenterEventLabel(blueTimerText, blueProgressBar);
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (gum == null) gum = FindAnyObjectByType<GumController>();
        if (panicVignetteController == null) panicVignetteController = FindAnyObjectByType<PanicVignetteController>();


        if (vcam == null) vcam = FindAnyObjectByType<CinemachineCamera>();
        if (vcam != null)
        {
            noiseComponent = vcam.GetComponent<CinemachineBasicMultiChannelPerlin>();
        }

        if (warningRectTransform != null)
        {
            defaultWarningPos = warningRectTransform.anchoredPosition;
        }

        ResetCooldown();
        HideAllUI();
        StopCameraShake();
    }

    void OnDisable()
    {
        if (AlienBrainwaveActive || currentEvent == EventType.Blue_HoldOff || blueOwnsPlayer)
            EndEvent(false);
        else
            Time.timeScale = 1f;
    }

    void Update()
    {
        if (blueOwnsClock && Time.timeScale > 0f) Time.timeScale = blueSlowScale;
        if (noiseComponent != null && currentState == EventState.Active)
        {
            currentShakeAmplitude = Mathf.Lerp(currentShakeAmplitude, currentEvent == EventType.Blue_HoldOff ? activeShakeAmplitude * 0.12f : activeShakeAmplitude, Time.deltaTime * shakeDamping);
            noiseComponent.AmplitudeGain = currentShakeAmplitude;
        }

        // ทำงานเฉพาะตอนหมากฝรั่งอยู่ในสถานะ Normal เท่านั้น
        if (gum == null || gum.currentState != GumController.GumState.Normal)
        {
            if (currentState != EventState.Idle) EndEvent(false);
            return;
        }

        switch (currentState)
        {
            case EventState.Idle:
                UpdateIdleState();
                break;

            case EventState.Warning:
                // เวลาเกมหยุดอยู่ (Time.timeScale = 0) อนิเมชันวิ่งผ่าน Coroutine unscaledDeltaTime
                break;

            case EventState.Active:
                UpdateActiveState();
                break;
        }
    }

    private void UpdateIdleState()
    {
        if (player == null) return;

        float currentAltitude = player.transform.position.y;
        if (currentAltitude >= startAltitude)
        {
            nextEventCooldown -= Time.deltaTime;
            if (nextEventCooldown <= 0f)
            {
                StartWarning();
            }
        }
    }

    private void StartWarning()
    {
        currentState = EventState.Warning;

        currentEvent = Random.value > 0.5f ? EventType.Red_Mash : EventType.Blue_HoldOff;
        if (currentEvent == EventType.Blue_HoldOff)
        {
            if (warningAnimCoroutine != null) StopCoroutine(warningAnimCoroutine);
            warningAnimCoroutine = StartCoroutine(AnimateBlueWarning());
            return;
        }
        Time.timeScale = 0f;

        // เปิดใช้งาน GameObject Dimmer
        if (warningDimmerObject != null)
        {
            warningDimmerObject.SetActive(true);
        }

        // เปลี่ยนรูป Image + SetNativeSize
        if (warningImage != null)
        {
            warningImage.sprite = (currentEvent == EventType.Red_Mash) ? redWarningSprite : blueWarningSprite;
            warningImage.SetNativeSize();
        }

        // เล่นอนิเมชันสไลด์ UI Warning จากซ้ายไปขวา (ใช้เวลาจริง)
        if (warningAnimCoroutine != null) StopCoroutine(warningAnimCoroutine);
        warningAnimCoroutine = StartCoroutine(AnimateWarningSlide());
    }

    /// <summary>
    /// Coroutine สไลด์ UI Warning โดยใช้ Time.unscaledDeltaTime เพื่อทำงานได้ขณะ Time.timeScale = 0
    /// </summary>
    private IEnumerator AnimateWarningSlide()
    {
        if (warningRectTransform != null)
        {
            warningRectTransform.gameObject.SetActive(true);

            Vector2 centerPos = defaultWarningPos;
            Vector2 leftOffscreenPos = centerPos - new Vector2(slideDistanceX, 0f);
            Vector2 rightOffscreenPos = centerPos + new Vector2(slideDistanceX, 0f);

            // Phase 1: Slide In (ใช้ unscaledDeltaTime)
            float t = 0f;
            while (t < slideInDuration)
            {
                t += Time.unscaledDeltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, t / slideInDuration);
                warningRectTransform.anchoredPosition = Vector2.Lerp(leftOffscreenPos, centerPos, progress);
                yield return null;
            }
            warningRectTransform.anchoredPosition = centerPos;

            // Phase 2: Hold (ใช้ WaitForSecondsRealtime)
            float holdDuration = Mathf.Max(0f, warningDuration - slideInDuration - slideOutDuration);
            yield return new WaitForSecondsRealtime(holdDuration);

            // Phase 3: Slide Out (ใช้ unscaledDeltaTime)
            t = 0f;
            while (t < slideOutDuration)
            {
                t += Time.unscaledDeltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, t / slideOutDuration);
                warningRectTransform.anchoredPosition = Vector2.Lerp(centerPos, rightOffscreenPos, progress);
                yield return null;
            }
            warningRectTransform.anchoredPosition = rightOffscreenPos;
            warningRectTransform.gameObject.SetActive(false);
        }

        // ปิด Dimmer
        if (warningDimmerObject != null)
        {
            warningDimmerObject.SetActive(false);
        }

        // คืนค่าเวลาเกมให้เดินต่อตามปกติก่อนเข้าช่วง Active Event
        Time.timeScale = 1f;
        StartActiveEvent();
    }

    private void StartActiveEvent()
    {
        currentState = EventState.Active;
        stateTimer = (currentEvent == EventType.Red_Mash) ? redEventDuration : blueEventDuration;
        currentMashes = 0;

        if (currentEvent == EventType.Blue_HoldOff) PrepareBluePlayer();
        else if (player != null) player.SetInputLocked(true);
        if (panicVignetteController != null) panicVignetteController.SetVignetteActive(true);

        TriggerContinuousShake(currentEvent == EventType.Blue_HoldOff ? activeShakeAmplitude * 0.12f : activeShakeAmplitude);

        if (currentEvent == EventType.Red_Mash)
        {
            targetMashes = Random.Range(minTargetMashes, maxTargetMashes + 1);
            if (panicVignetteController != null) panicVignetteController.SetVignetteColor(Color.red);

            if (redUIContainer != null)
            {
                redUIContainer.SetActive(true);
                UpdateRedUI();
            }
        }
        else if (currentEvent == EventType.Blue_HoldOff)
        {
            // 🎯 สุ่มระยะเวลา Blue Event (เช่น 1.5 ถึง 3.5 วินาที)
            currentBlueDuration = Random.Range(Mathf.Clamp(Mathf.Min(minBlueEventDuration, maxBlueEventDuration), 1f, 2f),
                Mathf.Clamp(Mathf.Max(minBlueEventDuration, maxBlueEventDuration), 1f, 2f));
            stateTimer = currentBlueDuration;

            if (panicVignetteController != null) panicVignetteController.SetVignetteColor(new Color(0.2f, 0.55f, 1f));

            if (blueUIContainer != null) blueUIContainer.SetActive(true);
            if (blueProgressBar != null)
            {
                blueProgressBar.gameObject.SetActive(true);
                blueProgressBar.enabled = true;
                blueProgressBar.fillAmount = 0f;
            }
            if (blueTimerText != null)
            {
                blueTimerText.gameObject.SetActive(true);
                blueTimerText.enabled = true;
                blueTimerText.text = $"{stateTimer:0.0}s";
            }
        }
    }

    private void UpdateActiveState()
    {
        if (AlienBrainwaveActive) { UpdateBrainwave(); return; }
        if (currentEvent == EventType.Red_Mash)
        {
            UpdateRedEvent();
        }
        else if (currentEvent == EventType.Blue_HoldOff)
        {
            UpdateBlueEvent();
        }
    }

    private void UpdateRedEvent()
    {
        stateTimer -= Time.deltaTime;
        UpdateRedUI();

        // ตรวจจับจังหวะกดปุ่ม
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            currentMashes++;

            // 1. เพิ่มขนาดหมากฝรั่งแบบ Fix Scale ต่อคลิก
            if (gum != null)
            {
                gum.AddSize(sizePerMash);
            }

            // 2. ให้แรงยกตัวละครส่งตัวลอยขึ้นสู้แรงโน้มถ่วง
            if (player != null)
            {
                player.ApplyMashImpulse(mashLiftImpulse);
            }

            // กระแทกจอสั่นเพิ่มขึ้นชั่วคราวทุกครั้งที่กดปุ่ม
            AddShakePulse(mashShakePulse);
            UpdateRedUI();

            // หากกดครบจำนวนเป้าหมาย จบ Event แบบสำเร็จทันที (เล่นเกมต่อด้วยขนาด gum ปัจจุบัน)
            if (currentMashes >= targetMashes)
            {
                EndEvent(true);
                return;
            }
        }

        if (stateTimer <= 0f)
        {
            FailEvent();
        }
    }

    private void UpdateBlueEvent()
    {
        if (Time.timeScale <= 0f) return;
        // A collected reward takes priority over the blue drift constraint.
        if (player != null && player.IsObstacleImmune) { EndEvent(true); return; }
        stateTimer = Mathf.Max(0f, stateTimer - Time.unscaledDeltaTime);
        if (blueTimerText != null) blueTimerText.text = $"{stateTimer:0.0}s";
        if (blueProgressBar != null && currentBlueDuration > 0f)
            blueProgressBar.fillAmount = Mathf.Clamp01(1f - stateTimer / currentBlueDuration);

        SubmitBlueInput(Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space));
        if (currentState == EventState.Active && stateTimer <= 0f) EndEvent(true);
    }

    // Same held/released rule for the live controls and API-driven verification.
    public void SubmitBlueInput(bool held)
    {
        if (Time.timeScale <= 0f) return;
        if (currentState != EventState.Active || currentEvent != EventType.Blue_HoldOff) return;
        if (currentBlueDuration - stateTimer < blueReleaseGrace) return;
        if (held) FailEvent();
    }

    public void CancelForObstacleHit() { EndEvent(false); }

    private void FailEvent()
    {
        EndEvent(false);
        if (gum != null)
        {
            // ระบุ duration ของ R/B Event โดยเฉพาะ ไม่งั้นจะ fallback ไปใช้ normalStunDuration
            // (ซึ่งเป็นค่าของกรณีชนตัว Player คนละ scenario กัน)
            gum.ForcePop(gum.panicEventStunDuration);
        }
    }

    private void EndEvent(bool success)
    {
        bool endingBrain = AlienBrainwaveActive;
        AlienBrainwaveActive = false;
        if (brainPortrait != null) Destroy(brainPortrait.gameObject);
        brainPortrait = null;
        if (brainScreenTint != null) Destroy(brainScreenTint.gameObject);
        brainScreenTint = null;
        bool endingBlue = currentEvent == EventType.Blue_HoldOff || blueOwnsPlayer;
        if (endingBlue) RestoreBlueClock();
        else Time.timeScale = 1f;

        if (warningAnimCoroutine != null)
        {
            StopCoroutine(warningAnimCoroutine);
            warningAnimCoroutine = null;
        }

        currentState = EventState.Idle;
        currentEvent = EventType.None;

        if (player != null)
        {
            if (endingBlue)
            {
                if (blueOwnsPlayer) player.SetInputLocked(blueInputLockedBefore);
            }
            else
            {
                player.SetInputLocked(false);
                player.ResetGravity();
            }
        }

        if (blueOwnsPlayer && blueBody != null) blueBody.gravityScale = blueGravityBefore;
        blueOwnsPlayer = false;
        blueBody = null;
        if (endingBrain && player != null) player.SetInputLocked(brainInputBefore);
        if (panicVignetteController != null) panicVignetteController.SetVignetteActive(false);

        StopCameraShake();
        HideAllUI();
        ResetCooldown();
    }

    #region Camera Shake Helpers
    private void TriggerContinuousShake(float amplitude)
    {
        if (noiseComponent != null)
        {
            currentShakeAmplitude = amplitude;
            noiseComponent.AmplitudeGain = amplitude;
        }
    }

    private void AddShakePulse(float pulseAmount)
    {
        if (noiseComponent != null)
        {
            currentShakeAmplitude += pulseAmount;
            noiseComponent.AmplitudeGain = currentShakeAmplitude;
        }
    }

    private void StopCameraShake()
    {
        if (noiseComponent != null)
        {
            currentShakeAmplitude = 0f;
            noiseComponent.AmplitudeGain = 0f;
        }
    }
    #endregion

    private void ResetCooldown()
    {
        nextEventCooldown = Random.Range(minInterval, maxInterval);
    }

    private void UpdateRedUI()
    {
        if (redProgressBar != null)
        {
            redProgressBar.fillAmount = (float)currentMashes / targetMashes;
        }
        if (redCountText != null)
        {
            redCountText.text = $"{currentMashes}/{targetMashes}  {Mathf.Max(0f, stateTimer):0.0}s";
        }
    }

    private void HideAllUI()
    {
        if (warningDimmerObject != null) warningDimmerObject.SetActive(false);
        if (warningRectTransform != null)
        {
            warningRectTransform.anchoredPosition = defaultWarningPos;
            warningRectTransform.gameObject.SetActive(false);
        }
        if (redUIContainer != null) redUIContainer.SetActive(false);
        if (blueUIContainer != null) blueUIContainer.SetActive(false);
    }

}
