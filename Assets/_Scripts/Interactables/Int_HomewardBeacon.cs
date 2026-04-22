using Mirror;
using UnityEngine;

public class Int_HomewardBeacon : InteractableObject, IMapFollowTarget
{
    public Transform FollowTransform => transform;

    public bool IsAvailable => true;

    public override void OnInteract(PlayerData sourceData)
    {
        base.OnInteract(sourceData);

        sourceData.RpcStartTeleport();
    }

    public override void OnStopInteract(PlayerData sourceData)
    {
        base.OnStopInteract(sourceData);

        sourceData.RpcCancelTeleport();
    }

    [Server]
    public void GoBackToOffice(PlayerData sourceData)
    {
        sourceData.RpcCompleteTeleport();
        sourceData.Character_Controller.enabled = false;
        sourceData.Character_Controller.transform.position = GameManager.Instance.Teleporter.transform.position;
        sourceData.Character_Controller.enabled = true;
        sourceData._PlayerInOffice = true;

        DisableCanvas();
        GameManager.Instance.dngMod.OnReturnOffice(sourceData);
    }
}
