using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpectatorMovement : NetworkBehaviour
{
    [SerializeField] PlayerData playerData;

    public PlayerData GetPlayerData() => currentPlayer? currentPlayer : GameManager.Instance.playMod.LocalPlayer;

    int currentIndex = 0;
    uint currentID;
    PlayerData currentPlayer;

    private void OnEnable()
    {
        if (!isLocalPlayer) return;

        currentIndex = playerData.Index;

        playerData.PlayerCamera.enabled = false;
        CmdRequestNewTarget(true);
    }

    private void OnDisable()
    {
        if (!isLocalPlayer) return;

        NetworkClient.spawned.TryGetValue(currentID, out NetworkIdentity identity);
        if (identity == null) return;

        PlayerData data = identity.GetComponent<PlayerData>();
        data.PlayerCamera.enabled = false;

        playerData.PlayerCamera.enabled = true;
        currentPlayer = null;
    }

    [Command(requiresAuthority = false)]
    void CmdRequestNewTarget(bool increase)
    {
        if (playerData._LockPlayer) return;

        uint oldID = GameManager.Instance.playMod.Players[currentIndex].netId;

        if (increase)
        {
            currentIndex++;

            if (currentIndex >= GameManager.Instance.playMod.Players.Count) 
                currentIndex = 0;
        }
        else
        {
            currentIndex--;
            if (currentIndex < 0) 
                currentIndex = GameManager.Instance.playMod.Players.Count - 1;
        }

        RpcChangeSpectatorTarget(oldID, GameManager.Instance.playMod.Players[currentIndex].netId);
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

    [TargetRpc]
    public void RpcChangeSpectatorTarget(uint oldID, uint newID)
    {
        if (!isLocalPlayer) return;

        NetworkClient.spawned.TryGetValue(oldID, out NetworkIdentity identity);
        if (identity == null) return;

        PlayerData data = identity.GetComponent<PlayerData>();
        data.PlayerCamera.enabled = false;

        NetworkClient.spawned.TryGetValue(newID, out identity);
        if (identity == null) return;

        currentID = newID;
        data = identity.GetComponent<PlayerData>();
        data.PlayerCamera.enabled = true;

        currentPlayer = data;
    }
}
