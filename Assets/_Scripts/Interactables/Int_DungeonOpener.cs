using Mirror;
using System.Collections;
using UnityEngine;

public class Int_DungeonOpener : InteractableObject
{
    [SerializeField] AudioSource audioSrc;
    [SerializeField] AudioSFX buttonPressSFX;
    [SerializeField] Transform playerTargetPos;
    [SerializeField] Transform topPosition;
    [SerializeField] Transform bottomPosition;
    [SerializeField] float animationDuration = 0.6f;

    public override bool CanBeInteracted()
    {
        bool interactable = this.interactable && !GameManager.Instance.onDeadTime;
        return interactable;
    }

    public override void OnHoldInteract(PlayerData sourceData)
    {
        base.OnHoldInteract(sourceData);

        sourceData._LockPlayer = true;

        sourceData.Teleport(playerTargetPos.position);
        sourceData.Player_Movement.ServerForceAimAt(playerTargetPos.position + playerTargetPos.forward);

        sourceData.Skin_Data.Rigging_Manager.RpcAnimateRightHandChainRig(topPosition.position, bottomPosition.position, animationDuration);
        StartCoroutine(WaitForAnimation(sourceData));
    }

    IEnumerator WaitForAnimation(PlayerData sourceData)
    {
        yield return new WaitForSeconds(animationDuration);

        sourceData._LockPlayer = false;
        sourceData.Player_Movement.ServerClearForcedAim();
        RpcPlayButtonPressSFX();
    }

    [ClientRpc]
    private void RpcPlayButtonPressSFX()
    {
        AudioManager.Instance.PlayOneShot(audioSrc, buttonPressSFX);
    }
}
