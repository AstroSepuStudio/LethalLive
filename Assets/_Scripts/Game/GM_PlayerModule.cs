using Mirror;
using Steamworks;
using System.Collections.Generic;
using UnityEngine;

public class GM_PlayerModule : NetworkBehaviour
{
    public Transform[] spawnPoints;

    [SerializeField] List<PlayerData> players = new();
    [SerializeField] List<uint> deadPlayers = new();

    [HideInInspector]
    public PlayerData LocalPlayer;

    public IReadOnlyList<PlayerData> Players => players;
    public List<PlayerData> playersOnDungeon = new();

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
        foreach(PlayerData p in players)
            if (p.netId == netId)
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

        foreach (var player in players)
            player.DeathOvManager.RefreshPlayers();
    }

    [Server]
    public void PlayerDies(uint index)
    { 
        if (!deadPlayers.Contains(index)) 
            deadPlayers.Add(index);

        foreach (var player in players)
        {
            if (deadPlayers.Contains(player.netId))
                player.DeathOvManager.RefreshPlayers();
        }

        if (deadPlayers.Count == players.Count)
        {
            Debug.Log("All players died");
            GameManager.Instance.ResetGame();
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
            deadPlayers.Remove(player.netId);
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
            deadPlayers.Remove(player.netId);
        }
    }

    public void RequestReturnToMainMenu()
    {
        LobbyManager.Instance.LeaveLobby();
    }
}
