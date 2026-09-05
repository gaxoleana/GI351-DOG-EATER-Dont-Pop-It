using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Unity.Cinemachine;

public class PanicEventManagerVariantTimeStop : MonoBehaviour
{
    public enum EventType { None, Red_Mash, Blue_HoldOff }
    public enum EventState { Idle, Warning, Active }

    [Header("References")]
    public PlayerController player;
    public GumController gum;

    [Header("World Overlay Settings (2D Sprite Dimmer)")]
    public GameObject warningDimmerObject;

    [Header("Event Interval Settings")]
    public float minInterval = 8f;
    public float maxInterval = 15f;
    public float startAltitude = 100f;

    [Header("Warning Phase Settings")]
    public float warningDuration = 1.5f;

    [Header("Warning Animation Settings")]
    public RectTransform warningRectTransform;
    public Image warningImage;
    public Sprite redWarningSprite;
    public Sprite blueWarningSprite;
    public float slideDistanceX = 1200f;
    public float slideInDuration = 0.3f;
    public float slideOutDuration = 0.3f;

    [Header("Red Event (Mash) Settings")]
    public float redEventDuration = 2.5f;
    public float sizePerMash = 0.05f;
    public float mashLiftImpulse = 2.5f; // คงไว้เพื่อไม่ให้ Inspector แจ้งเตือน (แต่จะไม่ได้ใช้งานแล้ว)
    public int minTargetMashes = 3;
    public int maxTargetMashes = 12;
    [HideInInspector] public int targetMashes; 

    [Header("Blue Event (Hold Off) Settings")]
    public float blueEventDuration = 3.0f;
    public float minBlueEventDuration = 1.5f;
    public float maxBlueEventDuration = 3.5f;
    public float blueEventGravityMultiplier = 0.4f;
    [HideInInspector] public float currentBlueDuration; 

    [Header("Active Event UI Feedback")]
    public GameObject redUIContainer;
    public Image redProgressBar;
    public TextMeshProUGUI redCountText;

    public GameObject blueUIContainer;
    public Image blueProgressBar;
    public TextMeshProUGUI blueTimerText;

    [Header("Camera Shake Settings")]
    public CinemachineCamera vcam;
    public float activeShakeAmplitude = 1.0f;
    public float mashShakePulse = 2.5f;
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

    // ตัวแปรสำหรับเก็บขนาดหมากฝรั่งก่อนเริ่ม Event
    private float preEventGumSize;

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
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (noiseComponent != null && currentState == EventState.Active)
        {
            currentShakeAmplitude = Mathf.Lerp(currentShakeAmplitude, activeShakeAmplitude, Time.unscaledDeltaTime * shakeDamping);
            noiseComponent.AmplitudeGain = currentShakeAmplitude;
        }

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
        Time.timeScale = 0f;

        // บันทึกขนาดของ Gum ก่อนที่จะเริ่ม Event
        if (gum != null)
        {
            preEventGumSize = gum.currentSize;
        }

        currentEvent = Random.value > 0.5f ? EventType.Red_Mash : EventType.Blue_HoldOff;

        if (warningDimmerObject != null)
        {
            warningDimmerObject.SetActive(true);
        }

        if (warningImage != null)
        {
            warningImage.sprite = (currentEvent == EventType.Red_Mash) ? redWarningSprite : blueWarningSprite;
            warningImage.SetNativeSize();
        }

