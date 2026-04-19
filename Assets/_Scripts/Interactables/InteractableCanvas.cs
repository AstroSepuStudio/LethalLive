using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractableCanvas : MonoBehaviour
{
    [SerializeField] bool hold = false;

    public Canvas itemCanvas;
    public Image higlightImg;
    public Image holdImgDisplayer;
    public Sprite highlightedSprite;
    public Sprite lowlightedSprite;

    [SerializeField] GameObject holdTxtGo;
    [SerializeField] TextMeshProUGUI labelTxt;
    [SerializeField] TextMeshProUGUI descriptionTxt;

    [SerializeField] Image bgImg;
    [SerializeField] Color tapColor = Color.darkCyan;
    [SerializeField] Color holdColor = Color.orangeRed;

    string defaultLabel;
    string defaultDescription;

    protected virtual void Start()
    {
        if (holdTxtGo != null) holdTxtGo.SetActive(hold);
        if (bgImg != null) bgImg.color = hold ? holdColor : tapColor;
        if (itemCanvas != null) itemCanvas.gameObject.SetActive(false);

        defaultLabel = labelTxt.text;
        defaultDescription = descriptionTxt.text;
    }

    public void SetLabel(string label) => labelTxt.text = label;
    public void SetDescription(string description) => descriptionTxt.text = description;
    public void ResetLabel() => labelTxt.text = defaultLabel;
    public void ResetDescription() => descriptionTxt.text = defaultDescription;

    public virtual void EnableCanvas()
    {
        if (itemCanvas != null) itemCanvas.gameObject.SetActive(true);
    }

    public virtual void DisableCanvas()
    {
        if (itemCanvas != null) itemCanvas.gameObject.SetActive(false);
    }

    public virtual void SelectClosest()
    {
        if (higlightImg != null) higlightImg.sprite = highlightedSprite;
    }

    public virtual void DeselectClosest()
    {
        if (higlightImg != null) higlightImg.sprite = lowlightedSprite;
    }
}
