using UnityEngine;

public class TakeDamageEffect : MonoBehaviour, IPostProcessEffect
{
    [SerializeField] float decaySpeed = 2.5f;
    [SerializeField] float minTriggerAmount = 0.3f;

    float intensity;

    public bool IsActive => intensity > 0.01f;

    public float Vignette => intensity;
    public float Chromatic => intensity;
    public float Saturation => intensity * 0.5f;
    public Color VignetteColor => Color.Lerp(Color.white, new Color(0.6f, 0f, 0f), intensity);

    public void Trigger(float amount)
    {
        intensity = Mathf.Clamp01(intensity + Mathf.Max(amount, minTriggerAmount));
    }

    public void UpdateEffect(float dt)
    {
        intensity = Mathf.MoveTowards(intensity, 0f, decaySpeed * dt);
    }
}
