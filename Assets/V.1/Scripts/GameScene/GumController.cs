using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

/// <summary>
/// จุดความสูงหนึ่งจุดที่จะขยาย DeadZone threshold — กำหนดเป็นค่าตายตัว ไม่ใช่สูตรต่อเนื่อง
/// </summary>
[System.Serializable]
public struct DeadZoneMilestone
{
    [Tooltip("ความสูง (เมตร) ที่จะ trigger การขยาย")]
    public float altitude;

    [Tooltip("บวกเพิ่มเข้า threshold เท่าไหร่ตอนถึงจุดนี้")]
    public float thresholdIncrease;
}

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

    [Tooltip("ขนาดเล็กที่สุดของ gum (ใช้สำหรับ reset)")]
    public float minSize = 0.1f;

    [Header("DeadZone Scaling (ขยายตามระดับความสูงที่กำหนดตายตัว)")]
    [Tooltip("รายการจุดความสูงที่จะขยาย threshold — ถึงจุดไหนก็บวกเพิ่มตามนั้น หลังจุดสุดท้ายจะไม่ขยายอีก")]
    public DeadZoneMilestone[] milestones = new DeadZoneMilestone[]
    {
        new DeadZoneMilestone { altitude = 400f, thresholdIncrease = 0.2f },
        new DeadZoneMilestone { altitude = 600f, thresholdIncrease = 0.2f },
        new DeadZoneMilestone { altitude = 800f, thresholdIncrease = 0.2f },
        new DeadZoneMilestone { altitude = 1000f, thresholdIncrease = 0.2f },
    };

    [Tooltip("เพดานสูงสุดของ threshold กันไว้เผื่อ (ปกติ milestone ท้ายสุดจะเป็นตัวจบเองอยู่แล้ว)")]
    public float maxDeadZoneThreshold = 3.0f;

    [Header("Timing")]
    [Tooltip("ช่วงร่วงฟรีหลังแตกจาก Threshold (เป่าเกิน) ก่อนจะเป่าลูกใหม่ได้ (วินาที)")]
    public float popRecoveryTime = 4f;

    [Tooltip("ระยะเวลามึนงงหลังโดน obstacle (วินาที) — ปัจจุบันยังไม่ถูกใช้งานจริง (ดู normalStunDuration แทน)")]
    public float dazedDuration = 0.7f;

    [Header("Recovery Settings")]
    [Tooltip("ระยะเวลาฟื้นตัวเมื่อโดนชนที่ตัว Player (วินาที) — เรียกผ่าน ForcePop()")]
    public float normalStunDuration = 5f;

    [Tooltip("ระยะเวลาฟื้นตัวเมื่อโดนชนที่ตัว หมากฝรั่งโดยตรง (วินาที)")]
    public float fastStunDuration = 1f;

    [Tooltip("ระยะเวลาฟื้นตัวเมื่อแตกเพราะทำ R/B Panic Event ไม่สำเร็จ (วินาที)")]
    public float panicEventStunDuration = 4f;

    [Header("Visual")]
    [Tooltip("Transform ของ sprite หมากฝรั่งที่จะ scale ตามขนาดจริง (ลาก sprite ลูกโป่งมาใส่)")]
    public Transform gumVisual;

    [Tooltip("ลาก SpriteRenderer ของหมากฝรั่งมาใส่ช่องนี้")]
    public SpriteRenderer gumSpriteRenderer;

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
    private bool isForcedRecovery; // true ตอนแตกจาก ForcePop() กันไม่ให้ auto-recovery (popRecoveryTime) มาแย่งรีเซ็ตก่อนเวลา

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
                // auto-recovery (popRecoveryTime) ใช้เฉพาะตอนแตกเองจากเป่าเกิน threshold เท่านั้น
                // ถ้าเป็น ForcePop (โดน obstacle) จะมี RecoveryRoutine ของตัวเองคุมเวลาแทน ไม่ให้มาชนกัน
                if (!isForcedRecovery)
                {
                    TickTimer(popRecoveryTime, ResetGum);
                }
                break;

            case GumState.Dazed:
                TickTimer(dazedDuration, EndDazed);
                break;
        }
    }

    public float CurrentMaxVisualScale
    {
        get
        {
            if (baseDeadZoneThreshold <= 0f) return maxVisualScale;
            return maxVisualScale * (currentDeadZoneThreshold / baseDeadZoneThreshold);
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
                : Mathf.Lerp(minVisualScale, CurrentMaxVisualScale, sizeRatio) + pulse;

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
            : Mathf.Lerp(minVisualScale, CurrentMaxVisualScale, sizeRatio);
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
        float grown = baseDeadZoneThreshold;

        // บวกเพิ่มทีละ milestone ที่ altitude ปัจจุบันผ่านมาแล้ว
        // หลัง milestone สุดท้าย (1000m ตาม default) จะไม่มีอะไรให้บวกเพิ่มอีก = หยุดขยายเอง
        foreach (var milestone in milestones)
        {
            if (altitude >= milestone.altitude)
            {
                grown += milestone.thresholdIncrease;
            }
        }

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

    /// <summary>
    /// เพิ่มขนาดหมากฝรั่งแบบ Fix Value ต่อการกด 1 ครั้ง (ใช้กับ Red Event)
    /// ป้องกันไม่ให้เกิน Threshold ปัจจุบัน
    /// </summary>
    public void AddSize(float amount)
    {
        if (currentState != GumState.Normal) return;

        // ขยายขนาดขึ้นแบบ Fix Scale แต่ไม่เกิน Threshold ปัจจุบัน
        currentSize = Mathf.Min(currentSize + amount, currentDeadZoneThreshold * 0.98f);
    }

    /// <summary>
    /// รีเซ็ตขนาดหมากฝรั่งกลับไปเป็นขนาดเริ่มต้น (เล็กที่สุด) โดยไม่ทำให้แตก
    /// (visual จะตามมาเองผ่าน UpdateVisualScale() ที่รันทุกเฟรมอยู่แล้ว ไม่ต้องไปยุ่งกับ transform ตรงนี้)
    /// </summary>
    public void ResetToMinSize()
    {
        if (currentState != GumState.Normal) return;
        currentSize = minSize;
    }

    private void Pop()
    {
        currentState = GumState.Popped;
        stateTimer = 0f;
        isForcedRecovery = false;
        FirePopShake();

        // ปิด sprite เหมือนกับ ForcePop() ให้ทุกกรณีที่แตกดูสม่ำเสมอกันหมด
        if (gumSpriteRenderer != null)
        {
            gumSpriteRenderer.enabled = false;
        }

        OnPop?.Invoke();
    }

    /// <summary>
    /// สั่งหมากฝรั่งแตก สามารถกำหนดเวลารีคัฟเวอรี่ customStunTime ได้
    /// </summary>
    public void ForcePop(float customStunDuration = -1f)
    {
        if (currentState == GumState.Popped) return;

        currentState = GumState.Popped;
        isForcedRecovery = true;
        float duration = (customStunDuration > 0f) ? customStunDuration : normalStunDuration;

        // 🔹 ซ่อนรูปหมากฝรั่ง (ตัว GameObject และ Coroutine ยังทำงานต่อได้ปกติ)
        if (gumSpriteRenderer != null)
        {
            gumSpriteRenderer.enabled = false;
        }

        // เรียก Coroutine ได้ตามปกติ ไม่ติด Error แล้ว
        StartCoroutine(RecoveryRoutine(duration));
    }

    private IEnumerator RecoveryRoutine(float stunTime)
    {
        yield return new WaitForSeconds(stunTime);

        // ใช้ 0f แทน minSize เพื่อให้ตรงกับขนาดเล็กสุดจริงของ Normal state
        // (Shrink() ก็ floor ไว้ที่ 0f เหมือนกัน) ถ้าใช้ minSize จะทำให้ scale เริ่มต้นสูงกว่า
        // minVisualScale นิดหน่อย แล้วพอ Shrink() ทำงานต่อ (ถ้ายังไม่ได้กดเป่า) จะเห็น gum
        // หดลงอีกรอบทันทีที่โผล่มา ดูเหมือนขนาดเพี้ยน/กระตุก
        currentSize = 0f;

        // 🔹 แสดงรูปหมากฝรั่งกลับมา
        if (gumSpriteRenderer != null)
        {
            gumSpriteRenderer.enabled = true;
        }

        currentState = GumState.Normal;
        isForcedRecovery = false;
        OnGumReset?.Invoke(); // แจ้งระบบอื่นที่ฟัง event นี้อยู่ด้วย ให้สอดคล้องกับ path ปกติ
    }

    private void ResetGum()
    {
        currentSize = 0f;
        currentState = GumState.Normal;

        // เปิด sprite กลับมา (คู่กับที่ Pop() สั่งปิดไว้)
        if (gumSpriteRenderer != null)
        {
            gumSpriteRenderer.enabled = true;
        }

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