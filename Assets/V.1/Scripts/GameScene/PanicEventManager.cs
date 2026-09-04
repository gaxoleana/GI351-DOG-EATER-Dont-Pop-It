using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Unity.Cinemachine; // Cinemachine 3.x Namespace

/// <summary>
/// สุ่มและจัดการ Panic Events สองรูปแบบ (Red = กดรัว, Blue = ห้ามกด)
/// หยุดเวลาเกม (Time.timeScale = 0) ชั่วคราวระหว่างช่วง Warning Phase
/// </summary>
public class PanicEventManager : MonoBehaviour
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

    [Tooltip("จำนวนครั้งที่ต้องกดให้ครบ")]
    public int targetMashes = 10;

    [Header("Blue Event (Hold Off) Settings")]
    [Tooltip("ระยะเวลาที่ต้องห้ามกดปุ่ม (วินาที)")]
    public float blueEventDuration = 3.0f;

    [Header("Active Event UI Feedback")]
    public GameObject redUIContainer;
    public Image redProgressBar;
    public TextMeshProUGUI redCountText;

    public GameObject blueUIContainer;
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

    [Header("Runtime Status (Read Only)")]
    public EventState currentState = EventState.Idle;
    public EventType currentEvent = EventType.None;
    public float stateTimer;
    public int currentMashes;

    private float nextEventCooldown;
    private Vector2 defaultWarningPos;
    private Coroutine warningAnimCoroutine;

    void Start()
    {
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (gum == null) gum = FindAnyObjectByType<GumController>();

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
        // Safety Check: คืนค่า Time.timeScale เสมอเผื่อ Scene เปลี่ยนหรือ Object ถูกสคริปต์อื่นปิด
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (noiseComponent != null && currentState == EventState.Active)
        {
            currentShakeAmplitude = Mathf.Lerp(currentShakeAmplitude, activeShakeAmplitude, Time.deltaTime * shakeDamping);
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

        // สั่งหยุดเวลาเกมทั้งหมด (Physics, Movement, Spawner จะหยุดนิ่ง)
        Time.timeScale = 0f;

        // สุ่มประเภท Event ไว้ล่วงหน้า
        currentEvent = Random.value > 0.5f ? EventType.Red_Mash : EventType.Blue_HoldOff;

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

        if (player != null) player.SetInputLocked(true);

        // เริ่มจอสั่นแบบต่อเนื่องเมื่อเข้าสู่ Active Event
        TriggerContinuousShake(activeShakeAmplitude);

        if (currentEvent == EventType.Red_Mash && redUIContainer != null)
        {
            redUIContainer.SetActive(true);
            UpdateRedUI();
        }
        else if (currentEvent == EventType.Blue_HoldOff && blueUIContainer != null)
        {
            blueUIContainer.SetActive(true);
        }
    }

    private void UpdateActiveState()
    {
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
        stateTimer -= Time.deltaTime;

        if (blueTimerText != null)
        {
            blueTimerText.text = $"{Mathf.Max(0f, stateTimer):0.0}s";
        }

        if (Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space))
        {
            FailEvent();
            return;
        }

        if (stateTimer <= 0f)
        {
            EndEvent(true);
        }
    }

    private void FailEvent()
    {
        EndEvent(false);
        if (gum != null)
        {
            gum.ForcePop();
        }
    }

    private void EndEvent(bool success)
    {
        // ปลดล็อกเวลาเผื่อมีการจบ Event กลางคัน
        Time.timeScale = 1f;

        if (warningAnimCoroutine != null)
        {
            StopCoroutine(warningAnimCoroutine);
            warningAnimCoroutine = null;
        }

        currentState = EventState.Idle;
        currentEvent = EventType.None;

        if (player != null) player.SetInputLocked(false);

        // หยุดจอสั่นทันทีเมื่อจบ Event
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
            redCountText.text = $"{currentMashes}/{targetMashes}";
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