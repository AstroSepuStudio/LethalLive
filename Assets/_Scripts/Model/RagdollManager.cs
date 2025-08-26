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
        Debug.Log("Try to enable ragdoll");
        if (!skinData.pData.isServer) return;
        Debug.Log("Enabling ragdoll");
        IsKnocked = true;
        LocalEnableRagdoll(momentum);
        RpcEnableRagdoll(momentum);
    }

    [ClientRpc]
    void RpcEnableRagdoll(Vector3 momentum)
    {
        LocalEnableRagdoll(momentum);
    }

    void LocalEnableRagdoll(Vector3 momentum)
    {
        Debug.Log("Ragdoll enabled");
        skinData.pData.Skin_Data.CharacterAnimator.enabled = false;
        skinData.pData.Character_Controller.enabled = false;
        skinData.pData.Model.parent = null;

        foreach (var col in ragdollColliders)
        {
            col.enabled = true;
        }

        foreach (var rb in ragdollRigidbodies)
        {
            rb.isKinematic = false;
        }

        hipRigidbody.AddForce(momentum * momentumMultiplier, ForceMode.Impulse);
    }

    [Server]
    public void DisableRagdoll()
    {
        if (!skinData.pData.isServer) return;

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

        skinData.pData.Model.parent = skinData.pData.transform;
        skinData.pData.Model.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        skinData.pData.Skin_Data.SkinRenderer.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
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
