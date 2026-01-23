using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ColorPickerControl : MonoBehaviour
{
    [SerializeField] SkinData skinData;
    [SerializeField] SVImageControl svIC;

    [SerializeField] RawImage hueImage;
    [SerializeField] RawImage satValImage;
    [SerializeField] RawImage outputImage;
    [SerializeField] Slider hueSlider;
    [SerializeField] TMP_InputField hexInputField;
    [SerializeField] MatColPair currentPair;

    Texture2D hueTexture, svTexture;
    float CurrentHue, CurrentSat, CurrentVal;

    bool initialized = false;

    Coroutine saveCoroutine;
    readonly WaitForSeconds saveDelay = new(1);

    private void OnEnable()
    {
        if (initialized) return;
        initialized = true;

        CreateHueImage();
        CreateSVImage();
        SetHSV(currentPair.GetColor());
        UpdateOutputImage();
    }

    private void OnDisable()
    {
        if (saveCoroutine != null) 
            StopCoroutine(saveCoroutine);

        skinData.SaveToJson(SkinData.CustomizationSaveDataLocation);
        skinData.SyncSkinData();
    }

    void CreateHueImage()
    {
        hueTexture = new(1, 16);
        hueTexture.wrapMode = TextureWrapMode.Clamp;
        hueTexture.name = "HueTexture";

        for (int i = 0; i < hueTexture.height; i++)
        {
            hueTexture.SetPixel(0, i, Color.HSVToRGB((float)i / hueTexture.height, 1, 1f));
        }

        hueTexture.Apply();
        CurrentHue = 0;

        hueImage.texture = hueTexture;
    }

    void CreateSVImage()
    {
        svTexture = new(16, 16);
        svTexture.wrapMode = TextureWrapMode.Clamp;
        svTexture.name = "SatValTexture";

        for (int y = 0; y < svTexture.height; y++)
        {
            for (int x = 0; x < svTexture.width; x++)
            {
                svTexture.SetPixel(x, y, Color.HSVToRGB(CurrentHue, (float)x / svTexture.width, (float)y / svTexture.height));
            }
        }

        svTexture.Apply();
        CurrentSat = 1;
        CurrentVal = 1;

        satValImage.texture = svTexture;
    }

    public void UpdateOutputImage()
    {
        Color color = Color.HSVToRGB(CurrentHue, CurrentSat, CurrentVal);

        hexInputField.text = ColorUtility.ToHtmlStringRGB(color);
        outputImage.color = color;

        currentPair.SetColor(color);

        RequestSave();
    }

    public void SetSV(float s, float v)
    {
        CurrentSat = s;
        CurrentVal = v;

        UpdateOutputImage();
    }

    public void UpdateSVImage()
    {
        CurrentHue = hueSlider.value;

        for (int y = 0; y < svTexture.height; y++)
        {
            for (int x = 0; x < svTexture.width; x++)
            {
                svTexture.SetPixel(x, y, Color.HSVToRGB(CurrentHue, (float)x / svTexture.width, (float)y / svTexture.height));
            }
        }

        svTexture.Apply();

        UpdateOutputImage();
    }

    void SetHSV(Color color)
    {
        Color.RGBToHSV(color, out CurrentHue, out CurrentSat, out CurrentVal);

        hueSlider.SetValueWithoutNotify(CurrentHue);

        for (int y = 0; y < svTexture.height; y++)
        {
            for (int x = 0; x < svTexture.width; x++)
            {
                svTexture.SetPixel(
                    x,
                    y,
                    Color.HSVToRGB(
                        CurrentHue,
                        (float)x / svTexture.width,
                        (float)y / svTexture.height
                    )
                );
            }
        }

        svTexture.Apply();

        UpdateOutputImage();

        svIC.SetPickerPosition(CurrentSat, CurrentVal);
    }

    public void OnTextInput()
    {
        if (hexInputField.text.Length < 6) return;

        if (ColorUtility.TryParseHtmlString("#" + hexInputField.text, out Color newCol))
            Color.RGBToHSV(newCol, out CurrentHue, out CurrentSat, out CurrentVal);

        hueSlider.value = CurrentHue;

        UpdateOutputImage();
    }

    public void SetCurrentMatColPair(MatColPair pair)
    {
        currentPair = pair;
        SetHSV(pair.GetColor());
    }

    void RequestSave()
    {
        if (saveCoroutine != null) return;

        saveCoroutine = StartCoroutine(SaveAfterDelay());
    }

    IEnumerator SaveAfterDelay()
    {
        yield return saveDelay;

        skinData.SaveToJson(SkinData.CustomizationSaveDataLocation);
        skinData.SyncSkinData();
        saveCoroutine = null;
    }
}
