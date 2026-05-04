using UnityEngine;

public interface IPostProcessEffect
{
    bool IsActive { get; }
    void UpdateEffect(float deltaTime);

    float Vignette { get; }
    float Chromatic { get; }
    float Saturation { get; }
    Color VignetteColor { get; }
}
