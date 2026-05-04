using UnityEngine;

public class LowHPEffect : MonoBehaviour, IPostProcessEffect
{
    [SerializeField] float smoothSpeed = 2f;

    float targetHealth01 = 1f;
    float currentSmoothed = 1f;

    public bool IsActive => currentSmoothed < 0.999f;
    public float HealthNormalized => currentSmoothed;

    public float Vignette => 1f - currentSmoothed;
    public float Chromatic => 0f;
    public float Saturation => 1f - currentSmoothed;
    public Color VignetteColor => Color.black;

    public void SetHealthNormalized(float value)
    {
        targetHealth01 = Mathf.Clamp01(value);
    }

    public void UpdateEffect(float dt)
    {
        currentSmoothed = Mathf.MoveTowards(currentSmoothed, targetHealth01, smoothSpeed * dt);
    }
}
