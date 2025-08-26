using Mirror;
using UnityEngine;

public class GravityController : NetworkBehaviour
{
    [SerializeField] CharacterController controller;
    [SerializeField] Vector3 gravity;

    void Update()
    {
        if (!isServer) return;
        if (!controller.enabled) return;

        controller.Move(gravity * Time.deltaTime);
    }
}
