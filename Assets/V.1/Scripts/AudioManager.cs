using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioMixer mixer;

    private const string MASTER_KEY = "MasterVolume";
    private const string MUSIC_KEY = "MusicVolume";
    private const string SFX_KEY = "SFXVolume";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance()
    {
        Instance = null;
    }

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
    }

    void Start()
    {
        // AudioMixer parameters are not ready during Awake.
        if (Instance == this)
            LoadVolumeSettings();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
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
        float clamped = Mathf.Clamp01(value);
        ApplyMixerVolume(parameterName, clamped);
        PlayerPrefs.SetFloat(parameterName, clamped);
        PlayerPrefs.Save();
    }

    private void ApplyMixerVolume(string parameterName, float value)
    {
        if (mixer == null) return;

        // Zero on the slider maps to the mixer's silence floor (-80 dB).
        float dB = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;
        mixer.SetFloat(parameterName, dB);
    }

    private void LoadVolumeSettings()
    {
        ApplyMixerVolume(MASTER_KEY, GetVolume(MASTER_KEY));
        ApplyMixerVolume(MUSIC_KEY, GetVolume(MUSIC_KEY));
        ApplyMixerVolume(SFX_KEY, GetVolume(SFX_KEY));
    }

    // ---------- ให้ UI slider ดึงค่าปัจจุบันไปตั้งตอนเปิดหน้า Settings ----------

    public float GetVolume(string parameterName)
    {
        return Mathf.Clamp01(PlayerPrefs.GetFloat(parameterName, 1f));
    }
}
