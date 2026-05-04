using Mirror;
using UnityEngine;

public class RagdollManager : NetworkBehaviour
{
    [SerializeField] SkinData skinData;
    [SerializeField] Rigidbody hipRigidbody;
    [SerializeField] Rigidbody[] ragdollRigidbodies;
    [SerializeField] Collider[] ragdollColliders;
    [SerializeField] float momentumMultiplier = 20f;

    [SyncVar] public bool IsKnocked;
    [SyncVar] private Vector3 syncedPosition;
    [SyncVar] private Quaternion syncedRotation;

    [Server]
    public void EnableRagdoll(Vector3 momentum)
    {
        if (!skinData.pData.isServer) return;
        IsKnocked = true;

        hipRigidbody.isKinematic = false;
        hipRigidbody.AddForce(momentum * momentumMultiplier, ForceMode.Impulse);

        RpcEnableRagdoll();
    }

    [ClientRpc]
    void RpcEnableRagdoll()
    {
        LocalEnableRagdoll();
    }

    void LocalEnableRagdoll()
    {
        skinData.pData.Skin_Data.CharacterAnimator.enabled = false;
        skinData.pData.Character_Controller.enabled = false;
        skinData.pData.Model.parent = null;

        foreach (var col in ragdollColliders)
        {
            col.enabled = true;
        }

        foreach (var rb in ragdollRigidbodies)
        {
            if (rb == hipRigidbody) continue;
            rb.isKinematic = false;
        }
    }

    [Server]
    public void DisableRagdoll()
    {
        if (!skinData.pData.isServer) return;

        IsKnocked = false;
        hipRigidbody.isKinematic = true;

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

        skinData.pData.Model.parent = skinData.pData.transform;
        skinData.pData.Model.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        //skinData.pData.Skin_Data.SkinRenderer.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        skinData.pData.Skin_Data.CharacterAnimator.enabled = true;
        skinData.pData.Character_Controller.enabled = true;
    }

    void FixedUpdate()
    {
        if (!IsKnocked || skinData.pData == null) return;

        if (skinData.pData.isServer)
        {
            syncedPosition = hipRigidbody.position;
            syncedRotation = hipRigidbody.rotation;
        }
        else
        {
            hipRigidbody.MovePosition(syncedPosition);
            hipRigidbody.MoveRotation(syncedRotation);
        }

        skinData.pData.transform.position = hipRigidbody.position;
    }
}
