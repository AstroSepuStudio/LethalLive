using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerExplodeComponent : NetworkBehaviour
{
    [SerializeField] ExplosionComponent explosionComponent;

    [Server]
    public void TriggerExplosion(bool force)
    {
        explosionComponent.TriggerExplosion(force);
    }

    public void ExplodeInput(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        if (!isLocalPlayer && !GameManager.Instance.playMod.LocalPlayer._LockPlayer) return;

        explosionComponent.CmdTriggerExplosion();
    }
}
