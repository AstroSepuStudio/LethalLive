using Mirror;
using Steamworks;
using UnityEngine;

public class LobbySettings : NetworkBehaviour
{
    [Header("Lobby Settings")]
    [SyncVar] private ELobbyType lobby_Type;
    [SyncVar] private int mapSize;
    [SyncVar] private bool teamDamage;
    [SyncVar] private bool teamKnock;

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
