using UnityEngine;
using UnityEngine.UI;

public class SettingsUIController : MonoBehaviour
{
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    void OnEnable()
    {
        // โหลดค่าที่เคยเซฟไว้มาตั้งตำแหน่ง slider ทุกครั้งที่เปิด panel
        if (AudioManager.Instance == null) return;

        masterSlider.SetValueWithoutNotify(AudioManager.Instance.GetVolume("MasterVolume"));
        musicSlider.SetValueWithoutNotify(AudioManager.Instance.GetVolume("MusicVolume"));
        sfxSlider.SetValueWithoutNotify(AudioManager.Instance.GetVolume("SFXVolume"));
    }

    void Start()
    {
        masterSlider.onValueChanged.AddListener(OnMasterChanged);
        musicSlider.onValueChanged.AddListener(OnMusicChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXChanged);
    }

    private void OnMasterChanged(float value) => AudioManager.Instance?.SetMasterVolume(value);
    private void OnMusicChanged(float value) => AudioManager.Instance?.SetMusicVolume(value);
    private void OnSFXChanged(float value) => AudioManager.Instance?.SetSFXVolume(value);
}