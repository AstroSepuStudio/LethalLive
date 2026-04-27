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

    [SyncVar] public bool interactable = true;
    protected bool _holding;
    protected float startHoldTime;

    public virtual void SelectClosest() => canvas.SelectClosest();
    public virtual void DeselectClosest() => canvas.DeselectClosest();
    public virtual void EnableCanvas() => canvas.EnableCanvas();
    public virtual void DisableCanvas() => canvas.DisableCanvas();
    [Server] public virtual void EnableInteractable() => interactable = true;
    [Server] public virtual void DisableInteractable() => interactable = false;

    public void SetLabel(string label) => canvas.SetLabel(label);
    public void SetDescription(string description) => canvas.SetLabel(description);
    public void ResetLabel() => canvas.ResetLabel();
    public void ResetDescription() => canvas.ResetDescription();

    public virtual bool CanBeInteracted()
    {
        return interactable;
    }

    private void Start()
    {
        if (canvas == null)
            canvas = GetComponentInChildren<InteractableCanvas>();
    }

    public virtual void OnInteract(PlayerData sourceData)
    {
        StartCoroutine(CheckForHold(sourceData));
    }

    public virtual void OnStopInteract(PlayerData sourceData)
    {
        _holding = false;
        TargetUpdateFill(sourceData.connectionToClient, 0f);
        OnStopInteractEvent?.Invoke(sourceData);
    }

    public virtual void OnHoldInteract(PlayerData sourceData)
    {
        OnHoldInteractEvent?.Invoke(sourceData);
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
            OnHoldInteract(sourceData);
        else
            OnInteractEvent?.Invoke(sourceData);
    }

    [TargetRpc]
    private void TargetUpdateFill(NetworkConnection target, float value)
    {
        if (canvas != null && canvas.holdImgDisplayer != null)
            canvas.holdImgDisplayer.fillAmount = value;
    }
}
