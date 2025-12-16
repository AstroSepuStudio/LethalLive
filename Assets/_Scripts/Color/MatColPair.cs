using UnityEngine;

public class MatColPair : MonoBehaviour
{
    enum ColorType { None, Texture, Main, Secondary, Tertiary, Mask }
    enum MaterialType { None, Body, Facial, Accesory }

    [SerializeField] SkinData skinData;
    [SerializeField] ColorPickerControl colorPC;
    [SerializeField] MaterialType matType;
    [SerializeField] ColorType colorType;
    [SerializeField] int accesoryIndex;

    public void SelectPair() => colorPC.SetCurrentMatColPair(this);

    public void SetColor(Color color)
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
                skinData.SetAccesoryColor(accesoryIndex, colorParam, color);
                break;
            default:
                break;
        }
    }
}
