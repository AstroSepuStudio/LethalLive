using Mirror;
using Steamworks;
using UnityEngine;
using UnityEngine.Events;

public class LobbySettings : NetworkBehaviour
{
    public static LobbySettings Instance;

    [Header("Lobby Settings")]
    [SyncVar] private ELobbyType lobby_Type = ELobbyType.k_ELobbyTypeFriendsOnly;
    [SyncVar] private int mapSize = 10;
    [SyncVar] private bool teamDamage = false;
    [SyncVar] private bool teamKnock = true;
    [SyncVar] private int maxDays = 3;

    public ELobbyType Lobby_Type => lobby_Type;
    public int MapSize => mapSize;
    public bool TeamDamage => teamDamage;
    public bool TeamKnock => teamKnock;

    public int MaxDays => maxDays;

    public UnityEvent OnLobbySettingsChanged;

    private void Awake()
    {
        Instance = this;
    }

    [Server]
    public void SetLobbyType(ELobbyType lobbyType)
    {
        lobby_Type = lobbyType;
        SteamMatchmaking.SetLobbyType(LobbyManager.Instance.CurrentLobbyID, lobbyType);
        Rpc_LobbySettingsChanged();
    }

    [Server]
    public void SetMapSize(int mapSize)
    {
        this.mapSize = mapSize;
        Rpc_LobbySettingsChanged();
    }

    [Server]
    public void SetTeamDamage(bool teamDamage)
    {
        this.teamDamage = teamDamage;
        Rpc_LobbySettingsChanged();
    }

    [Server]
    public void SetTeamKnock(bool teamKnock)
    {
        this.teamKnock = teamKnock;
        Rpc_LobbySettingsChanged();
    }

    [Server]
    public void SetMaxDays(int maxDays)
    {
        this.maxDays = maxDays;
        Rpc_LobbySettingsChanged();
    }

    [ClientRpc]
    private void Rpc_LobbySettingsChanged() => OnLobbySettingsChanged?.Invoke();
}
