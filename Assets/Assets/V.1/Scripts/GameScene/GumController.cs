using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// จัดการขนาดหมากฝรั่ง (gum), DeadZone threshold ที่ขยายขึ้นตามความสูง,
/// และ Pop/Reset cycle. ตัวนี้เป็น "หัวใจ" ของ core loop ทั้งหมด
/// </summary>
public class GumController : MonoBehaviour
{
    public enum GumState
    {
        Normal,     // เป่าได้ปกติ
        Popped,     // เพิ่งแตก รอ reset (ร่วงฟรีสั้น ๆ)
        Dazed       // โดน obstacle มึนงง เป่าไม่ได้
    }

    [Header("Base Settings")]
    [Tooltip("ขนาด DeadZone เริ่มต้น (ถือว่าแตกเมื่อ currentSize >= threshold นี้)")]
    public float baseDeadZoneThreshold = 1.0f;

    [Tooltip("อัตราการโตของ gum ต่อวินาทีตอนกดเป่า")]
    public float growthRate = 1.2f;

    [Tooltip("อัตราการยุบของ gum ต่อวินาทีตอนปล่อย")]
    public float shrinkRate = 1.5f;

    [Header("DeadZone Scaling (ยิ่งสูงยิ่งขยาย)")]
    [Tooltip("ทุกกี่เมตร ขยับ threshold หนึ่ง step")]
    public float altitudeStep = 600f;

    [Tooltip("ขยาย threshold เพิ่มกี่หน่วยต่อ step (linear)")]
    public float growthPerStep = 0.2f;

    [Tooltip("เพดานสูงสุดของ threshold กันไม่ให้ balance พัง")]
    public float maxDeadZoneThreshold = 3.0f;

    [Header("Timing")]
    [Tooltip("ช่วงร่วงฟรีหลังแตก ก่อนจะเป่าลูกใหม่ได้ (วินาที)")]
    public float popRecoveryTime = 0.4f;

    [Tooltip("ระยะเวลามึนงงหลังโดน obstacle (วินาที)")]
    public float dazedDuration = 0.7f;

    [Header("Visual")]
    [Tooltip("Transform ของ sprite หมากฝรั่งที่จะ scale ตามขนาดจริง (ลาก sprite ลูกโป่งมาใส่)")]
    public Transform gumVisual;

    [Tooltip("scale ตอน gum ยังไม่พองเลย (currentSize = 0)")]
    public float minVisualScale = 0.2f;

    [Tooltip("scale ตอน gum ใหญ่เต็ม DeadZone ปัจจุบัน (currentSize = threshold)")]
    public float maxVisualScale = 1.5f;

    [Header("DeadZone Warning Ring")]
    [Tooltip("Transform ของ sprite วงแหวน (ring/circle outline) ที่จะอยู่รอบ gum — วาง sprite เป็นวงกลมโปร่งกลาง แล้วลากมาใส่")]
    public Transform ringVisual;

    [Tooltip("SpriteRenderer ของวงแหวน (ถ้าไม่ใส่ จะลองหาจาก ringVisual เอง)")]
    public SpriteRenderer ringRenderer;

    [Tooltip("สีวงแหวนตอนเริ่มเห็น (โปร่งใสน้อย)")]
    public Color ringWarningColor = new Color(1f, 0.6f, 0f, 0.5f); // ส้ม โปร่งครึ่ง

    [Tooltip("สีวงแหวนตอนอันตรายเต็มที่")]
    public Color ringDangerColor = new Color(1f, 0f, 0f, 1f); // แดงเข้ม ทึบ

    [Tooltip("sizeRatio ที่วงแหวนเริ่ม fade in (0-1)")]
    [Range(0f, 1f)] public float warningRatioStart = 0.6f;

    [Tooltip("sizeRatio ที่วงแหวนแดงเต็มที่ + เริ่ม pulse (0-1)")]
    [Range(0f, 1f)] public float dangerRatioStart = 0.85f;

    [Tooltip("วงแหวนใหญ่กว่า gum เท่าไหร่ (คูณกับ maxVisualScale) เพื่อให้เห็นเป็นขอบรอบนอก")]
    public float ringSizeMultiplier = 1.3f;

    [Tooltip("ความเร็ว pulse (กระพริบ) ตอนอยู่โซนอันตราย")]
    public float dangerPulseSpeed = 8f;

    [Tooltip("ความแรงของ pulse scale เพิ่มเติมตอนอันตราย")]
    public float dangerPulseAmount = 0.08f;

    [Header("Camera Shake (Cinemachine Impulse)")]
    [Tooltip("CinemachineImpulseSource บน player (Add Component > Cinemachine Impulse Source)")]
    public CinemachineImpulseSource impulseSource;

