using Mirror;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class InteractableObject : NetworkBehaviour
{
    [SerializeField] Canvas itemCanvas;
    [SerializeField] Image higlightImg;
    [SerializeField] protected Sprite highlightedSprite;
    [SerializeField] protected Sprite lowlightedSprite;

    [SerializeField] protected UnityEvent<PlayerData> OnInteractEvent;
    [SerializeField] protected UnityEvent<PlayerData> OnStopInteractEvent;

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

    public virtual void OnStopInteract(PlayerData sourceData)
    {
        OnStopInteractEvent?.Invoke(sourceData);
    }
}
