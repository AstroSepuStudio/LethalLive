using System.Collections;
using UnityEngine;

public class SellPointSign : MonoBehaviour
{
    [SerializeField] Renderer signRenderer;
    [SerializeField] Light signLight;
    [SerializeField] int materialIndex = 1;

    [Header("Emission")]
    [SerializeField] Color emissionColor = Color.green;
    [SerializeField] float maxEmissionIntensity = 2f;

    [Header("Light")]
    [SerializeField] float maxLightIntensity = 1f;

    Material maMat;
    static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        Material[] mats = signRenderer.materials;
        maMat = new Material(mats[materialIndex]);
        mats[materialIndex] = maMat;
        signRenderer.materials = mats;

        SetBrightness(0f);
    }

    public void SetBrightness(float t)
    {
        t = Mathf.Clamp01(t);

        maMat.SetColor(EmissionColor, emissionColor * (t * maxEmissionIntensity));

        if (signLight != null)
            signLight.intensity = t * maxLightIntensity;
    }

    public void FadeIn(float duration) => StartCoroutine(FadeRoutine(0f, 1f, duration));
    public void FadeOut(float duration) => StartCoroutine(FadeRoutine(1f, 0f, duration));

    public IEnumerator FadeRoutine(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetBrightness(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }
        SetBrightness(to);
    }
}

