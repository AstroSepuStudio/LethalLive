using UnityEngine;

public class EnvironmentLightManager : MonoBehaviour
{
    public static EnvironmentLightManager Instance;

    [SerializeField] Color officeAmbienceClr;
    [SerializeField] float officeAmbienceIntensity;

    private void Awake()
    {
        if (Instance != null) Destroy(Instance);
        Instance = this;
    }

    public void SetAmbient(Color color, float intensity)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = color;
        RenderSettings.ambientIntensity = intensity;

        DynamicGI.UpdateEnvironment();
    }

    public void ResetAmbient() => SetAmbient(officeAmbienceClr, officeAmbienceIntensity);
}
