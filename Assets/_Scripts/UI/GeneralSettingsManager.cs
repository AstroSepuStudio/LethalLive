using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GeneralSettingsManager : MonoBehaviour
{
    [SerializeField] Slider sensitivitySlider;
    [SerializeField] TMP_InputField sensitivityIF;

    private void Start()
    {
        sensitivitySlider.SetValueWithoutNotify(SettingsManager.Instance.UserSettings.GetSensitivity());
        sensitivityIF.SetTextWithoutNotify(SettingsManager.Instance.UserSettings.GetSensitivity().ToString());
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
}
