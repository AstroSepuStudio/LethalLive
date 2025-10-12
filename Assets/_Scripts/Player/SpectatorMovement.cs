using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpectatorMovement : NetworkBehaviour
{
    [SerializeField] PlayerData playerData;

    [SyncVar] int currentIndex = 0;

    private void OnEnable()
    {
        if (!isLocalPlayer) return;

        if (isServer)
            currentIndex = playerData.Index;

        playerData.PlayerCamera.enabled = false;
        CmdRequestNewTarget(true);
    }

    private void OnDisable()
    {
        if (!isLocalPlayer) return;

        playerData.PlayerCamera.enabled = true;
    }

    [Command(requiresAuthority = false)]
    void CmdRequestNewTarget(bool increase)
    {
        uint oldID = GameManager.Instance.Players[currentIndex].netId;

        if (increase)
        {
            currentIndex++;
            if (currentIndex >= GameManager.Instance.Players.Count) 
                currentIndex = 0;
        }
        else
        {
            currentIndex--;
            if (currentIndex < 0) 
                currentIndex = GameManager.Instance.Players.Count - 1;
        }

        RpcChangeSpectatorTarget(oldID, GameManager.Instance.Players[currentIndex].netId);
    }

    public void PrimaryAction(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        CmdRequestNewTarget(true);
    }

    public void SecondaryAction(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        CmdRequestNewTarget(false);
    }

    [ClientRpc]
    public void RpcChangeSpectatorTarget(uint oldID, uint newID)
    {
        if (!isLocalPlayer) return;

        NetworkClient.spawned.TryGetValue(oldID, out NetworkIdentity identity);
        if (identity == null) return;

        PlayerData data = identity.GetComponent<PlayerData>();
        data.PlayerCamera.enabled = false;

        NetworkClient.spawned.TryGetValue(newID, out identity);
        if (identity == null) return;

        data = identity.GetComponent<PlayerData>();
        data.PlayerCamera.enabled = true;
    }
}
