using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DazedEffectController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Volume globalVolume;
    [SerializeField] private GumController gum;

    [Header("Chromatic Aberration Settings")]
    [SerializeField] private float targetIntensity = 0.6f;
    [SerializeField] private float fadeSpeed = 4f;

    private ChromaticAberration chromaticAberration;
    private float targetValue = 0f;

    void Start()
    {
        if (gum == null) gum = FindAnyObjectByType<GumController>();
        if (globalVolume != null)
        {
            globalVolume.profile.TryGet(out chromaticAberration);
        }

        if (gum != null)
        {
            gum.OnDazedStart += HandleDazedStart;
            gum.OnDazedEnd += HandleDazedEnd;
        }
    }

    void OnDestroy()
    {
        if (gum != null)
        {
            gum.OnDazedStart -= HandleDazedStart;
            gum.OnDazedEnd -= HandleDazedEnd;
        }
    }

    void Update()
    {
        if (chromaticAberration == null) return;

        chromaticAberration.intensity.value = Mathf.Lerp(
            chromaticAberration.intensity.value,
            targetValue,
            Time.deltaTime * fadeSpeed
        );
    }

    private void HandleDazedStart()
    {
        targetValue = targetIntensity;
    }

    private void HandleDazedEnd()
    {
        targetValue = 0f;
    }
}