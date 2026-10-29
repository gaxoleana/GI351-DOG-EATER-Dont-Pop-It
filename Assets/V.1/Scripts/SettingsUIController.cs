using UnityEngine;
using UnityEngine.UI;

public class SettingsUIController : MonoBehaviour
{
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    void OnEnable()
    {
        if (masterSlider != null) masterSlider.onValueChanged.AddListener(OnMasterChanged);
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSFXChanged);
        RefreshSliders();
    }

    void Start()
    {
        // OnEnable may run before AudioManager.Awake on another scene object.
        RefreshSliders();
    }

    void OnDisable()
    {
        if (masterSlider != null) masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
        if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(OnSFXChanged);
    }

    private void RefreshSliders()
    {
        if (AudioManager.Instance == null) return;

        if (masterSlider != null)
            masterSlider.SetValueWithoutNotify(AudioManager.Instance.GetVolume("MasterVolume"));
        if (musicSlider != null)
            musicSlider.SetValueWithoutNotify(AudioManager.Instance.GetVolume("MusicVolume"));
        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(AudioManager.Instance.GetVolume("SFXVolume"));
    }

    private void OnMasterChanged(float value) => AudioManager.Instance?.SetMasterVolume(value);
    private void OnMusicChanged(float value) => AudioManager.Instance?.SetMusicVolume(value);
    private void OnSFXChanged(float value) => AudioManager.Instance?.SetSFXVolume(value);
}
