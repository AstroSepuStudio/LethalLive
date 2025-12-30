using TMPro;
using UnityEngine;

public class CusElementUI : MonoBehaviour
{
    public enum ColorType { None, Texture, Main, Secondary, Tertiary, Mask }
    public enum MaterialType { None, Body, Facial, Accesory }

    [Header("References")]
    [SerializeField] SkinData skinData;
    [SerializeField] TextMeshProUGUI accesoryName;

    [Header("Config")]
    [SerializeField] MaterialType matType;

    [SerializeField] int accessoryIndex;

    public Color GetColor(ColorType type) => skinData.GetColor(type, matType, accessoryIndex);

    public void SwitchAccesory(int index)
    {
        skinData.SwitchAccesory(accessoryIndex, index);
        accesoryName.SetText(skinData.Accesories[index].name);
        accessoryIndex = index;
    }

    public void DisableAccessory()
    {
        accesoryName.text = skinData.DisableAccesory(accessoryIndex);
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
                skinData.SetAccesoryColor(accessoryIndex, colorParam, color);
                break;
            default:
                break;
        }
    }
}
