using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessingController : MonoBehaviour
{
    [SerializeField] Volume volume;
    [SerializeField] GameObject[] effectGOs;

    Vignette vignette;
    ChromaticAberration chromatic;
    ColorAdjustments colorAdjust;

    readonly List<IPostProcessEffect> effects = new();

    [Header("Max Values")]
    [SerializeField] float maxVignette = 0.5f;
    [SerializeField] float maxChromatic = 0.7f;
    [SerializeField] float minSaturation = -80f;

    float baseVignette;
    float baseChromatic;
    float baseSaturation;
    Color baseVignetteColor;

    void Awake()
    {
        volume.profile = Instantiate(volume.profile);

        volume.profile.TryGet(out vignette);
        volume.profile.TryGet(out chromatic);
        volume.profile.TryGet(out colorAdjust);

        baseVignette = vignette != null ? vignette.intensity.value : 0f;
        baseVignetteColor = vignette != null ? vignette.color.value : Color.white;
        baseChromatic = chromatic != null ? chromatic.intensity.value : 0f;
        baseSaturation = colorAdjust != null ? colorAdjust.saturation.value : 0f;
    }

    private void Start()
    {
        foreach (var go in effectGOs)
        {
            if (!go.TryGetComponent(out IPostProcessEffect effect)) continue;
            effects.Add(effect);
        }
    }

    public void RegisterEffect(IPostProcessEffect effect)
    {
        if (!effects.Contains(effect))
            effects.Add(effect);
    }

    public void UnregisterEffect(IPostProcessEffect effect)
    {
        effects.Remove(effect);
    }

    void Update()
    {
        float dt = Time.deltaTime;

        float vignetteAccum = 0f;
        float chromaticAccum = 0f;
        float saturationAccum = 0f;
        Color vignetteColorAccum = Color.white;
        float colorWeight = 0f;

        foreach (var effect in effects)
        {
            effect.UpdateEffect(dt);

            if (!effect.IsActive) continue;

            vignetteAccum += effect.Vignette;
            chromaticAccum += effect.Chromatic;
            saturationAccum += effect.Saturation;

            float w = effect.Vignette;
            vignetteColorAccum = Color.Lerp(vignetteColorAccum, effect.VignetteColor, w / (colorWeight + w + 0.0001f));
            colorWeight += w;
        }

        vignetteAccum = Mathf.Clamp01(vignetteAccum);
        chromaticAccum = Mathf.Clamp01(chromaticAccum);
        saturationAccum = Mathf.Clamp01(saturationAccum);

        Apply(vignetteAccum, chromaticAccum, saturationAccum, vignetteColorAccum);
    }

    void Apply(float v, float c, float s, Color vignetteColor)
    {
        if (vignette != null)
        {
            vignette.intensity.value = baseVignette + v * maxVignette;
            vignette.color.value = v > 0.01f ? vignetteColor : baseVignetteColor;
        }

        if (chromatic != null)
            chromatic.intensity.value = baseChromatic + c * maxChromatic;

        if (colorAdjust != null)
            colorAdjust.saturation.value = baseSaturation + Mathf.Lerp(0f, minSaturation, s);
    }
}
