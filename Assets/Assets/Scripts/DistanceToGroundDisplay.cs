using UnityEngine;
using TMPro;

public class DistanceToGroundDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform groundPoint; // ลาก object พื้น/DeadZone มาใส่
    [SerializeField] private TextMeshProUGUI distanceText;

    [Header("Distance Settings")]
    [SerializeField] private float totalDistanceMeters = 4500f;
    [SerializeField] private string suffix = " m";
    [SerializeField] private int decimalPlaces = 0;

    private float startY;
    private float groundY;
    private float totalWorldHeight;

    private void Start()
    {
        if (player == null || groundPoint == null)
        {
            Debug.LogWarning("DistanceToGroundDisplay: ยังไม่ได้ลาก Player หรือ Ground Point ใส่ใน Inspector");
            enabled = false;
            return;
        }

        startY = player.position.y;
        groundY = groundPoint.position.y;
        totalWorldHeight = Mathf.Max(0.0001f, startY - groundY); // กันหาร 0
    }

    private void Update()
    {
        if (distanceText == null) return;

        // ระยะทาง world unit ที่เหลือจาก player ถึงพื้น
        float remainingWorldHeight = Mathf.Max(0f, player.position.y - groundY);

        // แปลงเป็นสัดส่วนแล้วคูณด้วยระยะทางจริงที่กำหนด (4500 เมตร)
        float remainingMeters = (remainingWorldHeight / totalWorldHeight) * totalDistanceMeters;
        remainingMeters = Mathf.Clamp(remainingMeters, 0f, totalDistanceMeters);

        distanceText.text = remainingMeters.ToString("F" + decimalPlaces) + suffix;
    }
}