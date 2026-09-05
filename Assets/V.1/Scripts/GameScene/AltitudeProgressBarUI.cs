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
    [Tooltip("UI Slider แนวตั้ง (Bottom To Top)")]
    public Slider progressSlider;

    [Tooltip("Icon แสดงตำแหน่งตัวละครที่จะวิ่งตามความสูงบน UI")]
    public RectTransform playerIcon;

    [Tooltip("ตำแหน่ง Y ต่ำสุดและสูงสุดของ Icon บน UI Canvas (กรณีใช้ Icon เคลื่อนที่ตาม)")]
    public float iconMinY = -200f;
    public float iconMaxY = 200f;

    [Tooltip("Text แสดงระยะความสูงปัจจุบัน")]
    public TextMeshProUGUI altitudeText;

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

        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        // 1. คำนวณความสูงปัจจุบันโดยอิงจากตำแหน่ง Y ของ Player
        float currentAltitude = Mathf.Max(0f, playerTransform.position.y - startYOffset); // แปลงจากเมตรเป็นกิโลเมตร

        // 2. คำนวณ Ratio ความก้าวหน้า (0.0 ถึง 1.0)
        float progressRatio = Mathf.Clamp01((currentAltitude - minAltitude) / (maxAltitude - minAltitude));

        // 3. อัปเดตค่า UI Slider
        if (progressSlider != null)
        {
            progressSlider.value = progressRatio;
        }

        // 4. ขยับ Icon ตัวละครตามความสูงบน UI
        if (playerIcon != null)
        {
            Vector2 iconPos = playerIcon.anchoredPosition;
            iconPos.y = Mathf.Lerp(iconMinY, iconMaxY, progressRatio);
            playerIcon.anchoredPosition = iconPos;
        }

        // 5. อัปเดตข้อความแสดงระยะทาง
        if (altitudeText != null)
        {
            altitudeText.text = $"{Mathf.FloorToInt(currentAltitude/100):N0}km / {Mathf.FloorToInt(maxAltitude/100):N0}km";
        }

        // 6. เช็กเงื่อนไขเข้าเส้นชัย (30km)
        if (progressRatio >= 1.0f && !hasReachedFinishLine)
        {
            hasReachedFinishLine = true;
            TriggerFinishLine();
        }
    }

    private void TriggerFinishLine()
    {
        Debug.Log("🎉 บรรลุความสูง 30km เข้าเส้นชัยแล้ว!");
        OnReachFinishLine?.Invoke();

        // สามารถใส่ Logic จบเกม เช่น ขึ้นหน้าต่าง Win UI หรือหยุดเกมได้ที่นี่
    }
}