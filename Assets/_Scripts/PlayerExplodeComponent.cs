using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerExplodeComponent : NetworkBehaviour
{
    [SerializeField] ExplosionComponent explosionComponent;

    public void ExplodeInput(InputAction.CallbackContext context)
    {
        if (!isLocalPlayer) return;

        explosionComponent.TriggerExplosion();
    }
}
