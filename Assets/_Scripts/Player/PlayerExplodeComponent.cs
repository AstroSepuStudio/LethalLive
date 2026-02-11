using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerExplodeComponent : NetworkBehaviour
{
    [SerializeField] ExplosionComponent explosionComponent;

    public void ExplodeInput(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer && !GameManager.Instance.playMod.LocalPlayer._LockPlayer) return;

        explosionComponent.TriggerExplosion();
    }
}
