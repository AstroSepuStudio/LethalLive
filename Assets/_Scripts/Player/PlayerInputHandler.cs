using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : NetworkBehaviour
{
    // Class to handle conflicting inputs

    [SerializeField] PlayerData pData;

    public void OnPlayerInteract(InputAction.CallbackContext context)
    {
        if (!context.started || !isLocalPlayer) return;
    }

    public void OnPlayerAttack(InputAction.CallbackContext context)
    {
        if (!context.started || !isLocalPlayer) return;
    }
}
