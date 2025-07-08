using Mirror;
using UnityEngine;

public class RagdollManager : NetworkBehaviour
{
    [SerializeField] PlayerData pData;
    [SerializeField] Rigidbody hipRigidbody;
    [SerializeField] Rigidbody[] ragdollRigidbodies;
    [SerializeField] Collider[] ragdollColliders;

    [SyncVar] public bool IsKnocked;
    [SyncVar] private Vector3 syncedPosition;
    [SyncVar] private Quaternion syncedRotation;

    [Server]
    public void EnableRagdoll()
    {
        if (!isServer) return;

        IsKnocked = true;
        LocalEnableRagdoll();
        RpcEnableRagdoll();
    }

    [ClientRpc]
    void RpcEnableRagdoll()
    {
        LocalEnableRagdoll();
    }

    void LocalEnableRagdoll()
    {
        pData.CharacterAnimator.enabled = false;
        pData.Character_Controller.enabled = false;
        pData.Model.parent = null;

        foreach (var col in ragdollColliders)
        {
            col.enabled = true;
        }

        foreach (var rb in ragdollRigidbodies)
        {
            rb.isKinematic = false;
        }
    }

    [Server]
    public void DisableRagdoll()
    {
        if (!isServer) return;

        IsKnocked = false;
        LocalDisableRagdoll();
        RpcDisableRagdoll();
    }

    [ClientRpc]
    void RpcDisableRagdoll()
    {
        LocalDisableRagdoll();
    }

    void LocalDisableRagdoll()
    {
        foreach (var col in ragdollColliders)
        {
            col.enabled = false;
        }

        foreach (var rb in ragdollRigidbodies)
        {
            rb.isKinematic = true;
        }

        pData.Model.parent = pData.transform;
        pData.Model.localPosition = Vector3.zero;
        pData.Model.localRotation = Quaternion.identity;
        pData.CharacterAnimator.enabled = true;
        pData.Character_Controller.enabled = true;
    }

    void FixedUpdate()
    {
        if (!IsKnocked) return;

        if (isServer)
        {
            syncedPosition = hipRigidbody.position;
            syncedRotation = hipRigidbody.rotation;
        }
        else
        {
            hipRigidbody.MovePosition(syncedPosition);
            hipRigidbody.MoveRotation(syncedRotation);
        }

        pData.transform.position = hipRigidbody.position;
    }
}
