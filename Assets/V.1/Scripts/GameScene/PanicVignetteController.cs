using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PanicVignetteController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Volume globalVolume;

    [Header("Vignette Settings")]
    [SerializeField] private float targetIntensity = 0.4f;
    [SerializeField] private float fadeSpeed = 3f;

    private Vignette vignette;
    private float targetValue = 0f;

    void Start()
    {
        if (globalVolume != null)
        {
            globalVolume.profile.TryGet(out vignette);
        }
    }

    void Update()
    {
        if (vignette == null) return;

        vignette.intensity.value = Mathf.Lerp(
            vignette.intensity.value,
            targetValue,
            Time.deltaTime * fadeSpeed
        );
    }

    // เรียกจาก PanicEventManagerVariantNoStop ตอน Red/Blue event เริ่ม/จบ
    public void SetVignetteActive(bool active)
    {
        targetValue = active ? targetIntensity : 0f;
    }

    public void SetVignetteColor(Color color)
    {
        if (vignette != null)
        {
            vignette.color.value = color;
        }
    }
}