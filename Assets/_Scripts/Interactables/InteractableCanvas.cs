using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InteractableCanvas : MonoBehaviour
{
    [SerializeField] bool hold = false;

    public Canvas itemCanvas;
    public Image higlightImg;
    public Image holdImgDisplayer;

    [SerializeField] GameObject holdTxtGo;
    [SerializeField] GameObject interactGo;
    [SerializeField] TextMeshProUGUI labelTxt;
    [SerializeField] TextMeshProUGUI descriptionTxt;

    [SerializeField] Image bgImg;
    [SerializeField] Color selectedClr = Color.darkOrange;
    [SerializeField] Color deselectedClr = Color.darkGreen;

    string defaultLabel;
    string defaultDescription;

    protected virtual void Start()
    {
        if (holdTxtGo != null) holdTxtGo.SetActive(hold);
        if (itemCanvas != null) itemCanvas.gameObject.SetActive(false);
        if (interactGo != null) interactGo.SetActive(false);

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
        if (higlightImg != null) higlightImg.color = selectedClr;
        if (interactGo != null) interactGo.SetActive(true);
    }

    public virtual void DeselectClosest()
    {
        if (higlightImg != null) higlightImg.color = deselectedClr;
        if (interactGo != null) interactGo.SetActive(false);
    }
}
