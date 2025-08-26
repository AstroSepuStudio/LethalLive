using UnityEngine;
using Mirror;
using Steamworks;

public class SteamLobby : MonoBehaviour
{
    [SerializeField] NetworkManager networkManager;
    [SerializeField] GameObject hostBtn;

    protected Callback<LobbyCreated_t> lobbyCreatedCallback;
    protected Callback<GameLobbyJoinRequested_t> lobbyJoinRequestCallback;
    protected Callback<LobbyEnter_t> lobbyEnterCallback;

    private const string HostAdressKey = "HostAdress";

    private void Start()
    {
        if (!SteamManager.Initialized) return;

        lobbyCreatedCallback = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        lobbyJoinRequestCallback = Callback<GameLobbyJoinRequested_t>.Create(OnLobbyJoinRequested);
        lobbyEnterCallback = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
    }

    public void HostLobby()
    {
        hostBtn.SetActive(false);

        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, networkManager.maxConnections);
    }

    void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            hostBtn.SetActive(true);
            return;
        }

        Debug.Log("Lobby Created: " + callback.m_ulSteamIDLobby);
        networkManager.StartHost();
        SteamMatchmaking.SetLobbyData(new (callback.m_ulSteamIDLobby), HostAdressKey, SteamUser.GetSteamID().ToString());
    }

    void OnLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        Debug.Log("Lobby Join Requested: " + callback.m_steamIDLobby);
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    void OnLobbyEntered(LobbyEnter_t callback)
    {
        if (NetworkServer.active) return;

        Debug.Log("Lobby Entered: " + callback.m_ulSteamIDLobby);

        string hostAdress = SteamMatchmaking.GetLobbyData(new (callback.m_ulSteamIDLobby), HostAdressKey);

        networkManager.networkAddress = hostAdress;
        networkManager.StartClient();

        hostBtn.SetActive(false);
    }
}
