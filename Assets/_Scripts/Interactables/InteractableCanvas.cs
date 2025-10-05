using UnityEngine;
using UnityEngine.UI;

public class InteractableCanvas : MonoBehaviour
{
    public Canvas itemCanvas;
    public Image higlightImg;
    public Image holdImgDisplayer;
    public Sprite highlightedSprite;
    public Sprite lowlightedSprite;

    protected virtual void Start()
    {
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
