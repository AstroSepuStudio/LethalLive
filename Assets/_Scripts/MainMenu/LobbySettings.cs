using Mirror;
using Steamworks;
using UnityEngine;

public class LobbySettings : NetworkBehaviour
{
    [Header("Lobby Settings")]
    [SyncVar] private ELobbyType lobby_Type = ELobbyType.k_ELobbyTypeFriendsOnly;
    [SyncVar] private int mapSize = 30;
    [SyncVar] private bool teamDamage = false;
    [SyncVar] private bool teamKnock = true;

    public ELobbyType Lobby_Type => lobby_Type;
    public int MapSize => mapSize;
    public bool TeamDamage => teamDamage;
    public bool TeamKnock => teamKnock;

    [Server]
    public void SetLobbyType(ELobbyType lobbyType)
    {
        lobby_Type = lobbyType;
    }

    [Server]
    public void SetMapSize(int mapSize)
    {
        this.mapSize = mapSize;
    }

    [Server]
    public void SetTeamDamage(bool teamDamage)
    {
        this.teamDamage = teamDamage;
    }

    [Server]
    public void SetTeamKnock(bool teamKnock)
    {
        this.teamKnock = teamKnock;
    }
}