    [Tooltip("ความแรงสั่นตอนเริ่มเข้าโซนอันตราย")]
    public float shakeForceAtDangerStart = 0.1f;

    [Tooltip("ความแรงสั่นตอนใกล้แตกสุด ๆ (ratio ใกล้ 1)")]
    public float shakeForceAtMaxRisk = 0.35f;

    [Tooltip("ทุกกี่วินาที ยิง impulse สั่นหนึ่งครั้งตอนอยู่โซนอันตราย")]
    public float shakeInterval = 0.15f;

    [Tooltip("ความแรงสั่นตอนหมากฝรั่งแตก (impulse เดียว ครั้งเดียว)")]
    public float shakeForceOnPop = 0.6f;

    [Header("Runtime (อ่านอย่างเดียว)")]
    public GumState currentState = GumState.Normal;
    public float currentSize = 0f;
    public float currentDeadZoneThreshold;

    // Events ให้ PlayerController หรือระบบอื่นฟัง
    public System.Action OnPop;
    public System.Action OnGumReset;
    public System.Action OnDazedStart;
    public System.Action OnDazedEnd;

    private Transform playerTransform;
    private float startY;
    private float stateTimer;
    private float shakeTimer;

    /// <summary>
    /// เรียกตอน spawn player เพื่อผูก transform สำหรับคำนวณ altitude
    /// </summary>
    public void Init(Transform playerTf)
    {
        playerTransform = playerTf;
        startY = playerTransform.position.y;
        currentDeadZoneThreshold = baseDeadZoneThreshold;

        if (ringRenderer == null && ringVisual != null)
        {
            ringRenderer = ringVisual.GetComponent<SpriteRenderer>();
        }

        // เริ่มต้นให้วงแหวนโปร่งใสสนิท (ยังไม่เห็น)
        if (ringRenderer != null)
        {
            Color c = ringWarningColor;
            c.a = 0f;
            ringRenderer.color = c;
        }
    }

    void Update()
    {
        UpdateDeadZoneThreshold();
        UpdateVisualScale();
        UpdateDangerShake();

        switch (currentState)
        {
            case GumState.Normal:
                // การโต/ยุบของ gum ถูกควบคุมจาก PlayerController ผ่าน Grow()/Shrink()
                if (currentSize >= currentDeadZoneThreshold)
                {
                    Pop();
                }
                break;

            case GumState.Popped:
                TickTimer(popRecoveryTime, ResetGum);
                break;

            case GumState.Dazed:
                TickTimer(dazedDuration, EndDazed);
                break;
        }
    }

    /// <summary>
    /// scale sprite ตาม currentSize เทียบกับ threshold ปัจจุบัน (0 = ยุบสุด, 1 = ใกล้แตก)
    /// ตอน Popped จะ scale เหลือ minVisualScale ทันทีให้เห็นว่าแตกแล้ว
    /// </summary>
    private void UpdateVisualScale()
    {
        float sizeRatio = GetSizeRatio(); // 0-1
        float pulse = 0f;

        // pulse เฉพาะตอน Normal + อยู่โซนอันตราย ให้ความรู้สึกเร่งเร้า
        if (currentState == GumState.Normal && sizeRatio >= dangerRatioStart)
        {
            pulse = Mathf.Sin(Time.time * dangerPulseSpeed) * dangerPulseAmount;
        }

        if (gumVisual != null)
        {
            float targetScale = currentState == GumState.Popped
                ? minVisualScale
                : Mathf.Lerp(minVisualScale, maxVisualScale, sizeRatio) + pulse;

            gumVisual.localScale = Vector3.one * targetScale;
        }

        UpdateDeadZoneRing(sizeRatio, pulse);
    }

    /// <summary>
    /// วงแหวนแดงรอบ gum: fade in ตั้งแต่ warningRatioStart, เข้มขึ้นจนแดงเต็มที่ที่ dangerRatioStart,
    /// แล้ว pulse ตามจังหวะเดียวกับ gum ตอนอยู่โซนอันตราย
    /// </summary>
    private void UpdateDeadZoneRing(float sizeRatio, float pulse)
    {
        if (ringVisual == null) return;

        // ขนาดวงแหวน = ตามขนาด gum ปัจจุบัน + ระยะห่างคงที่ (ringSizeMultiplier)
        float gumScale = currentState == GumState.Popped
            ? minVisualScale
            : Mathf.Lerp(minVisualScale, maxVisualScale, sizeRatio);
        ringVisual.localScale = Vector3.one * (gumScale * ringSizeMultiplier + pulse);

        if (ringRenderer == null) return;

        if (currentState == GumState.Popped || sizeRatio < warningRatioStart)
        {
            // ยังปลอดภัย หรือเพิ่งแตกไป — วงแหวนไม่ต้องโชว์
            Color hidden = ringWarningColor;
            hidden.a = 0f;
            ringRenderer.color = hidden;
            return;
        }

        float t = Mathf.InverseLerp(warningRatioStart, dangerRatioStart, sizeRatio);
        ringRenderer.color = Color.Lerp(ringWarningColor, ringDangerColor, t);
    }

