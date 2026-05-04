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

    public void SetAmbient(Color color, float intensity, bool enableFog = false)
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = color;
        RenderSettings.ambientIntensity = intensity;

        if (enableFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogStartDistance = 20;
            RenderSettings.fogEndDistance = 30;
        }

        DynamicGI.UpdateEnvironment();
    }

    public void ResetAmbient() => SetAmbient(officeAmbienceClr, officeAmbienceIntensity, true);
}
