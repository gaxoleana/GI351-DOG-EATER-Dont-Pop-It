using UnityEngine;
using TMPro;

/// <summary>
/// แสดงความสูง (altitude) ของผู้เล่นบน UI แบบ real-time
/// ค่าเริ่มต้นจะโชว์ "ความสูงสูงสุดที่เคยขึ้นถึง" (ไม่ลดลงตอนร่วง/แตก) ให้ความรู้สึกเหมือน score
/// สลับเป็นโชว์ความสูงปัจจุบันตรง ๆ ได้ผ่าน trackHighestOnly
/// </summary>
public class AltitudeUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Transform ของ player ใช้คำนวณความสูง")]
    public Transform playerTransform;

    [Tooltip("TextMeshProUGUI ที่จะโชว์ตัวเลขความสูง (ลาก Text object จาก Canvas มาใส่)")]
    public TextMeshProUGUI altitudeText;

    [Header("Settings")]
    [Tooltip("true = โชว์ค่าสูงสุดที่เคยขึ้นถึง (เหมือน score, ไม่ลดตอนร่วง) | false = โชว์ความสูงปัจจุบันตรง ๆ")]
    public bool trackHighestOnly = true;

    [Tooltip("เกิน altitude เท่านี้ ให้เปลี่ยนหน่วยจาก m เป็น km อัตโนมัติ")]
    public bool useKmAboveThreshold = true;
    public float kmThreshold = 1000f;

    [Header("Goal (ใช้โชว์ % ความคืบหน้าถ้าต้องการ)")]
    [Tooltip("เป้าหมายของเกม (ตามดีไซน์ 3000m)")]
    public float goalAltitude = 3000f;

    private float startY;
    private float highestAltitude;

    // เผื่อระบบอื่น (win condition, UI อื่น ๆ) อยากอ่านค่าไปใช้ต่อ
    public float CurrentAltitude { get; private set; }
    public float HighestAltitude => highestAltitude;
    public float ProgressRatio => Mathf.Clamp01(highestAltitude / goalAltitude);

    void Start()
    {
        if (playerTransform != null)
        {
            startY = playerTransform.position.y;
        }
        else
        {
            Debug.LogWarning("[AltitudeUI] ยังไม่ได้ผูก Player Transform ใน Inspector");
        }

        if (altitudeText == null)
        {
            Debug.LogWarning("[AltitudeUI] ยังไม่ได้ผูก Altitude Text ใน Inspector");
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        CurrentAltitude = Mathf.Max(0f, playerTransform.position.y - startY);
        highestAltitude = Mathf.Max(highestAltitude, CurrentAltitude);

        if (altitudeText == null) return;

        float displayValue = trackHighestOnly ? highestAltitude : CurrentAltitude;
        altitudeText.text = FormatAltitude(displayValue);
    }

    private string FormatAltitude(float altitude)
    {
        if (useKmAboveThreshold && altitude >= kmThreshold)
        {
            float km = altitude / 1000f;
            return $"{km:0.00} km";
        }

        return $"{Mathf.FloorToInt(altitude)} m";
    }
}