using UnityEngine;

public class PlayerSoundController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("ลาก GumController มาใส่ (ตัวเดียวกับที่ผูกกับ Player)")]
    [SerializeField] private GumController gum;

    [Header("Sound Clips")]
    [SerializeField] private AudioClip blowSound;
    [SerializeField] private AudioClip popSound;

    [Header("Audio Sources (แยกกัน ห้ามใช้ตัวเดียวกัน)")]
    [Tooltip("AudioSource เฉพาะสำหรับเสียงเป่า (loop) — ตั้ง Loop = true ไว้ล่วงหน้าใน Inspector ก็ได้")]
    [SerializeField] private AudioSource blowSource;

    [Tooltip("AudioSource เฉพาะสำหรับเสียง one-shot เช่น Pop")]
    [SerializeField] private AudioSource sfxSource;

    private bool wasHolding = false;
    private bool isSubscribed = false;
    private bool isBlowPlaying = false; // เช็คสถานะเอง แทนการพึ่ง audioSource.isPlaying

    private void Start()
    {
        if (gum == null) gum = GetComponent<GumController>();
        if (gum == null) gum = FindAnyObjectByType<GumController>();

        if (gum != null && !isSubscribed)
        {
            gum.OnPop += HandlePop;
            isSubscribed = true;
        }
        else if (gum == null)
        {
            Debug.LogWarning("[PlayerSoundController] ไม่พบ GumController — ลาก reference ใส่ Inspector ด้วยมือ");
        }
    }

    private void OnDestroy()
    {
        if (gum != null && isSubscribed)
        {
            gum.OnPop -= HandlePop;
        }
    }

    private void Update()
    {
        bool rawInput = Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space);
        bool isHolding = rawInput && (gum == null || gum.CanBlow());

        if (isHolding && !wasHolding)
        {
            PlayBlowSound();
        }
        else if (!isHolding && wasHolding)
        {
            StopBlowSound();
        }

        wasHolding = isHolding;
    }

    private void PlayBlowSound()
    {
        if (blowSound == null || blowSource == null) return;
        blowSource.clip = blowSound;
        blowSource.loop = true;
        blowSource.Play();
        isBlowPlaying = true;
    }

    private void StopBlowSound()
    {
        if (!isBlowPlaying) return; // เช็คจาก flag ของเราเอง ไม่พึ่ง .isPlaying ที่มโนได้
        blowSource.Stop();
        isBlowPlaying = false;
    }

    private void HandlePop()
    {
        PlayPopSound();
    }

    public void PlayPopSound()
    {
        StopBlowSound();
        if (popSound == null || sfxSource == null) return;
        sfxSource.PlayOneShot(popSound); // ใช้ AudioSource คนละตัวกับ blow เลย ไม่ชนกันอีก
    }
}