        if (warningAnimCoroutine != null) StopCoroutine(warningAnimCoroutine);
        warningAnimCoroutine = StartCoroutine(AnimateWarningSlide());
    }

    private IEnumerator AnimateWarningSlide()
    {
        if (warningRectTransform != null)
        {
            warningRectTransform.gameObject.SetActive(true);

            Vector2 centerPos = defaultWarningPos;
            Vector2 leftOffscreenPos = centerPos - new Vector2(slideDistanceX, 0f);
            Vector2 rightOffscreenPos = centerPos + new Vector2(slideDistanceX, 0f);

            float t = 0f;
            while (t < slideInDuration)
            {
                t += Time.unscaledDeltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, t / slideInDuration);
                warningRectTransform.anchoredPosition = Vector2.Lerp(leftOffscreenPos, centerPos, progress);
                yield return null;
            }
            warningRectTransform.anchoredPosition = centerPos;

            float holdDuration = Mathf.Max(0f, warningDuration - slideInDuration - slideOutDuration);
            yield return new WaitForSecondsRealtime(holdDuration);

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

        if (warningDimmerObject != null)
        {
            warningDimmerObject.SetActive(false);
        }

        StartActiveEvent();
    }

    private void StartActiveEvent()
    {
        currentState = EventState.Active;
        stateTimer = (currentEvent == EventType.Red_Mash) ? redEventDuration : blueEventDuration;
        currentMashes = 0;

        if (player != null) player.SetInputLocked(true);

        TriggerContinuousShake(activeShakeAmplitude);

        if (currentEvent == EventType.Red_Mash)
        {
            targetMashes = Random.Range(minTargetMashes, maxTargetMashes + 1);
            if (redUIContainer != null)
            {
                redUIContainer.SetActive(true);
                UpdateRedUI();
            }
        }
        else if (currentEvent == EventType.Blue_HoldOff)
        {
            currentBlueDuration = Random.Range(minBlueEventDuration, maxBlueEventDuration);
            stateTimer = currentBlueDuration;

            if (player != null)
            {
                player.SetGravityMultiplier(blueEventGravityMultiplier);
                player.DampDownwardVelocity(-1.0f);
            }

            if (blueUIContainer != null)
            {
                blueUIContainer.SetActive(true);
            }
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
        stateTimer -= Time.unscaledDeltaTime; 

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            currentMashes++;

            // เพิ่มขนาดของ Gum ให้ดูพองขึ้นระหว่างที่กดรัวๆ (ให้ผลตอบสนองทางสายตา)
            if (gum != null)
            {
                gum.AddSize(sizePerMash);
            }

            // [FIXED BUG]: ลบคำสั่ง player.ApplyMashImpulse ออก
            // สาเหตุที่ผู้เล่นพุ่งกระฉูดเมื่อจบอีเวนท์ เพราะเมื่ออยู่ในสถานะ Time.timeScale = 0
            // ฟิสิกส์จะไม่ทำงาน การ AddForce หรือเพิ่ม Velocity จะไปทับถมกันอยู่เบื้องหลัง
            // และระเบิดตู้มเดียวตอนเวลาเดินกลับมาเป็น 1 อีกครั้ง

            AddShakePulse(mashShakePulse);
            UpdateRedUI();

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
        stateTimer -= Time.unscaledDeltaTime; 

        if (blueTimerText != null)
        {
            blueTimerText.text = $"{Mathf.Max(0f, stateTimer):0.0}s";
        }

        if (blueProgressBar != null && currentBlueDuration > 0f)
        {
            float progress = 1f - (stateTimer / currentBlueDuration);
            blueProgressBar.fillAmount = progress;
        }

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
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
            gum.ForcePop(gum.panicEventStunDuration);
        }
    }

    private void EndEvent(bool success)
    {
        Time.timeScale = 1f; 

        if (warningAnimCoroutine != null)
        {
            StopCoroutine(warningAnimCoroutine);
            warningAnimCoroutine = null;
        }

        // [FIXED BUG]: ถ้าชนะ Event ให้รีเซ็ตขนาด Gum กลับไปเป็นขนาดเดิมเท่าตอนเพิ่งเริ่มเตือน
        if (success && gum != null)
        {
            gum.currentSize = preEventGumSize;
        }

        currentState = EventState.Idle;
        currentEvent = EventType.None;

        if (player != null)
        {
            player.SetInputLocked(false);
            player.ResetGravity(); 
        }

        PlayResultShake(); 
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

    private void PlayResultShake()
    {
        if (noiseComponent != null)
        {
            StartCoroutine(ResultShakeRoutine());
        }
    }

    private IEnumerator ResultShakeRoutine()
    {
        if (noiseComponent != null)
        {
            noiseComponent.AmplitudeGain = mashShakePulse; 
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.unscaledDeltaTime;
                noiseComponent.AmplitudeGain = Mathf.Lerp(mashShakePulse, 0f, t / 0.5f);
                yield return null;
            }
            noiseComponent.AmplitudeGain = 0f;
            currentShakeAmplitude = 0f;
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
