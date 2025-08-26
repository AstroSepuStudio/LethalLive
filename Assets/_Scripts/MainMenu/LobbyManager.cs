using Mirror;
using Steamworks;
using UnityEngine;
using UnityEngine.Events;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instace;

    public CSteamID CurrentLobbyID { get; private set; }
    public LobbySettings LobbySettings => lobbySettings;

    [SerializeField] NetworkManager networkManager;
    [SerializeField] string lobbyName = "New Lobby";
    [SerializeField] LobbySettings lobbySettings;

    #region Callbacks
    [HideInInspector] public UnityEvent<LobbyCreated_t> OnLobbyCreatedEvent;
    [HideInInspector] public UnityEvent<LobbyEnter_t> OnLobbyJoinedEvent;
    [HideInInspector] public UnityEvent<LobbyChatUpdate_t> OnLobbyChatUpdateEvent;

    protected Callback<LobbyCreated_t> lobbyCreatedCallback;
    protected Callback<GameLobbyJoinRequested_t> lobbyJoinRequestCallback;
    protected Callback<LobbyEnter_t> lobbyEnterCallback;
    protected Callback<LobbyChatUpdate_t> lobbyChatUpdateCallback;
    #endregion

    private void Awake()
    {
        if (Instace != null)
            Destroy(Instace);

        Instace = this;
    }

    private void Start()
    {
        if (!SteamManager.Initialized)
        {
            BuildConsole.Instance.SendConsoleMessage("Steam is not initialized!");
            return;
        }

        if (networkManager == null)
            networkManager = FindAnyObjectByType<LLNetworkManager>();

        lobbyCreatedCallback = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        lobbyJoinRequestCallback = Callback<GameLobbyJoinRequested_t>.Create(OnLobbyJoinRequested);
        lobbyEnterCallback = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        lobbyChatUpdateCallback = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
    }

    void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
    {
        if (callback.m_ulSteamIDLobby == CurrentLobbyID.m_SteamID)
        {
            OnLobbyChatUpdateEvent?.Invoke(callback);
        }
    }

    #region Lobby Creation
    public void CreateLobby()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("Steam is not initialized!");
            return;
        }

        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, networkManager.maxConnections);
        BuildConsole.Instance.SendConsoleMessage($"Requesting to create lobby...");
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            BuildConsole.Instance.SendConsoleMessage($"Failed to create lobby: {callback.m_eResult}");
            return;
        }

        CurrentLobbyID = new (callback.m_ulSteamIDLobby);
        BuildConsole.Instance.SendConsoleMessage($"Lobby created successfully! Lobby ID: {CurrentLobbyID}");

        networkManager.StartHost();
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, "name", lobbyName);
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, "host", SteamFriends.GetPersonaName());
        SteamMatchmaking.SetLobbyData(CurrentLobbyID, "networkAddress", SteamUser.GetSteamID().ToString());

        OnLobbyCreatedEvent?.Invoke(callback);
    }
    #endregion

    #region Lobby Join
    void OnLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        BuildConsole.Instance.SendConsoleMessage("Lobby Join Requested: " + callback.m_steamIDLobby);
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    void OnLobbyEntered(LobbyEnter_t callback)
    {
        if (NetworkServer.active)
        {
            BuildConsole.Instance.SendConsoleMessage("Server seems to be already active");
            return;
        }

        BuildConsole.Instance.SendConsoleMessage("Lobby Entered: " + callback.m_ulSteamIDLobby);

        CurrentLobbyID = new(callback.m_ulSteamIDLobby);

        string hostAddress = SteamMatchmaking.GetLobbyData(new(callback.m_ulSteamIDLobby), "networkAddress");

        if (string.IsNullOrEmpty(hostAddress))
        {
            BuildConsole.Instance.SendConsoleMessage("Host address is missing in lobby data.");
            return;
        }
        
        networkManager.networkAddress = hostAddress;
        networkManager.StartClient();

        OnLobbyJoinedEvent?.Invoke(callback);
    }
    #endregion

    public void StartGame()
    {
        if (!NetworkServer.active)
        {
            Debug.LogWarning("Only the host can start the game!");
            return;
        }

        networkManager.ServerChangeScene("TestScene");
    }
}
