using Mirror;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class InteractableObject : NetworkBehaviour
{
    [SerializeField] Canvas itemCanvas;
    [SerializeField] Image higlightImg;
    [SerializeField] Sprite highlightedSprite;
    [SerializeField] Sprite lowlightedSprite;

    [SerializeField] UnityEvent<PlayerData> OnInteractEvent;

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

    public virtual void OnInteract(PlayerData sourceData)
    {
        OnInteractEvent?.Invoke(sourceData);
    }
}
