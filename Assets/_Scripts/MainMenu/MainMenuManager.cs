using Steamworks;
using UnityEngine;

public class MainMenuManager : UIManager
{
    [SerializeField] GameObject cameraParent;

    protected override void Start()
    {
        base.Start();

        LobbyManager.Instance.OnLobbyCreatedEvent.AddListener(HideMainMenu);
        LobbyManager.Instance.OnLobbyJoinedEvent.AddListener(HideMainMenu);

        LobbyManager.Instance.OnLobbyLeaveEvent.AddListener(DisplayMainMenu);
        LobbyManager.Instance.OnLobbyKickedEvent.AddListener(DisplayMainMenu);
    }

    private void HideMainMenu(LobbyEnter_t arg0)
    {
        gameObject.SetActive(false);
        cameraParent.SetActive(false);
    }

    private void HideMainMenu(LobbyCreated_t arg0)
    {
        gameObject.SetActive(false);
        cameraParent.SetActive(false);
    }

    private void DisplayMainMenu()
    {
        gameObject.SetActive(true);
        cameraParent.SetActive(true);
    }

    private void DisplayMainMenu(LobbyKicked_t arg0)
    {
        gameObject.SetActive(true);
        cameraParent.SetActive(true);
    }

    public void QuitApplication()
    {
        Application.Quit();
    }
}
