using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioMixer mixer;

    private const string MASTER_KEY = "MasterVolume";
    private const string MUSIC_KEY = "MusicVolume";
    private const string SFX_KEY = "SFXVolume";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadVolumeSettings();
    }

    // ---------- Playback (เดิม + null guard) ----------

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (musicSource == null || clip == null) return;
        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    // ---------- Volume control ----------

    public void SetMasterVolume(float value)
    {
        SetMixerVolume(MASTER_KEY, value);
    }

    public void SetMusicVolume(float value)
    {
        SetMixerVolume(MUSIC_KEY, value);
    }

    public void SetSFXVolume(float value)
    {
        SetMixerVolume(SFX_KEY, value);
    }

    private void SetMixerVolume(string parameterName, float value)
    {
        if (mixer == null) return;

        // value: 0.0001 - 1 (linear) -> dB (log)
        float clamped = Mathf.Clamp(value, 0.0001f, 1f);
        float dB = Mathf.Log10(clamped) * 20f;

        mixer.SetFloat(parameterName, dB);
        PlayerPrefs.SetFloat(parameterName, value);
        PlayerPrefs.Save();
    }

    private void LoadVolumeSettings()
    {
        if (mixer == null) return;

        SetMixerVolume(MASTER_KEY, PlayerPrefs.GetFloat(MASTER_KEY, 1f));
        SetMixerVolume(MUSIC_KEY, PlayerPrefs.GetFloat(MUSIC_KEY, 1f));
        SetMixerVolume(SFX_KEY, PlayerPrefs.GetFloat(SFX_KEY, 1f));
    }

    // ---------- ให้ UI slider ดึงค่าปัจจุบันไปตั้งตอนเปิดหน้า Settings ----------

    public float GetVolume(string parameterName)
    {
        return PlayerPrefs.GetFloat(parameterName, 1f);
    }
}