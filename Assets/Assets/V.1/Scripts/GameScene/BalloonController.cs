using UnityEngine;
using System;

public class BalloonController : MonoBehaviour
{
    [Header("Size Settings")]
    public float currentSize = 1f;
    public float minSize = 0.3f;
    public float maxSize = 3f;
    public float inflateRate = 1.5f;   // อัตราการขยายตอนกดเป่า
    public float deflateRate = 0.8f;   // อัตราการหดตอนปล่อย (ค่อยๆ หด)

    [Header("Stamina (ลมหายใจ)")]
    public float currentStamina = 100f;
    public float maxStamina = 100f;
    public float staminaDrainRate = 15f;   // ใช้ลมตอนเป่า
    public float staminaRegenRate = 10f;   // ฟื้นตอนไม่เป่า

    [Header("DeadZone (ขนาดที่แตกได้)")]
    public float deadZoneMin = 0.5f;
    public float deadZoneMax = 2.5f;

    public bool IsPopped { get; private set; }
    public bool IsBlowing { get; private set; }

    bool hasStartedBlowing;   // true ตั้งแต่กด input ครั้งแรก ก่อนหน้านี้จะยังไม่เช็ค deadzone

    // Events ให้ manager อื่นมา subscribe แทนที่จะให้ BalloonController รู้จักคนอื่น
    public event Action OnPop;
    public event Action OnStaminaEmpty;
    public event Action<float> OnSizeChanged;     // ส่งขนาดปัจจุบันออกไป (เผื่อ UI ใช้)
    public event Action<float> OnStaminaChanged;

    void Start()
    {
        // sync ค่าเริ่มต้นให้ตรงกับ currentSize ที่ตั้งไว้ใน Inspector
        // กัน transform.localScale (ที่ตั้งด้วยมือ) กับ currentSize (ค่าที่ script ใช้คำนวณ) ไม่ตรงกัน
        transform.localScale = Vector3.one * currentSize;
    }

    void Update()
    {
        if (IsPopped) return;

        HandleInput();
        HandleStamina();

        if (!hasStartedBlowing) return; // ยังไม่เคยกด input เลย ไม่เช็ค deadzone

        CheckDeadZone();
    }

    void HandleInput()
    {
        bool inputHeld = Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0); // Space หรือ LMB
        IsBlowing = inputHeld && currentStamina > 0f;

        if (IsBlowing)
        {
            hasStartedBlowing = true;
            Inflate(inflateRate * Time.deltaTime);
        }
        else if (hasStartedBlowing)
        {
            // deflate เฉพาะหลังจากเคยกด input ไปแล้วเท่านั้น
            // ก่อนหน้านั้นให้ค้างที่ขนาดเริ่มต้น กันหดจนต่ำกว่า deadzone ก่อนเริ่มเช็คด้วยซ้ำ
            Deflate(deflateRate * Time.deltaTime);
        }
    }

    void HandleStamina()
    {
        if (IsBlowing)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                OnStaminaEmpty?.Invoke();
            }
        }
        else
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
        }

        OnStaminaChanged?.Invoke(currentStamina / maxStamina); // ส่งเป็น 0-1 ให้ UI ใช้ทำบาร์ง่ายๆ
    }

    void Inflate(float amount)
    {
        SetSize(currentSize + amount);
    }

    void Deflate(float amount)
    {
        SetSize(currentSize - amount);
    }

    void SetSize(float newSize)
    {
        currentSize = Mathf.Clamp(newSize, minSize, maxSize);
        transform.localScale = Vector3.one * currentSize;
        OnSizeChanged?.Invoke(currentSize);
    }

    void CheckDeadZone()
    {
        if (currentSize <= deadZoneMin || currentSize >= deadZoneMax)
        {
            Pop();
        }
    }

    // เรียกจากภายนอกได้ด้วย เช่นตอน Event fail หรือโดน enemy ชน
    public void Pop()
    {
        if (IsPopped) return;

        IsPopped = true;
        OnPop?.Invoke();
    }

    // ใช้ตอนจบ cooldown แล้วให้เริ่มเป่าใหม่ได้
    public void ResetBalloon()
    {
        IsPopped = false;
        currentSize = 1f;
        currentStamina = maxStamina;
        transform.localScale = Vector3.one;
        hasStartedBlowing = false;
    }

    // เผื่อ Event (เช่น Free event) ต้องการ regen stamina ให้ manual
    public void RegenerateStamina(float amount)
    {
        currentStamina = Mathf.Min(currentStamina + amount, maxStamina);
    }
}