using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// ควบคุม UI แถบความสูงติดตามการเดินทางของผู้เล่นตั้งแต่ 0m ถึง 3000m
/// </summary>
public class AltitudeProgressBarUI : MonoBehaviour
{
    [Header("Target References")]
    [Tooltip("Transform ของตัวละครที่ใช้เช็กค่า Y Position")]
    public Transform playerTransform;

    [Header("Altitude Settings")]
    [Tooltip("ความสูงเริ่มต้น (เมตร)")]
    public float minAltitude = 0f;

    [Tooltip("ความสูงเส้นชัย (เมตร)")]
    public float maxAltitude = 3000f;

    [Tooltip("Offset ตำแหน่ง Y เริ่มต้นของ Player ใน Scene (ถ้า Player เริ่มต้นยืนที่ Y = -2 ให้ใส่ -2)")]
    public float startYOffset = 0f;

    [Header("UI Components")]
    [Tooltip("Image ที่ตั้ง Image Type = Filled (แทน Slider) ใช้ fillAmount แทน value")]
    public Image progressFillImage;

    [Tooltip("Icon แสดงตำแหน่งตัวละครที่จะวิ่งตามความสูงบน UI")]
    public RectTransform playerIcon;

    [Tooltip("ตำแหน่ง Y ต่ำสุดและสูงสุดของ Icon บน UI Canvas (กรณีใช้ Icon เคลื่อนที่ตาม)")]
    public float iconMinY = -200f;
    public float iconMaxY = 200f;

    [Tooltip("Text แสดงระยะความสูงปัจจุบัน")]
    public TextMeshProUGUI altitudeText;

    [Header("Icon Rewards")]
    [Tooltip("ลาก GameObject bird / plane / alien ที่ 'ปิด' อยู่ตอนแรก (ตัวที่จะซ่อนเมื่อปลดล็อก)")]
    public GameObject birdOff;
    public GameObject planeOff;
    public GameObject alienOff;

    [Tooltip("ลาก GameObject bird (1) / plane (1) / alien (1) ที่จะ 'เปิด' แทนตอนปลดล็อก")]
    public GameObject birdOn;
    public GameObject planeOn;
    public GameObject alienOn;

    [Tooltip("ระดับความสูง (หน่วย km ตามที่โชว์บนจอ) ที่จะปลดล็อกแต่ละตัว")]
    public float birdUnlockKm = 1f;
    public float planeUnlockKm = 10f;
    public float alienUnlockKm = 20f;

    private bool birdUnlocked = false;
    private bool planeUnlocked = false;
    private bool alienUnlocked = false;

    [Header("Events")]
    [Tooltip("เหตุการณ์ที่จะทำงานเมื่อถึงเส้นชัย 30km")]
    public bool hasReachedFinishLine = false;

    // Action เผื่อดึงไปใช้ตัดเข้าฉากจบ หรือขึ้น UI Win
    public event Action OnReachFinishLine;

    void Start()
    {
        if (playerTransform == null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null) playerTransform = player.transform;
        }

        // ตั้งสถานะเริ่มต้นให้แน่ใจว่า *Off เปิด, *On ปิด
        SetIconState(birdOff, birdOn, false);
        SetIconState(planeOff, planeOn, false);
        SetIconState(alienOff, alienOn, false);
    }

    void Update()
    {
        if (playerTransform == null) return;

        // 1. คำนวณความสูงปัจจุบันโดยอิงจากตำแหน่ง Y ของ Player
        float currentAltitude = Mathf.Max(0f, playerTransform.position.y - startYOffset);

        // 2. คำนวณ Ratio ความก้าวหน้า (0.0 ถึง 1.0)
        float progressRatio = Mathf.Clamp01((currentAltitude - minAltitude) / (maxAltitude - minAltitude));

        // 3. อัปเดตค่า Image fillAmount แทน Slider value
        if (progressFillImage != null)
        {
            progressFillImage.fillAmount = progressRatio;
        }

        // 4. ขยับ Icon ตัวละครตามความสูงบน UI
        if (playerIcon != null)
        {
            Vector2 iconPos = playerIcon.anchoredPosition;
            iconPos.y = Mathf.Lerp(iconMinY, iconMaxY, progressRatio);
            playerIcon.anchoredPosition = iconPos;
        }

        // 5. อัปเดตข้อความแสดงระยะทาง
        float currentKm = currentAltitude / 100f;
        if (altitudeText != null)
        {
            altitudeText.text = $"{Mathf.FloorToInt(currentAltitude / 100):N0}km / {Mathf.FloorToInt(maxAltitude / 100):N0}km";
        }

        // 5.5 เช็กปลดล็อก icon ตามระดับความสูง
        if (!birdUnlocked && currentKm >= birdUnlockKm)
        {
            birdUnlocked = true;
            SetIconState(birdOff, birdOn, true);
        }
        if (!planeUnlocked && currentKm >= planeUnlockKm)
        {
            planeUnlocked = true;
            SetIconState(planeOff, planeOn, true);
        }
        if (!alienUnlocked && currentKm >= alienUnlockKm)
        {
            alienUnlocked = true;
            SetIconState(alienOff, alienOn, true);
        }

        // 6. เช็กเงื่อนไขเข้าเส้นชัย
        if (progressRatio >= 1.0f && !hasReachedFinishLine)
        {
            hasReachedFinishLine = true;
            TriggerFinishLine();
        }
    }

    private void SetIconState(GameObject offObj, GameObject onObj, bool unlocked)
    {
        if (offObj != null) offObj.SetActive(!unlocked);
        if (onObj != null) onObj.SetActive(unlocked);
    }

    private void TriggerFinishLine()
    {
        Debug.Log("🎉 บรรลุความสูง 30km เข้าเส้นชัยแล้ว!");
        OnReachFinishLine?.Invoke();
    }
}