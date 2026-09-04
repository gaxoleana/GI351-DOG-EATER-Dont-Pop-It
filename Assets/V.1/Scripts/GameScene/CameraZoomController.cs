using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// ควบคุมการ Zoom Out ของ CinemachineCamera (v3.1.7) 
/// ตามการขยายตัวของ DeadZoneThreshold ใน GumController
/// </summary>
public class CameraZoomController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("CinemachineCamera (v3.1.7) — ถ้าไม่ใส่จะดึงจากตัวมันเอง")]
    public CinemachineCamera vcam;

    [Tooltip("GumController สำหรับอ่านค่า currentDeadZoneThreshold")]
    public GumController gum;

    [Header("Zoom Settings")]
    [Tooltip("ขนาด OrthographicSize ของกล้องตอน DeadZone อยู่ระดับเริ่มต้น ( baseThreshold )")]
    public float baseOrthoSize = 5f;

    [Tooltip("ขนาด OrthographicSize สูงสุด ตอน DeadZone ขยายเต็มที่ ( maxThreshold )")]
    public float maxOrthoSize = 8.5f;

    [Tooltip("ความนุ่มนวลในการซูมเปลี่ยนขนาดกล้อง (ยิ่งเยอะยิ่งเปลี่ยนเร็ว)")]
    public float zoomSpeed = 2f;

    void Start()
    {
        if (vcam == null) vcam = GetComponent<CinemachineCamera>();
        if (gum == null) gum = FindAnyObjectByType<GumController>();

        // ตั้งค่าขนาดกล้องเริ่มต้น
        if (vcam != null)
        {
            LensSettings lens = vcam.Lens;
            lens.OrthographicSize = baseOrthoSize;
            vcam.Lens = lens;
        }
    }

    void Update()
    {
        if (vcam == null || gum == null) return;

        // 1. คำนวณสัดส่วนการเติบโตของ Threshold (0.0 = base, 1.0 = max)
        float thresholdRange = gum.maxDeadZoneThreshold - gum.baseDeadZoneThreshold;
        float ratio = thresholdRange > 0f
            ? Mathf.Clamp01((gum.currentDeadZoneThreshold - gum.baseDeadZoneThreshold) / thresholdRange)
            : 0f;

        // 2. คำนวณ OrthographicSize เป้าหมาย
        float targetOrthoSize = Mathf.Lerp(baseOrthoSize, maxOrthoSize, ratio);

        // 3. ปรับค่า Lens.OrthographicSize สำหรับ Cinemachine 3.x
        LensSettings lens = vcam.Lens;
        lens.OrthographicSize = Mathf.Lerp(lens.OrthographicSize, targetOrthoSize, Time.deltaTime * zoomSpeed);
        vcam.Lens = lens;
    }
}