    /// <summary>
    /// สั่นกล้องเบา ๆ ต่อเนื่องตอนอยู่โซนอันตราย ความแรงเพิ่มขึ้นตาม sizeRatio
    /// (ใกล้ dangerRatioStart = สั่นเบา, ใกล้ 1 = สั่นแรงสุด)
    /// </summary>
    private void UpdateDangerShake()
    {
        if (impulseSource == null) return;
        if (currentState != GumState.Normal) return;

        float sizeRatio = GetSizeRatio();
        if (sizeRatio < dangerRatioStart) return;

        shakeTimer += Time.deltaTime;
        if (shakeTimer < shakeInterval) return;
        shakeTimer = 0f;

        float t = Mathf.InverseLerp(dangerRatioStart, 1f, sizeRatio);
        float force = Mathf.Lerp(shakeForceAtDangerStart, shakeForceAtMaxRisk, t);

        impulseSource.GenerateImpulseWithForce(force);
    }

    /// <summary>
    /// สั่นกล้องแรง ๆ ครั้งเดียวตอนหมากฝรั่งแตก (แยกจากสั่นต่อเนื่องตอนใกล้แตก)
    /// </summary>
    private void FirePopShake()
    {
        if (impulseSource == null) return;
        impulseSource.GenerateImpulseWithForce(shakeForceOnPop);
    }

    private void TickTimer(float duration, System.Action onComplete)
    {
        stateTimer += Time.deltaTime;
        if (stateTimer >= duration)
        {
            stateTimer = 0f;
            onComplete?.Invoke();
        }
    }

    private float GetCurrentAltitude()
    {
        if (playerTransform == null) return 0f;
        return Mathf.Max(0f, playerTransform.position.y - startY);
    }

    private void UpdateDeadZoneThreshold()
    {
        float altitude = GetCurrentAltitude();
        int stepsPassed = Mathf.FloorToInt(altitude / altitudeStep);

        float grown = baseDeadZoneThreshold + (stepsPassed * growthPerStep);
        currentDeadZoneThreshold = Mathf.Min(grown, maxDeadZoneThreshold);

        
    }

    /// <summary>เรียกจาก PlayerController ตอนกดเป่า (เฉพาะตอน state = Normal)</summary>
    public void Grow(float deltaTime)
    {
        if (currentState != GumState.Normal) return;
        currentSize = Mathf.Min(currentSize + growthRate * deltaTime, currentDeadZoneThreshold);
    }

    /// <summary>เรียกจาก PlayerController ตอนปล่อย (เฉพาะตอน state = Normal)</summary>
    public void Shrink(float deltaTime)
    {
        if (currentState != GumState.Normal) return;
        currentSize = Mathf.Max(currentSize - shrinkRate * deltaTime, 0f);
    }

    private void Pop()
    {
        currentState = GumState.Popped;
        stateTimer = 0f;
        FirePopShake();
        OnPop?.Invoke();
    }

    private void ResetGum()
    {
        currentSize = 0f;
        currentState = GumState.Normal;
        OnGumReset?.Invoke();
    }

    /// <summary>เรียกจากระบบ Obstacle ตอน player โดน hitbox ตัว (ไม่ใช่ hitbox gum)</summary>
    public void TriggerDazed()
    {
        if (currentState == GumState.Dazed) return; // กัน re-trigger ซ้อน
        currentState = GumState.Dazed;
        stateTimer = 0f;
        currentSize = 0f; // มึนแล้ว gum ยุบ ต้องเริ่มใหม่ตอนหายมึน
        OnDazedStart?.Invoke();
    }

    private void EndDazed()
    {
        currentState = GumState.Normal;
        OnDazedEnd?.Invoke();
    }

    /// <summary>ให้ PlayerController เอาไปคูณ lift force (0-1)</summary>
    public float GetSizeRatio()
    {
        if (currentDeadZoneThreshold <= 0f) return 0f;
        return currentSize / currentDeadZoneThreshold;
    }

    /// <summary>เช็คว่าตอนนี้เป่าได้ไหม (Normal state เท่านั้น)</summary>
    public bool CanBlow() => currentState == GumState.Normal;
}