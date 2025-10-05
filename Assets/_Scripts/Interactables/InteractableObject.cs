using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class InteractableObject : NetworkBehaviour
{
    [SerializeField] protected float holdTime = 2f;
    [SerializeField] protected InteractableCanvas canvas;

    [SerializeField] protected UnityEvent<PlayerData> OnInteractEvent;
    [SerializeField] protected UnityEvent<PlayerData> OnStopInteractEvent;
    [SerializeField] protected UnityEvent<PlayerData> OnHoldInteractEvent;

    protected bool _holding;
    protected float startHoldTime;

    public virtual void SelectClosest() => canvas.SelectClosest();
    public virtual void DeselectClosest() => canvas.DeselectClosest();
    public virtual void DisableCanvas() => canvas.DisableCanvas();
    public virtual void EnableCanvas() => canvas.EnableCanvas();

    private void Start()
    {
        if (canvas == null)
            canvas = GetComponentInChildren<InteractableCanvas>();
    }

    public virtual void OnInteract(PlayerData sourceData)
    {
        StartCoroutine(CheckForHold(sourceData));
    }

    IEnumerator CheckForHold(PlayerData sourceData)
    {
        _holding = true;
        canvas.holdImgDisplayer.fillAmount = 0;

        float timer = 0f;
        while (timer < holdTime && _holding && OnHoldInteractEvent.GetPersistentEventCount() > 0)
        {
            canvas.holdImgDisplayer.fillAmount = timer / holdTime;
            timer += Time.deltaTime;
            yield return null;
        }

        _holding = false;
        canvas.holdImgDisplayer.fillAmount = 0;

        if (timer >= holdTime)
            OnHoldInteractEvent?.Invoke(sourceData);
        else
            OnInteractEvent?.Invoke(sourceData);
    }

    public virtual void OnStopInteract(PlayerData sourceData)
    {
        _holding = false;
        canvas.holdImgDisplayer.fillAmount = 0;
        OnStopInteractEvent?.Invoke(sourceData);
    }
}
