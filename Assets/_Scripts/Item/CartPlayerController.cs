using UnityEngine;

public class CartPlayerController : IPlayerController
{
    readonly Item_Cart cart;
    Vector2 currentInput;

    public Item_Cart Cart => cart;
    public Vector2 CurrentInput => currentInput;

    public CartPlayerController(Item_Cart cart)
    {
        this.cart = cart;
    }

    public void OnGainControl(PlayerData player)
    {
        player.Player_Movement.CmdStopSprint();
    }

    public void OnLoseControl(PlayerData player)
    {
        currentInput = Vector2.zero;
        cart.OnPlayerReleaseControl(player);
    }

    public void OnMove(Vector2 input) => currentInput = input;

    public void OnJump()
    {
        cart.OnDriverJump();
    }

    public void OnJumpCanceled() { }
    public void OnSprintStart() { }
    public void OnSprintStop() { }

    public void OnCrouchStart()
    {
        cart.ReleaseDriver();
    }

    public void OnCrouchStop() { }
}
