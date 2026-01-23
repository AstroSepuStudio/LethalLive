using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CusElementUI : MonoBehaviour
{
    public enum ColorType { None, Texture, Main, Secondary, Tertiary, Mask }
    public enum MaterialType { None, Body, Facial, Accesory }

    [Header("References")]
    [SerializeField] SkinData skinData;
    [SerializeField] TextMeshProUGUI accesoryName;
    [SerializeField] Slider glowSlider;

    [Header("Config")]
    [SerializeField] MaterialType matType;
    [SerializeField] SkinData.AccessoryType accesoryType;

    public Color GetColor(ColorType type) => skinData.GetColor(accesoryType, type, matType);

    private void OnEnable()
    {
        if (glowSlider != null)
            glowSlider.SetValueWithoutNotify(skinData.GetFacialGlow());

        string name = skinData.GetAccessoryName(accesoryType);
        if (name != null)
            accesoryName.SetText(name);
    }

    public void SwitchAccesory(int index)
    {
        skinData.SwitchAccesory(index);
        accesoryName.SetText(skinData.Accesories[index].name);
    }

    public void DisableAccessory()
    {
        accesoryName.text = skinData.DisableAccesory(accesoryType);
    }

    public void SetIntensity(float intensity) => skinData.SetFacialGlow(intensity);

    public void SetColor(Color color, ColorType colorType)
    {
        string colorParam = "";
        switch (colorType)
        {
            case ColorType.Texture:
                colorParam = "_TextureColor";
                break;
            case ColorType.Main:
                colorParam = "_MainColor";
                break;
            case ColorType.Secondary:
                colorParam = "_SecondColor";
                break;
            case ColorType.Tertiary:
                colorParam = "_ThirdColor";
                break;
            case ColorType.Mask:
                colorParam = "_MultiplyColor";
                break;
        }

        if (colorParam.Equals("")) return;

        switch (matType)
        {
            case MaterialType.Body:
                skinData.SetBodyColor(colorParam, color);
                break;
            case MaterialType.Facial:
                skinData.SetFacialColor(colorParam, color);
                break;
            case MaterialType.Accesory:
                skinData.SetAccesoryColor(accesoryType, colorParam, color);
                break;
            default:
                break;
        }
    }
}
