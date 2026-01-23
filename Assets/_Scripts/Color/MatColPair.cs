using UnityEngine;
using UnityEngine.UI;
using static CusElementUI;

public class MatColPair : MonoBehaviour
{
    [SerializeField] CusElementUI elementUI;
    [SerializeField] Image btnImage;
    [SerializeField] ColorPickerControl colorPC;
    [SerializeField] ColorType colorType;

    private void OnEnable()
    {
        btnImage.color = elementUI.GetColor(colorType);
    }

    public void SelectPair() => colorPC.SetCurrentMatColPair(this);

    public void SetColor(Color color)
    {
        elementUI.SetColor(color, colorType);
        btnImage.color = color;
    }

    public Color GetColor() => btnImage.color;
}
