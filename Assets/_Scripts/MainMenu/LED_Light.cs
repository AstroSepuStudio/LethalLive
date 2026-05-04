using UnityEngine;

public class LED_Light : MonoBehaviour
{
    [SerializeField] Light ledLight;
    [SerializeField] Renderer lightRenderer;
    [SerializeField] Renderer borderRenderer;

    Material lMat;

    public float Intensity
    {
        get => ledLight.intensity;
        set
        {
            ledLight.intensity = value;
            //lMat.SetColor("_EmissionColor", Color.white * value);
        }
    }

    private void Awake()
    {
        lMat = new Material(lightRenderer.material);
        lightRenderer.material = lMat;
    }

    public void SwitchLight(bool enable)
    {
        ledLight.enabled = enable;

        if (enable)
            lMat.EnableKeyword("_EMISSION");
        else
            lMat.DisableKeyword("_EMISSION");
    }

    public void RenderLight(bool enable)
    {
        ledLight.enabled = enable;
        borderRenderer.enabled = enable;
        lightRenderer.enabled = enable;
    }
}
