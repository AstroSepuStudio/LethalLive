using UnityEngine;

public class DefaultPlayerController : IPlayerController
{
    readonly PlayerMovement movement;

    public DefaultPlayerController(PlayerMovement movement)
    {
        this.movement = movement;
    }

    public void OnGainControl(PlayerData player) { }
    public void OnLoseControl(PlayerData player) { }

    public void OnMove(Vector2 input) => movement.CmdSendMovementInput(input);
    public void OnJump() => movement.CmdSendJumpInput();
    public void OnJumpCanceled() { }
    public void OnSprintStart() => movement.CmdStartSprint();
    public void OnSprintStop() => movement.CmdStopSprint();
    public void OnCrouchStart() => movement.CmdStartCrouchAction();
    public void OnCrouchStop() => movement.CmdStopCrouch();
}
