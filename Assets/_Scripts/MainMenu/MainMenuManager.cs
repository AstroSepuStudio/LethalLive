using Steamworks;

public class MainMenuManager : UIManager
{
    protected override void Start()
    {
        base.Start();

        LobbyManager.Instace.OnLobbyCreatedEvent.AddListener(HideMainMenu);
        LobbyManager.Instace.OnLobbyJoinedEvent.AddListener(HideMainMenu);
    }

    private void HideMainMenu(LobbyEnter_t arg0)
    {
        gameObject.SetActive(false);
    }

    private void HideMainMenu(LobbyCreated_t arg0)
    {
        gameObject.SetActive(false);
    }
}
