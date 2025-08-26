using Mirror;
using Steamworks;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }
    [SerializeField] ItemSO[] itemsData;
    [SerializeField] LobbyManagerScreen lobbyManagerScreen;

    [SerializeField] List<PlayerData> players = new ();
    public IReadOnlyList<PlayerData> Players => players;

    public readonly SyncList<uint> syncedItemIds = new();

    public PlayerData LocalPlayer;

    public struct LobbyMemberData
    {
        public CSteamID SteamID;
        public string Name;
        public byte[] AvatarData;
        public PlayerTeam Team;
        public int Ping;
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        //DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (isServer)
        {
            SpawnItem(transform.position);
            SpawnItem(transform.position + transform.forward);
            SpawnItem(transform.position - transform.forward);
            SpawnItem(transform.position + transform.right);
            SpawnItem(transform.position - transform.right);
        }
    }

    [Server]
    public void RegisterPlayer(PlayerData player)
    {
        if (!players.Contains(player))
        {
            player.Index = players.Count;
            player.Team = PlayerTeam.Hololive;
            players.Add(player);
            Debug.Log($"Player registered: {player.netIdentity.connectionToClient.connectionId}");
        }
    }

    [Server]
    public void UnregisterPlayer(PlayerData player)
    {
        if (players.Contains(player))
        {
            players.Remove(player);
            Debug.Log($"Player unregistered: {player.netIdentity.connectionToClient.connectionId}");
        }
    }

    [Server]
    void SpawnItem(Vector3 position)
    {
        GameObject itemGO = Instantiate(itemsData[0].itemPrefab, position, Quaternion.identity);
        NetworkServer.Spawn(itemGO);

        uint netId = itemGO.GetComponent<NetworkIdentity>().netId;
        syncedItemIds.Add(netId);
    }

    [Command(requiresAuthority = false)]
    public void CmdSetPlayerPing(int index, int ping) => players[index].Ping = ping;

    [Server]
    public void RequestScreenRefresh()
    {
        List<LobbyMemberData> members = new();

        foreach (var player in players)
        {
            members.Add(new LobbyMemberData
            {
                SteamID = player.SteamID,
                Name = player.PlayerName,
                AvatarData = player.AvatarData,
                Team = player.Team,
                Ping = player.Ping
            });
        }

        RpcRefreshScreen(members.ToArray());
    }

    [ClientRpc]
    void RpcRefreshScreen(LobbyMemberData[] members)
    {
        if (lobbyManagerScreen == null)
            return;

        lobbyManagerScreen.RefreshScreen(members);
    }

    [Server]
    public void CmdRequestOpenLMS(int index) => RpcOpenLobbyManagerScreen(index);

    [ClientRpc]
    void RpcOpenLobbyManagerScreen(int index)
    {
        if (lobbyManagerScreen == null)
            return;

        lobbyManagerScreen.OpenLobbyManagerScreen(index);
    }

    [Command(requiresAuthority = false)]
    public void CmdRequestTeamChange(int playerIndex, PlayerTeam team)
    {
        players[playerIndex].Team = team;
    }
}
