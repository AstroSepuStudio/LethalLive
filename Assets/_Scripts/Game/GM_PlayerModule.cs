using Mirror;
using Steamworks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static GameManager;

public class GM_PlayerModule : NetworkBehaviour
{
    public Transform[] spawnPoints;

    [SerializeField] List<PlayerData> players = new();
    [SerializeField] List<uint> deadPlayers = new();

    [HideInInspector]
    public PlayerData LocalPlayer;

    public List<PlayerData> Players => players;
    public List<PlayerData> playersOnDungeon = new();

    public LobbyMemberData[] CachedMemberData { get; private set; }
    public UnityEvent OnLobbyMemberDataChanged;

    [Command(requiresAuthority = false)]
    public void CmdRequestTeamChange(int playerIndex, PlayerTeam team)
    {
        players[playerIndex].Team = team;
        players[playerIndex].RPC_OnPlayerTeamChanged(team);
    }

    public PlayerData GetPlayerByIndex(int index)
    {
        foreach (PlayerData p in Players)
            if (p.Index == index)
                return p;
        return null;
    }

    public PlayerData GetPlayerByNetId(uint netId)
    {
        foreach (PlayerData p in players)
            if (p.netId == netId)
                return p;
        return null;
    }

    public PlayerData GetPlayerBySteamId(CSteamID steamID)
    {
        foreach (PlayerData p in players)
            if (p.SteamID == steamID)
                return p;
        return null;
    }

    [Server]
    public void RegisterPlayer(PlayerData player)
    {
        if (!players.Contains(player))
        {
            player.Index = players.Count;
            player.Team = PlayerTeam.White;
            players.Add(player);
        }
    }

    [Server]
    public void UnregisterPlayer(PlayerData player)
    {
        if (players.Contains(player))
        {
            players.Remove(player);
        }
    }

    [Server]
    public void ExecuteAllPlayers()
    {
        foreach (var player in players)
        {
            if (deadPlayers.Contains(player.netId)) continue;
            player.Player_Stats.ExecutePlayer();
            deadPlayers.Add(player.netId);
        }

        RefreshLobbyMemberData();
    }

    [Server]
    public void PlayerDies(uint index)
    {
        if (!deadPlayers.Contains(index))
            deadPlayers.Add(index);

        RefreshLobbyMemberData();

        if (deadPlayers.Count == players.Count)
        {
            Debug.Log("All players died");
            Instance.ResetGame();
        }
    }

    [Server]
    public void ReviveAllPlayers()
    {
        foreach (var player in players)
        {
            if (!deadPlayers.Contains(player.netId)) continue;

            Debug.Log($"[server] revives player {player.PlayerName}({player.netId})");

            Transform spawn = spawnPoints.Length > 0
            ? spawnPoints[player.Index % spawnPoints.Length]
            : null;

            Vector3 spawnPos = spawn ? spawn.position : transform.position;

            player.RevivePlayer(spawnPos);
            player._PlayerInOffice = true;

            deadPlayers.Remove(player.netId);

            RefreshLobbyMemberData();
        }
    }

    [Server]
    public void RevivePlayer(uint id)
    {
        foreach (var player in players)
        {
            if (player.netId != id) return;
            if (!deadPlayers.Contains(player.netId)) continue;

            Debug.Log($"[server] revives player {player.PlayerName}({player.netId})");

            Transform spawn = spawnPoints.Length > 0
            ? spawnPoints[player.Index % spawnPoints.Length]
            : null;

            Vector3 spawnPos = spawn ? spawn.position : transform.position;

            player.RevivePlayer(spawnPos);
            player._PlayerInOffice = true;

            deadPlayers.Remove(player.netId);

            RefreshLobbyMemberData();
        }
    }

    public void RequestReturnToMainMenu()
    {
        LobbyManager.Instance.LeaveLobby();
    }

    [Server]
    public void RefreshLobbyMemberData()
    {
        List<LobbyMemberData> players = new();

        foreach (var player in Instance.playMod.Players)
        {
            players.Add(new LobbyMemberData
            {
                SteamID = player.SteamID,
                netID = player.netId,
                Name = player.PlayerName,
                AvatarData = player.AvatarData,
                Team = player.Team,
                Ping = player.Ping
            });
        }

        Rpc_RefreshLobbyMemberData(players.ToArray());
    }

    [ClientRpc]
    public void Rpc_RefreshLobbyMemberData(LobbyMemberData[] members)
    {
        CachedMemberData = members;

        foreach (var member in members)
        {
            if (PlayerContains(member.SteamID))
                continue;

            if (NetworkClient.spawned.TryGetValue(member.netID, out NetworkIdentity identity))
                if (identity.TryGetComponent(out PlayerData pData))
                {
                    players.Add(pData);
                    pData.SteamID = member.SteamID;
                    pData.AvatarData = member.AvatarData;
                }
        }

        OnLobbyMemberDataChanged?.Invoke();
    }

    private bool PlayerContains(CSteamID steamID)
    {
        foreach (var player in players)
        {
            if (player == null) continue;
            if (player.SteamID == steamID)
                return true;
        }
        return false;
    }
}
