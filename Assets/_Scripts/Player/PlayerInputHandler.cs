using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : NetworkBehaviour
{
    [SerializeField] PlayerData pData;

    public void RequestReturnMainMenu() => GameManager.Instance.playMod.RequestReturnToMainMenu();

    public void OnPlayerPressEscape(InputAction.CallbackContext context)
    {
        if (!context.started || !isLocalPlayer) return;

        RequestSwitchTabletState();
    }

    public void OnForceMouseFreeState(InputAction.CallbackContext context)
    {
        if (context.started)
            SettingsManager.Instance.SetMouseLockState(false);

        if (context.canceled)
            SettingsManager.Instance.SetMouseLockState(true);
    }

    public void RequestSwitchTabletState()
    {
        if (pData.TabletManager.TrySwitchState())
            Cmd_SwitchTabletState(pData.TabletManager.IsActive);
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
