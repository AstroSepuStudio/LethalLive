using UnityEngine;

public interface IPlayerController
{
    void OnGainControl(PlayerData player);
    void OnLoseControl(PlayerData player);

    void OnMove(Vector2 input);
    void OnJump();
    void OnJumpCanceled();
    void OnSprintStart();
    void OnSprintStop();
    void OnCrouchStart();
    void OnCrouchStop();
}
