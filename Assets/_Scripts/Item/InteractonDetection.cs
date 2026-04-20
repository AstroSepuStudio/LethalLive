using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractonDetection : NetworkBehaviour
{
    [SerializeField] PlayerData pData;

    [SerializeField] float detectRadius = 2f;
    [SerializeField] float raycastDistance = 3f;
    [SerializeField] float aimAngleThreshold = 10f;
    [SerializeField] LayerMask itemLayer;
    [SerializeField] LayerMask interactLayer;

    List<InteractableObject> nearbyItems = new();
    InteractableObject currentInteractable;

    private void Start()
    {
        if (!isLocalPlayer) return;
        GameTick.OnTick += OnTick;
    }

    private void OnDestroy()
    {
        if (!isLocalPlayer) return;
        GameTick.OnTick -= OnTick;
    }

    void OnTick()
    {
        if (!isLocalPlayer) return;

        if (currentInteractable != null)
        {
            foreach (var item in nearbyItems)
            {
                if (item == currentInteractable) item.SelectClosest();
                else item.DeselectClosest();
            }
            return;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, detectRadius, interactLayer);
        HashSet<InteractableObject> detectedItems = new();

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<InteractableObject>(out var item)) continue;
            detectedItems.Add(item);

            if (!nearbyItems.Contains(item))
            {
                item.EnableCanvas();
                nearbyItems.Add(item);
            }
        }

        for (int i = nearbyItems.Count - 1; i >= 0; i--)
        {
            var item = nearbyItems[i];
            if (item == null || !detectedItems.Contains(item))
            {
                item?.DisableCanvas();
                nearbyItems.RemoveAt(i);
            }
        }

        InteractableObject best = GetBestItem(nearbyItems, pData.PlayerCamera.transform);

        foreach (var item in nearbyItems)
        {
            if (item == null) continue;
            if (item == best) item.SelectClosest();
            else item.DeselectClosest();
        }
    }

    InteractableObject GetBestItem(List<InteractableObject> candidates, Transform cam)
    {
        InteractableObject bestAngle = null;
        InteractableObject bestDist = null;
        float lowestAngle = float.MaxValue;
        float lowestDist = float.MaxValue;

        foreach (var item in candidates)
        {
            if (item == null) continue;

            Vector3 toItem = (item.transform.position - cam.position).normalized;
            float angle = Vector3.Angle(cam.forward, toItem);
            float dist = Vector3.Distance(transform.position, item.transform.position);

            if (angle < aimAngleThreshold && angle < lowestAngle)
            {
                lowestAngle = angle;
                bestAngle = item;
            }

            if (dist < lowestDist)
            {
                lowestDist = dist;
                bestDist = item;
            }
        }

        return bestAngle != null ? bestAngle : bestDist;
    }

    public void OnPlayerInteract(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return;

        if (context.started && !pData._LockPlayer)
            CmdRequestInteraction();
        else if (context.canceled)
            CmdRequestStopInteraction();
    }

    [Command]
    void CmdRequestInteraction()
    {
        if (currentInteractable != null) return;
        if (pData.Player_Stats.dead || pData.Player_Stats.knocked) return;

        Collider[] hits = Physics.OverlapSphere(pData.transform.position, detectRadius, interactLayer);
        List<InteractableObject> candidates = new();

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<InteractableObject>(out var item))
                candidates.Add(item);
        }

        if (candidates.Count == 0) return;

        InteractableObject best = GetBestItem(candidates, pData.PlayerCamera.transform);
        if (best == null) return;

        best.OnInteract(pData);
        currentInteractable = best;
        TargetSetCurrentInteractable(connectionToClient, best.netIdentity);
    }

    [TargetRpc]
    void TargetSetCurrentInteractable(NetworkConnection targetConn, NetworkIdentity obj)
    {
        if (obj != null && obj.TryGetComponent(out InteractableObject interactable))
            currentInteractable = interactable;
    }

    [Command]
    void CmdRequestStopInteraction()
    {
        if (pData.Player_Stats.dead || pData.Player_Stats.knocked) return;
        if (currentInteractable == null) return;

        currentInteractable.OnStopInteract(pData);
        currentInteractable = null;
        TargetClearCurrentInteractable(connectionToClient);
    }

    [TargetRpc]
    void TargetClearCurrentInteractable(NetworkConnection targetConn)
    {
        currentInteractable = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectRadius);

        if (pData?.PlayerCamera == null) return;
        Transform cam = pData.PlayerCamera.transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(cam.position, cam.forward * raycastDistance);

        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Vector3 left = Quaternion.Euler(0, -aimAngleThreshold, 0) * cam.forward;
        Vector3 right = Quaternion.Euler(0, aimAngleThreshold, 0) * cam.forward;
        Gizmos.DrawRay(cam.position, left * raycastDistance);
        Gizmos.DrawRay(cam.position, right * raycastDistance);
    }
}
