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
    [SerializeField] Image bgImg;
    [SerializeField] Color tapColor = Color.darkCyan;
    [SerializeField] Color holdColor = Color.orangeRed;

    protected virtual void Start()
    {
        holdTxtGo.SetActive(hold);
        bgImg.color = hold ? holdColor : tapColor;
        itemCanvas.gameObject.SetActive(false);
    }

    public virtual void EnableCanvas()
    {
        itemCanvas.gameObject.SetActive(true);
    }

    public virtual void DisableCanvas()
    {
        itemCanvas.gameObject.SetActive(false);
    }

    public virtual void SelectClosest()
    {
        higlightImg.sprite = highlightedSprite;
    }

    public virtual void DeselectClosest()
    {
        higlightImg.sprite = lowlightedSprite;
    }
}
