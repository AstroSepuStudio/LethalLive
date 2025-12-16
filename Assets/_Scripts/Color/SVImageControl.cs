using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SVImageControl : MonoBehaviour, IDragHandler, IPointerClickHandler
{
    [SerializeField] ColorPickerControl colorPickerControl;
    [SerializeField] Image pickerImage;

    [SerializeField] RectTransform rectTransform;
    [SerializeField] RectTransform pickerTransform;

    [SerializeField] RawImage svImage;

    void UpdateColour(PointerEventData eventData)
    {
        Vector3 pos = rectTransform.InverseTransformPoint(eventData.position);

        float deltaX = rectTransform.sizeDelta.x * 0.5f;
        float deltaY = rectTransform.sizeDelta.y * 0.5f;

        pos.x = Mathf.Clamp(pos.x, -deltaX, deltaX);
        pos.y = Mathf.Clamp(pos.y, -deltaY, deltaY);

        float x = pos.x + deltaX;
        float y = pos.y + deltaY;

        float xNorm = x / rectTransform.sizeDelta.x;
        float yNorm = y / rectTransform.sizeDelta.y;

        pickerTransform.localPosition = pos;
        pickerImage.color = Color.HSVToRGB(0, 0, 1 - yNorm);

        colorPickerControl.SetSV(xNorm, yNorm);
    }

    public void OnDrag(PointerEventData eventData) => UpdateColour(eventData);
    public void OnPointerClick(PointerEventData eventData) => UpdateColour(eventData);
}
