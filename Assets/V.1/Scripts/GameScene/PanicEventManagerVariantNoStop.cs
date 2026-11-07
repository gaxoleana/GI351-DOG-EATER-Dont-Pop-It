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

    [Header("Green Angry Cat Event (Brainwave) Settings")]
    [Tooltip("เวลานับถอยหลัง 3 2 1 ก่อนเริ่ม Event แมวโกรธ (วินาที)")]
    public float brainwaveCountdownDuration = 3.0f;

    [Tooltip("จำนวนช่วงย่อยของ Event แมวโกรธ (3 ช่วง ช่วงละ 1 วินาที รวม 3 วินาที)")]
    public int brainwavePhaseCount = 3;

    [Tooltip("เวลาต่อ 1 ช่วงย่อยของ Event แมวโกรธ (วินาที)")]
    public float brainwaveSinglePhaseDuration = 1.0f;

    [Tooltip("จังหวะพักหายใจและกระพริบเตือนสั้น ๆ ระหว่างช่วง (วินาที)")]
    [Range(0.1f, 0.35f)]
    public float brainwaveMicroGapDuration = 0.18f;

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
    private bool brainCountingDown;
    private float brainCountdownTotal;
    private float brainCountdownRemaining;
    private int brainCurrentPhaseIndex;
    private float brainRemaining, brainPhaseRemaining, brainPhaseDuration;
    private bool brainInMicroGap;
    private float brainMicroGapRemaining;
    private EventType[] brainSequence;
    private bool brainInputBefore;
    private Image brainPortrait;
    private Image brainScreenTint;
    private TextMeshProUGUI brainCountdownText;
    private RectTransform brainSequenceRoot;
    private RectTransform[] brainDotRects;
    private Image[] brainDotBorders;
    private Image[] brainDotFills;
    private Sprite brainCircleSprite;

    public bool TryStartAlienBrainwave(float duration, Sprite portrait, bool flipX)
    {
        if (!isActiveAndEnabled || currentState != EventState.Idle || player == null ||
            gum == null || gum.currentState != GumController.GumState.Normal || Time.timeScale <= 0f) return false;

        brainInputBefore = player.IsInputLocked;
        AlienBrainwaveActive = true;
        brainCountingDown = true;
        brainInMicroGap = false;
        brainMicroGapRemaining = 0f;
        brainCountdownTotal = Mathf.Max(1f, brainwaveCountdownDuration);
        brainCountdownRemaining = brainCountdownTotal;
        brainCurrentPhaseIndex = 0;

        int phaseCount = Mathf.Max(1, brainwavePhaseCount);
        float singleDuration = Mathf.Max(0.1f, brainwaveSinglePhaseDuration);
        brainRemaining = phaseCount * singleDuration;

        // สุ่มลำดับทั้ง 3 ช่วงไว้ล่วงหน้าตั้งแต่เริ่มขึ้นรูปแมวกลางจอ
        brainSequence = new EventType[phaseCount];
        for (int i = 0; i < phaseCount; i++)
        {
            brainSequence[i] = Random.value < 0.5f ? EventType.Red_Mash : EventType.Blue_HoldOff;
        }

        currentState = EventState.Warning;
        currentEvent = EventType.None;
        HideAllUI();

        Transform uiParent = warningRectTransform != null ? warningRectTransform.parent : null;
        if (uiParent == null && redCountText != null)
        {
            Canvas canvas = redCountText.GetComponentInParent<Canvas>();
            if (canvas != null) uiParent = canvas.transform;
        }

        if (uiParent != null)
        {
            var tintObject = new GameObject("Alien Brainwave Screen Tint", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            brainScreenTint = tintObject.GetComponent<Image>();
            brainScreenTint.rectTransform.SetParent(uiParent, false);
            brainScreenTint.rectTransform.anchorMin = Vector2.zero;
            brainScreenTint.rectTransform.anchorMax = Vector2.one;
            brainScreenTint.rectTransform.offsetMin = brainScreenTint.rectTransform.offsetMax = Vector2.zero;
            brainScreenTint.color = new Color(0.1f, 0.85f, 0.2f, 0.1f);
            brainScreenTint.raycastTarget = false;

            if (portrait != null)
            {
                var go = new GameObject("Alien Brainwave Portrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                brainPortrait = go.GetComponent<Image>();
                brainPortrait.rectTransform.SetParent(uiParent, false);
                brainPortrait.rectTransform.anchorMin = brainPortrait.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                brainPortrait.rectTransform.anchoredPosition = new Vector2(0f, 100f);
                brainPortrait.rectTransform.sizeDelta = new Vector2(300f, 300f);
                brainPortrait.rectTransform.localScale = new Vector3(flipX ? -1f : 1f, 1f, 1f);
                brainPortrait.sprite = portrait;
                brainPortrait.color = Color.Lerp(Color.white, new Color(0.65f, 1f, 0.65f), 0.35f);
                brainPortrait.preserveAspect = true;
                brainPortrait.raycastTarget = false;
            }

            CreateBrainSequenceDots(uiParent, phaseCount);
            brainCountdownText = CreateBrainCountdownText(uiParent);
        }

        UpdateBrainCountdownUI();
        return true;
    }

    private Sprite GetOrCreateBrainCircleSprite()
    {
        if (brainCircleSprite != null) return brainCircleSprite;

        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float center = (size - 1) * 0.5f;
        float radius = size * 0.46f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01((radius - dist) * 1.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        brainCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        eventHudSprites.Add(brainCircleSprite);
        return brainCircleSprite;
    }

    private void CreateBrainSequenceDots(Transform uiParent, int count)
    {
        Sprite circleSprite = GetOrCreateBrainCircleSprite();

        GameObject rootObj = new GameObject("Alien Brainwave Sequence Dots", typeof(RectTransform));
        brainSequenceRoot = rootObj.GetComponent<RectTransform>();
        brainSequenceRoot.SetParent(uiParent, false);
        brainSequenceRoot.anchorMin = new Vector2(0.5f, 0.5f);
        brainSequenceRoot.anchorMax = new Vector2(0.5f, 0.5f);
        brainSequenceRoot.pivot = new Vector2(0.5f, 0.5f);
        brainSequenceRoot.anchoredPosition = new Vector2(0f, -55f);
        brainSequenceRoot.sizeDelta = new Vector2(count * 90f, 80f);

        brainDotRects = new RectTransform[count];
        brainDotBorders = new Image[count];
        brainDotFills = new Image[count];

        float spacing = 86f;
        float startX = -(count - 1) * spacing * 0.5f;

        for (int i = 0; i < count; i++)
        {
            GameObject dotObj = new GameObject($"Sequence Dot {i + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform dotRt = dotObj.GetComponent<RectTransform>();
            dotRt.SetParent(brainSequenceRoot, false);
            dotRt.anchorMin = new Vector2(0.5f, 0.5f);
            dotRt.anchorMax = new Vector2(0.5f, 0.5f);
            dotRt.pivot = new Vector2(0.5f, 0.5f);
            dotRt.anchoredPosition = new Vector2(startX + i * spacing, 0f);
            dotRt.sizeDelta = new Vector2(64f, 64f);

            Image borderImg = dotObj.GetComponent<Image>();
            borderImg.sprite = circleSprite;
            borderImg.color = new Color(0.08f, 0.1f, 0.14f, 0.95f);
            borderImg.raycastTarget = false;

            GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform fillRt = fillObj.GetComponent<RectTransform>();
            fillRt.SetParent(dotRt, false);
            fillRt.anchorMin = new Vector2(0.5f, 0.5f);
            fillRt.anchorMax = new Vector2(0.5f, 0.5f);
            fillRt.pivot = new Vector2(0.5f, 0.5f);
            fillRt.anchoredPosition = Vector2.zero;
            fillRt.sizeDelta = new Vector2(50f, 50f);

            Image fillImg = fillObj.GetComponent<Image>();
            fillImg.sprite = circleSprite;
            fillImg.color = new Color(0.22f, 0.25f, 0.3f, 0.85f);
            fillImg.raycastTarget = false;

            brainDotRects[i] = dotRt;
            brainDotBorders[i] = borderImg;
            brainDotFills[i] = fillImg;
        }
    }

    private Color GetBrainEventColor(EventType type)
    {
        return type == EventType.Red_Mash
            ? new Color(1f, 0.2f, 0.25f, 1f)
            : new Color(0.18f, 0.68f, 1f, 1f);
    }

    private void UpdateBrainSequenceDotsUI()
    {
        if (brainSequenceRoot == null || brainSequence == null || brainDotRects == null) return;

        int count = brainSequence.Length;
        brainSequenceRoot.anchoredPosition = brainCountingDown ? new Vector2(0f, -55f) : new Vector2(0f, 35f);

        if (brainCountingDown)
        {
            // ค่อย ๆ เปิดสีทีละดวงตามจังหวะนับ 3 -> เปิดดวงที่ 1, 2 -> เปิดดวงที่ 2, 1 -> เปิดดวงที่ 3
            float elapsed = Mathf.Clamp(brainCountdownTotal - brainCountdownRemaining, 0f, brainCountdownTotal);
            float stepDuration = brainCountdownTotal / Mathf.Max(1, count);
            int revealedCount = Mathf.Clamp(Mathf.FloorToInt(elapsed / stepDuration) + 1, 1, count);

            for (int i = 0; i < count; i++)
            {
                bool revealed = i < revealedCount;
                if (revealed)
                {
                    Color c = GetBrainEventColor(brainSequence[i]);
                    brainDotFills[i].color = c;
                    brainDotBorders[i].color = Color.Lerp(new Color(0.08f, 0.1f, 0.14f, 0.95f), Color.white, i == revealedCount - 1 ? 0.65f : 0.25f);

                    float stepElapsed = elapsed - i * stepDuration;
                    float pop = (i == revealedCount - 1 && stepElapsed <= 0.25f)
                        ? 1f + Mathf.Sin(Mathf.Clamp01(stepElapsed / 0.25f) * Mathf.PI) * 0.28f
                        : 1f;
                    brainDotRects[i].localScale = Vector3.one * pop;
                }
                else
                {
                    brainDotFills[i].color = new Color(0.22f, 0.25f, 0.3f, 0.75f);
                    brainDotBorders[i].color = new Color(0.08f, 0.1f, 0.14f, 0.85f);
                    brainDotRects[i].localScale = Vector3.one * 0.9f;
                }
            }
            return;
        }

        // ช่วงเริ่มเล่น Event จริง (ไฮไลต์ดวงปัจจุบัน และกระพริบเตือนช่วงถัดไป)
        for (int i = 0; i < count; i++)
        {
            Color baseColor = GetBrainEventColor(brainSequence[i]);
            if (i < brainCurrentPhaseIndex)
            {
                // ดวงที่ผ่านไปแล้ว
                brainDotFills[i].color = new Color(baseColor.r * 0.55f, baseColor.g * 0.55f, baseColor.b * 0.55f, 0.45f);
                brainDotBorders[i].color = new Color(0.08f, 0.1f, 0.14f, 0.5f);
                brainDotRects[i].localScale = Vector3.one * 0.85f;
            }
            else if (i == brainCurrentPhaseIndex)
            {
                if (brainInMicroGap)
                {
                    // กระพริบเตือนสั้น ๆ ในช่วง Micro-Gap ก่อนเข้าสีถัดไป
                    float blink = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 55f);
                    brainDotFills[i].color = Color.Lerp(baseColor, Color.white, blink * 0.65f);
                    brainDotBorders[i].color = Color.white;
                    brainDotRects[i].localScale = Vector3.one * Mathf.Lerp(1.15f, 1.4f, blink);
                }
                else
                {
                    // ดวงที่กำลังเล่นอยู่
                    brainDotFills[i].color = baseColor;
                    brainDotBorders[i].color = Color.white;
                    float pulse = 1.28f + Mathf.Sin(Time.unscaledTime * 18f) * 0.06f;
                    brainDotRects[i].localScale = Vector3.one * pulse;
                }
            }
            else
            {
                // ดวงถัดไปที่กำลังจะมาถึง
                bool preWarnNext = !brainInMicroGap && i == brainCurrentPhaseIndex + 1 && brainPhaseRemaining <= Mathf.Max(0.18f, brainwaveMicroGapDuration);
                if (preWarnNext)
                {
                    float blink = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 45f);
                    brainDotFills[i].color = Color.Lerp(baseColor, Color.white, blink * 0.45f);
                    brainDotBorders[i].color = Color.Lerp(new Color(0.08f, 0.1f, 0.14f, 0.95f), Color.white, blink);
                    brainDotRects[i].localScale = Vector3.one * Mathf.Lerp(1.0f, 1.18f, blink);
                }
                else
                {
                    brainDotFills[i].color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.9f);
                    brainDotBorders[i].color = new Color(0.08f, 0.1f, 0.14f, 0.95f);
                    brainDotRects[i].localScale = Vector3.one;
                }
            }
        }
    }

    private TextMeshProUGUI CreateBrainCountdownText(Transform uiParent)
    {
        TextMeshProUGUI template = redCountText != null ? redCountText : blueTimerText;
        GameObject textObj;
        TextMeshProUGUI tmp;

        if (template != null)
        {
            textObj = Instantiate(template.gameObject, uiParent);
            textObj.name = "Alien Brainwave Countdown Text";
            tmp = textObj.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            textObj = new GameObject("Alien Brainwave Countdown Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(uiParent, false);
            tmp = textObj.GetComponent<TextMeshProUGUI>();
        }

        RectTransform rt = tmp.rectTransform;
        rt.SetParent(uiParent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, -155f);
        rt.sizeDelta = new Vector2(400f, 160f);
        rt.localScale = Vector3.one;

        tmp.fontSize = 110f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableAutoSizing = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.color = new Color(0.55f, 1f, 0.55f, 1f);
        tmp.raycastTarget = false;
        textObj.SetActive(true);
        tmp.enabled = true;
        return tmp;
    }

    private void UpdateBrainCountdownUI()
    {
        UpdateBrainSequenceDotsUI();
        if (brainCountdownText == null) return;
        int displayNum = Mathf.Clamp(Mathf.CeilToInt(brainCountdownRemaining), 1, 99);
        brainCountdownText.text = displayNum.ToString();

        float frac = brainCountdownRemaining - Mathf.Floor(brainCountdownRemaining);
        float scale = 1f + Mathf.Sin(frac * Mathf.PI) * 0.2f;
        brainCountdownText.rectTransform.localScale = Vector3.one * scale;
    }

    private void ReleaseBrainBlue()
    {
        RestoreBlueClock();
        if (blueOwnsPlayer && blueBody != null) blueBody.gravityScale = blueGravityBefore;
        blueOwnsPlayer = false;
        blueBody = null;
    }

    private void BeginBrainMicroGap(EventType upcomingType)
    {
        ReleaseBrainBlue();
        brainInMicroGap = true;
        brainMicroGapRemaining = Mathf.Clamp(brainwaveMicroGapDuration, 0.1f, 0.35f);
        currentEvent = EventType.None;
        if (player != null) player.SetInputLocked(true);

        if (redUIContainer != null) redUIContainer.SetActive(false);
        if (blueUIContainer != null) blueUIContainer.SetActive(false);
        if (panicVignetteController != null)
        {
            panicVignetteController.SetVignetteColor(upcomingType == EventType.Red_Mash ? Color.red : new Color(0.2f, 0.55f, 1f));
            panicVignetteController.SetVignetteActive(true);
        }
        TriggerContinuousShake(activeShakeAmplitude * 0.15f);
        UpdateBrainSequenceDotsUI();
    }

    private void SetBrainPhase(EventType type)
    {
        ReleaseBrainBlue();
        brainInMicroGap = false;
        brainMicroGapRemaining = 0f;
        currentState = EventState.Active;
        currentEvent = type;
        currentMashes = 0;
        targetMashes = 1;

        // แต่ละช่วงย่อยมีเวลาคงที่ 1 วินาที (รวม 3 ช่วง = 3 วินาที)
        brainPhaseDuration = Mathf.Max(0.1f, brainwaveSinglePhaseDuration);
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
        UpdateBrainSequenceDotsUI();
    }

    private void UpdateBrainwave()
    {
        if (Time.timeScale <= 0f) return;
        float dt = Time.unscaledDeltaTime;

        if (brainCountingDown)
        {
            if (brainPortrait != null)
            {
                brainPortrait.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(Time.unscaledTime * 35f) * 4f, 100f);
                brainPortrait.color = Color.Lerp(Color.white, new Color(0.65f, 1f, 0.65f), 0.35f);
            }

            brainCountdownRemaining -= dt;
            if (brainCountdownRemaining > 0f)
            {
                UpdateBrainCountdownUI();
                return;
            }

            brainCountingDown = false;
            if (brainCountdownText != null)
            {
                Destroy(brainCountdownText.gameObject);
                brainCountdownText = null;
            }

            brainCurrentPhaseIndex = 0;
            EventType firstPhase = (brainSequence != null && brainSequence.Length > 0)
                ? brainSequence[0]
                : (Random.value < 0.5f ? EventType.Red_Mash : EventType.Blue_HoldOff);
            SetBrainPhase(firstPhase);
            return;
        }

        if (brainPortrait != null)
        {
            brainPortrait.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(Time.unscaledTime * 45f) * 5f, 180f);
            brainPortrait.color = Color.Lerp(Color.white, new Color(0.65f, 1f, 0.65f), 0.35f);
        }

        // จังหวะพักหายใจสั้น ๆ (Micro-Gap) พร้อมกระพริบเตือนสีถัดไปก่อนเริ่มช่วงใหม่
        if (brainInMicroGap)
        {
            brainMicroGapRemaining -= dt;
            EventType upcoming = (brainSequence != null && brainCurrentPhaseIndex < brainSequence.Length)
                ? brainSequence[brainCurrentPhaseIndex]
                : EventType.Red_Mash;

            if (brainScreenTint != null)
            {
                float flash = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 55f);
                Color baseTint = upcoming == EventType.Red_Mash
                    ? new Color(1f, 0.08f, 0.14f, 0.22f)
                    : new Color(0.08f, 0.45f, 1f, 0.22f);
                brainScreenTint.color = new Color(baseTint.r, baseTint.g, baseTint.b, Mathf.Lerp(0.05f, 0.24f, flash));
            }

            UpdateBrainSequenceDotsUI();

            if (brainMicroGapRemaining <= 0f)
            {
                SetBrainPhase(upcoming);
            }
            return;
        }

        brainRemaining = Mathf.Max(0f, brainRemaining - dt);
        brainPhaseRemaining = Mathf.Max(0f, brainPhaseRemaining - dt);
        stateTimer = brainPhaseRemaining;
        UpdateBrainSequenceDotsUI();

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

        brainCurrentPhaseIndex++;
        int totalPhases = brainSequence != null ? brainSequence.Length : Mathf.Max(1, brainwavePhaseCount);
        if (brainCurrentPhaseIndex >= totalPhases)
        {
            EndEvent(true);
            return;
        }

        EventType nextPhase = (brainSequence != null && brainCurrentPhaseIndex < brainSequence.Length)
            ? brainSequence[brainCurrentPhaseIndex]
            : (Random.value < 0.5f ? EventType.Red_Mash : EventType.Blue_HoldOff);

        // เข้าจังหวะพักหายใจสั้น ๆ (Micro-Gap) ก่อนเริ่มสีถัดไป
        BeginBrainMicroGap(nextPhase);
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
        if (brainCircleSprite != null && brainCircleSprite.texture != null)
            Destroy(brainCircleSprite.texture);
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
            if (currentState != EventState.Idle || AlienBrainwaveActive) EndEvent(false);
            return;
        }

        if (AlienBrainwaveActive)
        {
            UpdateBrainwave();
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
        if (player != null && player.RocketBoostTimeRemaining > 0f) { EndEvent(true); return; }
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
        brainCountingDown = false;
        brainInMicroGap = false;
        brainMicroGapRemaining = 0f;
        if (brainCountdownText != null) Destroy(brainCountdownText.gameObject);
        brainCountdownText = null;
        if (brainSequenceRoot != null) Destroy(brainSequenceRoot.gameObject);
        brainSequenceRoot = null;
        brainDotRects = null;
        brainDotBorders = null;
        brainDotFills = null;
        brainSequence = null;
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

        if (success && player != null)
        {
            player.TryActivateEventImmunity();
        }

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
