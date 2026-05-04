using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpectatorMovement : NetworkBehaviour
{
    [SerializeField] PlayerData playerData;

    public PlayerData GetPlayerData() => currentPlayer ? currentPlayer : GameManager.Instance.playMod.LocalPlayer;

    int currentIndex = 0;
    PlayerData currentPlayer;

    private void OnEnable()
    {
        if (!isLocalPlayer) return;

        currentIndex = playerData.Index;
        playerData.PlayerCamera.enabled = false;
        playerData.PlayerAudio.enabled = false;

        ChangeTarget(true);
    }

    private void OnDisable()
    {
        if (!isLocalPlayer) return;

        currentPlayer.PlayerCamera.enabled = false;
        currentPlayer.PlayerAudio.enabled = false;

        currentPlayer = null;

        playerData.PlayerCamera.enabled = true;
        playerData.PlayerAudio.enabled = true;
    }

    public void PrimaryAction(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        ChangeTarget(true);
    }

    public void SecondaryAction(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        ChangeTarget(false);
    }

    private void ChangeTarget(bool increase)
    {
        if (!isLocalPlayer) return;

        if (currentPlayer != null)
        {
            currentPlayer.PlayerCamera.enabled = false;
            currentPlayer.PlayerAudio.enabled = false;
        }

        if (increase)
        {
            currentIndex++;

            if (currentIndex >= GameManager.Instance.playMod.Players.Count)
                currentIndex = 0;
        }
        else
        {
            currentIndex--;
            if (currentIndex < 0)
                currentIndex = GameManager.Instance.playMod.Players.Count - 1;
        }

        currentPlayer = GameManager.Instance.playMod.Players[currentIndex];
        currentPlayer.PlayerCamera.enabled = true;
        currentPlayer.PlayerAudio.enabled = true;
    }
}
