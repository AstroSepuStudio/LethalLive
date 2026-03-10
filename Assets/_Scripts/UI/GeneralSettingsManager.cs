using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LethalLive;

public class GeneralSettingsManager : MonoBehaviour
{
    [SerializeField] Slider sensitivitySlider;
    [SerializeField] TMP_InputField sensitivityIF;
    [SerializeField] FPS_Displayer fpsDisplay;
    [SerializeField] Toggle fpsDisplayT;
    [SerializeField] Toggle avfpsDisplayT;

    private void Start()
    {
        sensitivitySlider.SetValueWithoutNotify(SettingsManager.Instance.UserSettings.GetSensitivity());
        sensitivityIF.SetTextWithoutNotify(SettingsManager.Instance.UserSettings.GetSensitivity().ToString());
        fpsDisplayT.SetIsOnWithoutNotify(fpsDisplay.DisplayFPS);
        avfpsDisplayT.SetIsOnWithoutNotify(fpsDisplay.DisplayAv);
    }

    public void OnMouseSensitivitySetChanged(string value)
    {
        float v = float.Parse(value);
        SettingsManager.Instance.UserSettings.SetSensitivity(v);
        sensitivitySlider.SetValueWithoutNotify(v);
    }

    public void OnMouseSensitivitySetChanged(float value)
    {
        SettingsManager.Instance.UserSettings.SetSensitivity(value);
        sensitivityIF.SetTextWithoutNotify(value.ToString());
    }

    public void OnToggleFPS(bool toggle)
    {
        fpsDisplay.SetDisplayFPS(toggle);
    }

    public void OnToggleAverageFPS(bool toggle)
    {
        fpsDisplay.SetDisplayAverageFPS(toggle);
    }
}
