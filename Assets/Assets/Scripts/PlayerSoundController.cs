using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerSoundController : MonoBehaviour
{
    [Header("Sound Clips")]
    [SerializeField] private AudioClip blowSound;
    [SerializeField] private AudioClip popSound;

    private AudioSource audioSource;
    private bool wasHolding = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        bool isHolding = Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space);

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
        if (blowSound == null) return;
        audioSource.clip = blowSound;
        audioSource.loop = true;
        audioSource.Play();
    }

    private void StopBlowSound()
    {
        if (audioSource.isPlaying && audioSource.clip == blowSound)
        {
            audioSource.Stop();
        }
    }

    public void PlayPopSound()
    {
        StopBlowSound();
        if (popSound != null) audioSource.PlayOneShot(popSound);
    }
}