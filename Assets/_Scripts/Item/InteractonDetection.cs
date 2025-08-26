using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractonDetection : NetworkBehaviour
{
    [SerializeField] PlayerData pData;

    [SerializeField] float detectRadius = 2f;
    [SerializeField] float raycastDistance = 3f;
    [SerializeField] LayerMask itemLayer;
    [SerializeField] LayerMask interactLayer;

    private InteractableObject nearestItem;
    List<InteractableObject> nearbyItems = new();

    private void Start()
    {
        GameTick.OnTick += OnTick;
    }

    private void OnDestroy()
    {
        GameTick.OnTick -= OnTick;
    }

    void OnTick()
    {
        if (!isLocalPlayer) return;

        Transform cam = pData.PlayerCamera.transform;
        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit rayHit, raycastDistance, pData.IgnorePlayer))
        {
            if (rayHit.transform.TryGetComponent<InteractableObject>(out var item))
            {
                for (int i = nearbyItems.Count - 1; i >= 0; i--)
                {
                    nearbyItems[i].DisableCanvas();
                    nearbyItems.RemoveAt(i);
                }

                item.EnableCanvas();
                item.SelectClosest();
                return;
            }
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, detectRadius, interactLayer);
        HashSet<InteractableObject> detectedItems = new();

        InteractableObject closestItem = null;
        float closestDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<InteractableObject>(out var item))
                continue;

            detectedItems.Add(item);

            if (!nearbyItems.Contains(item))
            {
                item.EnableCanvas();
                nearbyItems.Add(item);
            }

            float dist = Vector3.Distance(transform.position, item.transform.position);
            if (dist < closestDistance)
            {
                closestItem = item;
                closestDistance = dist;
            }
        }

        for (int i = nearbyItems.Count - 1; i >= 0; i--)
        {
            InteractableObject item = nearbyItems[i];
            if (!detectedItems.Contains(item))
            {
                item.DisableCanvas();
                nearbyItems.RemoveAt(i);
            }
        }

        foreach (var item in nearbyItems)
        {
            if (item == closestItem)
                item.SelectClosest();
            else
                item.DeselectClosest();
        }
    }

    public void OnPlayerInteract(InputAction.CallbackContext context)
    {
        if (!context.started || !isLocalPlayer || pData._LockPlayer) return;

        CmdRequestInteracttion();
    }

    [Command]
    void CmdRequestInteracttion()
    {
        Transform cam = pData.PlayerCamera.transform;
        if (Physics.Linecast(cam.position, cam.forward * raycastDistance + cam.position, out RaycastHit rayHit, pData.IgnorePlayer))
        {
            if (rayHit.transform.TryGetComponent<InteractableObject>(out var item))
            {
                item.OnInteract(pData);
                return;
            }
        }

        Collider[] hits = Physics.OverlapSphere(pData.transform.position, detectRadius, interactLayer);
        InteractableObject closest = null;
        float closestDist = float.MaxValue;
        foreach (var hit in hits)
        {
            if (!hit.TryGetComponent<InteractableObject>(out var item)) continue;
            float dist = Vector3.Distance(pData.transform.position, item.transform.position);
            if (dist < closestDist) 
            { 
                closest = item; 
                closestDist = dist; 
            }
        }
        if (closest != null)
        {
            closest.OnInteract(pData);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectRadius);

        Transform cam = pData.PlayerCamera.transform;
        Gizmos.DrawLine(cam.position, cam.forward * raycastDistance + cam.position);
    }
}
