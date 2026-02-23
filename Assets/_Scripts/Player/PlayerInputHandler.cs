using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using LethalLive;

public class PlayerInputHandler : NetworkBehaviour
{
    [SerializeField] PlayerData pData;
    bool mWasFree;

    public void RequestReturnMainMenu() => GameManager.Instance.playMod.RequestReturnToMainMenu();

    public void OnPlayerPressEscape(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        if (!isLocalPlayer || GameManager.Instance.lobbyManagerScreen.playerOnLMS == pData.Index) return;

        if (pData.TabletManager.TrySwitchState())
        {
            Cmd_SwitchTabletState(pData.TabletManager.IsActive);
        }
    }

    public void OnForceMouseFreeState(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (Cursor.lockState == CursorLockMode.None)
            {
                mWasFree = true;
                return;
            }
            SettingsManager.Instance.SetMouseLockState(false);
        }

        if (context.canceled)
        {
            if (mWasFree) return;
            SettingsManager.Instance.SetMouseLockState(true);
        }
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
