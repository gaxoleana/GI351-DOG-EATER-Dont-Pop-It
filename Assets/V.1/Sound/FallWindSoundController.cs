using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FallWindSoundController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("ลาก PlayerController มาใส่ (ใช้ดึงความเร็ว Y ปัจจุบัน)")]
    [SerializeField] private Rigidbody2D playerRb;

    [Header("Wind Sound")]
    [Tooltip("เสียงลม/แหวกอากาศ ตั้ง Loop = true ใน AudioSource ไว้ล่วงหน้า")]
    [SerializeField] private AudioClip windLoopClip;

    [Header("Speed Thresholds")]
    [Tooltip("ความเร็วร่วง (ค่าติดลบ) ที่เริ่มได้ยินเสียงลมแผ่วๆ เช่น -3 = เริ่มร่วงนิดหน่อยก็ได้ยินแล้ว")]
    public float minAudibleFallSpeed = -3f;

    [Tooltip("ความเร็วร่วงที่เสียงลมดังเต็มที่ (100% volume) ควรใกล้เคียง maxFallSpeed ใน PlayerController")]
    public float maxFallSpeedForFullVolume = -12f;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float maxVolume = 0.8f;
    [Tooltip("ความเร็วในการ fade volume เข้า/ออก (ยิ่งมากยิ่งไว)")]
    public float volumeSmoothSpeed = 4f;

    [Header("Pitch Settings")]
    public float minPitch = 0.8f;
    public float maxPitch = 1.3f;

    private AudioSource audioSource;
    private float targetVolume = 0f;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = windLoopClip;
        audioSource.loop = true;
        audioSource.volume = 0f;
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        if (playerRb == null)
        {
            var pc = FindAnyObjectByType<PlayerController>();
            if (pc != null) playerRb = pc.GetComponent<Rigidbody2D>();
        }

        if (windLoopClip != null)
        {
            audioSource.Play(); // เล่นวนตลอด ควบคุมแค่ volume
        }
    }

    void Update()
    {
        if (playerRb == null || windLoopClip == null) return;

        float velY = playerRb.linearVelocity.y;

        // เฉพาะตอนร่วง (velY ติดลบ) เท่านั้น ถึงจะมีเสียงลม ตอนลอยขึ้นไม่ต้องมี
        if (velY < minAudibleFallSpeed)
        {
            float ratio = Mathf.InverseLerp(minAudibleFallSpeed, maxFallSpeedForFullVolume, velY);
            targetVolume = Mathf.Lerp(0f, maxVolume, ratio);
            audioSource.pitch = Mathf.Lerp(minPitch, maxPitch, ratio);
        }
        else
        {
            targetVolume = 0f;
        }

        audioSource.volume = Mathf.Lerp(audioSource.volume, targetVolume, Time.deltaTime * volumeSmoothSpeed);
    }
}