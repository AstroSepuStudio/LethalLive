using Mirror;
using SimpleVoiceChat;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : NetworkBehaviour
{
    [SerializeField] PlayerData pData;

    public void OnPlayerPressEscape(InputAction.CallbackContext context)
    {
        if (!context.started || !isLocalPlayer) return;

        bool newState = !pData.TabletGMO.activeInHierarchy;
        pData.TabletGMO.SetActive(newState);
        Cmd_SwitchTabletState(newState);
    }

    [Command(requiresAuthority = false)]
    private void Cmd_SwitchTabletState(bool open)
    {
        pData.SetLockPlayer(open);
        Rpc_SwitchTabletState(open);
    }

    [ClientRpc]
    private void Rpc_SwitchTabletState(bool open)
    {
        pData.Skin_Data.CharacterAnimator.SetBool("Tablet", open);
    }
}
