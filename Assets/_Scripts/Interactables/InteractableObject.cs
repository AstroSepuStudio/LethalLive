using Mirror;
using Mirror.Examples.MultipleMatch;
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
        TargetUpdateFill(sourceData.connectionToClient, 0f);

        float timer = 0f;
        float updateTimer = 0f;
        while (timer < holdTime && _holding && OnHoldInteractEvent.GetPersistentEventCount() > 0)
        {
            timer += Time.deltaTime;
            updateTimer += Time.deltaTime;

            if (updateTimer >= 0.05f)
            {
                float fillValue = timer / holdTime;
                TargetUpdateFill(sourceData.connectionToClient, fillValue);
                updateTimer = 0f;
            }
            yield return null;
        }

        _holding = false;
        TargetUpdateFill(sourceData.connectionToClient, 0f);

        if (timer >= holdTime)
            OnHoldInteractEvent?.Invoke(sourceData);
        else
            OnInteractEvent?.Invoke(sourceData);
    }

    public virtual void OnStopInteract(PlayerData sourceData)
    {
        _holding = false;
        TargetUpdateFill(sourceData.connectionToClient, 0f);
        OnStopInteractEvent?.Invoke(sourceData);
    }

    [TargetRpc]
    private void TargetUpdateFill(NetworkConnection target, float value)
    {
        if (canvas != null && canvas.holdImgDisplayer != null)
            canvas.holdImgDisplayer.fillAmount = value;
    }
}
